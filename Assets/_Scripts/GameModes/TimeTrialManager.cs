using UnityEngine;
using System.Collections.Generic;
using VRacer.Car;
using VRacer.Data;

namespace VRacer.GameModes
{
    /// <summary>
    /// Time Trial mode: solo hotlap with ghost car.
    /// Best time per track saved locally.
    /// </summary>
    public class TimeTrialManager : MonoBehaviour
    {
        public static TimeTrialManager Instance { get; private set; }

        [Header("Ghost Car")]
        [SerializeField] private GameObject ghostCarPrefab;
        [SerializeField] private Material ghostMaterial;
        private GameObject ghostCarInstance;
        private List<GhostFrame> ghostFrames;
        private List<GhostFrame> currentRunFrames;
        private int currentGhostFrameIndex;

        [Header("Timing")]
        [SerializeField] private float bestLapTime = float.MaxValue;
        [SerializeField] private float currentLapTime = 0f;
        [SerializeField] private bool isRecording = false;
        [SerializeField] private float recordingInterval = 0.05f; // 20Hz recording
        private float lastRecordTime;

        private string currentTrackName;

        private void Awake()
        {
            Instance = this;
        }

        public void StartTimeTrial(string trackName)
        {
            currentTrackName = trackName;
            bestLapTime = SaveManager.Instance?.GetBestLap(trackName) ?? float.MaxValue;
            currentLapTime = 0f;
            isRecording = true;
            lastRecordTime = 0f;

            currentRunFrames = new List<GhostFrame>();

            // Load and spawn ghost car
            ghostFrames = SaveManager.Instance?.LoadGhostData(trackName) ?? new List<GhostFrame>();
            if (ghostFrames.Count > 0)
            {
                SpawnGhostCar();
            }
        }

        private void Update()
        {
            if (!isRecording) return;

            currentLapTime += Time.deltaTime;

            // Record ghost frame
            if (Time.time - lastRecordTime >= recordingInterval)
            {
                RecordFrame();
                lastRecordTime = Time.time;
            }

            // Playback ghost
            if (ghostCarInstance != null && ghostFrames.Count > 0)
            {
                UpdateGhostCar();
            }
        }

        private void RecordFrame()
        {
            CarController player = FindPlayerCar();
            if (player == null) return;

            currentRunFrames.Add(new GhostFrame
            {
                time = currentLapTime,
                position = player.transform.position,
                rotation = player.transform.eulerAngles,
                speed = player.CurrentSpeed
            });
        }

        private CarController FindPlayerCar()
        {
            var cars = FindObjectsByType<CarController>(FindObjectsSortMode.None);
            foreach (var car in cars)
            {
                if (car.IsPlayer) return car;
            }
            return null;
        }

        // ============================================================
        // GHOST CAR
        // ============================================================

        private void SpawnGhostCar()
        {
            if (ghostCarPrefab == null) return;

            ghostCarInstance = Instantiate(ghostCarPrefab);

            // Make ghost car translucent/distinct
            var renderers = ghostCarInstance.GetComponentsInChildren<MeshRenderer>();
            foreach (var r in renderers)
            {
                if (ghostMaterial != null)
                    r.material = ghostMaterial;

                // Semi-transparent
                Color c = r.material.color;
                c.a = 0.4f;
                r.material.color = c;
            }

            // Ghost car has no physics
            Destroy(ghostCarInstance.GetComponent<Rigidbody>());
            Destroy(ghostCarInstance.GetComponent<CarController>());

            currentGhostFrameIndex = 0;
        }

        private void UpdateGhostCar()
        {
            if (ghostFrames.Count == 0) return;

            // Find the frame closest to current lap time
            while (currentGhostFrameIndex < ghostFrames.Count - 1 &&
                   ghostFrames[currentGhostFrameIndex + 1].time <= currentLapTime)
            {
                currentGhostFrameIndex++;
            }

            if (currentGhostFrameIndex >= ghostFrames.Count)
            {
                // Ghost has finished
                ghostCarInstance.SetActive(false);
                return;
            }

            // Interpolate between frames
            GhostFrame current = ghostFrames[currentGhostFrameIndex];
            GhostFrame next = currentGhostFrameIndex < ghostFrames.Count - 1
                ? ghostFrames[currentGhostFrameIndex + 1]
                : current;

            float t = 0f;
            float frameDuration = next.time - current.time;
            if (frameDuration > 0.001f)
            {
                t = (currentLapTime - current.time) / frameDuration;
                t = Mathf.Clamp01(t);
            }

            ghostCarInstance.transform.position = Vector3.Lerp(current.position, next.position, t);
            ghostCarInstance.transform.rotation = Quaternion.Slerp(
                Quaternion.Euler(current.rotation),
                Quaternion.Euler(next.rotation),
                t
            );
        }

        // ============================================================
        // LAP COMPLETION
        // ============================================================

        public void OnLapCompleted(float lapTime)
        {
            if (lapTime < bestLapTime)
            {
                bestLapTime = lapTime;

                // Save best lap
                SaveManager.Instance?.UpdateBestLap(currentTrackName, lapTime);

                // Save ghost data for this new best lap
                SaveManager.Instance?.SaveGhostData(currentTrackName, currentRunFrames);

                // New ghost to beat
                ghostFrames = currentRunFrames;
                if (ghostCarInstance == null) SpawnGhostCar();
                currentGhostFrameIndex = 0;
            }

            // Start new lap recording
            currentRunFrames.Clear();
            currentLapTime = 0f;
            lastRecordTime = 0f;
        }

        public void StopTimeTrial()
        {
            isRecording = false;

            if (ghostCarInstance != null)
            {
                Destroy(ghostCarInstance);
                ghostCarInstance = null;
            }
        }

        public float BestLapTime => bestLapTime;
        public float CurrentLapTime => currentLapTime;
    }
}
