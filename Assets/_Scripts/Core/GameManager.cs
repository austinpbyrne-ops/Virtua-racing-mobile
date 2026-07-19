using UnityEngine;

namespace VRacer.Core
{
    /// <summary>
    /// Central game state machine. Controls the entire game flow:
    /// BOOT → Title → Menu → Race → Results → (loop)
    /// </summary>
    public enum GameState
    {
        Boot,
        TitleScreen,
        AttractMode,
        MainMenu,
        TransmissionSelect,
        PreRaceGrid,
        Racing,
        RaceFinished,
        Results,
        HighScores,
        Options
    }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("State")]
        [SerializeField] private GameState currentState = GameState.Boot;

        [Header("Race Settings")]
        [SerializeField] private int defaultLaps = 5;
        [SerializeField] private int checkpointTimeBonus = 30; // seconds added per checkpoint

        [Header("References")]
        [SerializeField] private RaceManager raceManager;
        [SerializeField] private AudioManager audioManager;
        [SerializeField] private CameraManager cameraManager;

        // Current game context
        public GameState CurrentState => currentState;
        public bool IsGrandPrix { get; set; }
        public int GrandPrixTrackIndex { get; set; }
        public int GrandPrixTotalPoints { get; set; }
        public bool IsManualTransmission { get; set; }
        public int CurrentLaps => defaultLaps;
        public int CheckpointTimeBonus => checkpointTimeBonus;

        // Time tracking for attract mode
        private float idleTimer = 0f;
        private const float ATTRACT_MODE_DELAY = 15f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
        }

        private void Start()
        {
            SetState(GameState.TitleScreen);
        }

        private void Update()
        {
            // Attract mode timer (title screen only)
            if (currentState == GameState.TitleScreen)
            {
                idleTimer += Time.unscaledDeltaTime;
                if (idleTimer >= ATTRACT_MODE_DELAY)
                {
                    SetState(GameState.AttractMode);
                }
            }
        }

        public void SetState(GameState newState)
        {
            if (currentState == newState) return;

            ExitState(currentState);
            currentState = newState;
            EnterState(currentState);

            // Reset idle timer on any state change
            idleTimer = 0f;
        }

        private void ExitState(GameState state)
        {
            switch (state)
            {
                case GameState.AttractMode:
                    // Stop attract replay
                    break;
                case GameState.Racing:
                    // Cleanup race
                    break;
            }
        }

        private void EnterState(GameState state)
        {
            switch (state)
            {
                case GameState.TitleScreen:
                    idleTimer = 0f;
                    break;
                case GameState.AttractMode:
                    StartAttractMode();
                    break;
                case GameState.Racing:
                    StartRace();
                    break;
                case GameState.Results:
                    ShowResults();
                    break;
            }
        }

        // ============================================================
        // GAME FLOW METHODS
        // ============================================================

        public void StartSingleRace(int trackIndex = 0)
        {
            IsGrandPrix = false;
            SetState(GameState.TransmissionSelect);
        }

        public void StartGrandPrix()
        {
            IsGrandPrix = true;
            GrandPrixTrackIndex = 0;
            GrandPrixTotalPoints = 0;
            SetState(GameState.TransmissionSelect);
        }

        public void StartTimeTrial(int trackIndex = 0)
        {
            IsGrandPrix = false;
            // Load time trial directly
            raceManager?.InitializeTimeTrial(trackIndex);
            SetState(GameState.PreRaceGrid);
        }

        public void OnTransmissionSelected(bool isManual)
        {
            IsManualTransmission = isManual;
            SetState(GameState.PreRaceGrid);
        }

        private void StartRace()
        {
            int trackIndex = IsGrandPrix ? GrandPrixTrackIndex : 0;
            raceManager?.InitializeRace(trackIndex, defaultLaps, IsGrandPrix);
        }

        public void OnRaceFinished(int playerPosition, float bestLapTime, float totalTime)
        {
            if (IsGrandPrix)
            {
                int points = PositionToPoints(playerPosition);
                GrandPrixTotalPoints += points;
                GrandPrixTrackIndex++;

                if (GrandPrixTrackIndex >= 3) // All 3 tracks done
                {
                    IsGrandPrix = false;
                    SetState(GameState.Results);
                }
                else
                {
                    // Next track
                    SetState(GameState.PreRaceGrid);
                }
            }
            else
            {
                SetState(GameState.Results);
            }
        }

        private int PositionToPoints(int position)
        {
            // F1-style points: 25, 18, 15, 12, 10, 8, 6, 4, 2, 1
            int[] points = { 25, 18, 15, 12, 10, 8, 6, 4, 2, 1 };
            int index = Mathf.Clamp(position - 1, 0, points.Length - 1);
            return points[index];
        }

        private void ShowResults()
        {
            // Results screen handles display
        }

        private void StartAttractMode()
        {
            // Play AI replay on Big Forest
            // Cycle camera views automatically
            Debug.Log("[Attract Mode] Playing demo reel...");
        }

        public void ReturnToTitle()
        {
            SetState(GameState.TitleScreen);
        }

        public void ReturnToMenu()
        {
            SetState(GameState.MainMenu);
        }

        public void OnAnyInput()
        {
            if (currentState == GameState.AttractMode)
            {
                SetState(GameState.TitleScreen);
            }
        }
    }
}
