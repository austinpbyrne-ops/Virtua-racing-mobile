using System;
using System.Collections.Generic;
using UnityEngine;
using VRacer.Core;

namespace VRacer.Core
{
    /// <summary>
    /// Manages a single race session: countdown, laps, checkpoints,
    /// positions, timer, and race end conditions.
    /// </summary>
    public class RaceManager : MonoBehaviour
    {
        public static RaceManager Instance { get; private set; }

        [Header("Race State")]
        [SerializeField] private int currentLap = 0;
        [SerializeField] private int totalLaps = 5;
        [SerializeField] private float raceTimer = 0f;
        [SerializeField] private float checkpointTimer = 0f;
        [SerializeField] private float bestLapTime = float.MaxValue;
        [SerializeField] private float currentLapTime = 0f;
        [SerializeField] private bool raceActive = false;
        [SerializeField] private bool raceFinished = false;

        [Header("Countdown")]
        [SerializeField] private float countdownDuration = 3f;
        private float countdownTimer = 0f;
        private bool countdownActive = false;

        [Header("Checkpoints")]
        [SerializeField] private List<CheckpointTrigger> checkpoints;
        [SerializeField] private int nextCheckpointIndex = 0;
        [SerializeField] private float initialCheckpointTime = 75f;

        [Header("Timing")]
        [SerializeField] private float warningBeepThreshold = 10f;
        private bool warningBeeping = false;

        // Events
        public event Action<int> OnLapCompleted;
        public event Action<int> OnCheckpointPassed;
        public event Action OnRaceFinished;
        public event Action OnTimerWarning;
        public event Action OnTimerExpired;

        private void Awake()
        {
            Instance = this;
        }

        private void Update()
        {
            if (!raceActive) return;

            if (countdownActive)
            {
                UpdateCountdown();
                return;
            }

            if (!raceFinished)
            {
                checkpointTimer -= Time.deltaTime;
                currentLapTime += Time.deltaTime;
                raceTimer += Time.deltaTime;

                // Timer warning
                if (checkpointTimer <= warningBeepThreshold && !warningBeeping)
                {
                    warningBeeping = true;
                    OnTimerWarning?.Invoke();
                }

                // Timer expired
                if (checkpointTimer <= 0f)
                {
                    checkpointTimer = 0f;
                    OnTimerExpired?.Invoke();
                    EndRace(false);
                }
            }
        }

        private void UpdateCountdown()
        {
            countdownTimer -= Time.deltaTime;
            if (countdownTimer <= 0f)
            {
                countdownActive = false;
                StartEngines();
            }
        }

        // ============================================================
        // INITIALIZATION
        // ============================================================

        public void InitializeRace(int trackIndex, int laps, bool isGrandPrix)
        {
            totalLaps = laps;
            currentLap = 0;
            raceTimer = 0f;
            bestLapTime = float.MaxValue;
            currentLapTime = 0f;
            raceActive = false;
            raceFinished = false;
            warningBeeping = false;

            // Find checkpoints for this track
            checkpoints = new List<CheckpointTrigger>(
                FindObjectsByType<CheckpointTrigger>(FindObjectsSortMode.None)
            );
            checkpoints.Sort((a, b) => a.CheckpointIndex.CompareTo(b.CheckpointIndex));

            nextCheckpointIndex = 0;
            checkpointTimer = initialCheckpointTime;

            // Start countdown
            StartCountdown();
        }

        public void InitializeTimeTrial(int trackIndex)
        {
            InitializeRace(trackIndex, 5, false);
            // Time trial: no opponents, ghost car enabled
        }

        private void StartCountdown()
        {
            countdownTimer = countdownDuration;
            countdownActive = true;
            raceActive = true;
            // Audio: "3... 2... 1... GO!" beeps
        }

        private void StartEngines()
        {
            // Race is go!
            raceActive = true;
        }

        // ============================================================
        // CHECKPOINT SYSTEM
        // ============================================================

        public void OnCarPassedCheckpoint(int checkpointIndex)
        {
            if (checkpointIndex != nextCheckpointIndex) return;

            nextCheckpointIndex = (nextCheckpointIndex + 1) % checkpoints.Count;

            // Add time bonus
            int bonus = GameManager.Instance?.CheckpointTimeBonus ?? 30;
            checkpointTimer += bonus;

            // Flash HUD
            OnCheckpointPassed?.Invoke(checkpointIndex);

            // If we completed a full lap (back to checkpoint 0)
            if (nextCheckpointIndex == 0)
            {
                CompleteLap();
            }

            warningBeeping = false;
        }

        private void CompleteLap()
        {
            currentLap++;

            // Track best lap
            if (currentLapTime < bestLapTime)
            {
                bestLapTime = currentLapTime;
            }
            currentLapTime = 0f;

            OnLapCompleted?.Invoke(currentLap);

            // Race finished?
            if (currentLap >= totalLaps)
            {
                EndRace(true);
            }
        }

        private void EndRace(bool completedAllLaps)
        {
            raceActive = false;
            raceFinished = true;

            // Finalize race
            OnRaceFinished?.Invoke();
        }

        // ============================================================
        // PUBLIC ACCESSORS
        // ============================================================

        public int CurrentLap => currentLap;
        public int TotalLaps => totalLaps;
        public float RaceTime => raceTimer;
        public float CheckpointTime => checkpointTimer;
        public float LapTime => currentLapTime;
        public float BestLapTime => bestLapTime == float.MaxValue ? 0f : bestLapTime;
        public bool RaceActive => raceActive && !countdownActive;
        public bool CountdownActive => countdownActive;
        public bool RaceFinished => raceFinished;
        public float CountdownRemaining => countdownTimer;
        public bool IsTimerWarning => checkpointTimer <= warningBeepThreshold;

        public string FormatTime(float seconds)
        {
            int mins = Mathf.FloorToInt(seconds / 60f);
            int secs = Mathf.FloorToInt(seconds % 60f);
            int hundredths = Mathf.FloorToInt((seconds * 100f) % 100f);
            return $"{mins}'{secs:00}\"{hundredths:00}";
        }

        public string FormatLapTime()
        {
            return FormatTime(currentLapTime);
        }
    }
}
