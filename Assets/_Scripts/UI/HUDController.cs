using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VRacer.Car;
using VRacer.Core;

namespace VRacer.UI
{
    /// <summary>
    /// Full race HUD matching the original Virtua Racing layout:
    /// ┌──────────────────────────────┐
    /// │ POSITION    TIME    LAP TIME │
    /// │ 7TH/16      65      1'00"00 │
    /// │                              │
    /// │     [GAMEPLAY VIEW]          │
    /// │                              │
    /// │ SPEED           BEGINNER     │
    /// │ 150mph          [MINIMAP]   │
    /// └──────────────────────────────┘
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        public static HUDController Instance { get; private set; }

        [Header("Top Bar")]
        [SerializeField] private TextMeshProUGUI positionText;
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private TextMeshProUGUI lapTimeText;
        [SerializeField] private TextMeshProUGUI lapCounterText;

        [Header("Bottom Bar")]
        [SerializeField] private TextMeshProUGUI speedText;
        [SerializeField] private TextMeshProUGUI speedUnitText;
        [SerializeField] private TextMeshProUGUI gearText;
        [SerializeField] private TextMeshProUGUI difficultyText;
        [SerializeField] private TextMeshProUGUI bestLapText;

        [Header("Checkpoint Flash")]
        [SerializeField] private GameObject checkpointFlash;
        [SerializeField] private TextMeshProUGUI checkpointText;
        [SerializeField] private float checkpointFlashDuration = 2f;
        private float checkpointFlashTimer;

        [Header("Timer Warning")]
        [SerializeField] private Color normalTimerColor = Color.white;
        [SerializeField] private Color warningTimerColor = Color.red;
        [SerializeField] private float warningFlashSpeed = 2f;
        private bool timerWarning = false;

        [Header("Countdown")]
        [SerializeField] private TextMeshProUGUI countdownText;
        [SerializeField] private GameObject countdownPanel;

        [Header("Minimap")]
        [SerializeField] private RawImage minimapImage;
        [SerializeField] private RectTransform playerDot;
        [SerializeField] private RectTransform[] opponentDots;

        [Header("Gear Indicator")]
        [SerializeField] private GameObject manualGearPanel;

        [Header("Results Overlay")]
        [SerializeField] private GameObject resultsPanel;
        [SerializeField] private TextMeshProUGUI finalPositionText;
        [SerializeField] private TextMeshProUGUI bestLapResultText;
        [SerializeField] private TextMeshProUGUI totalTimeText;
        [SerializeField] private TextMeshProUGUI continueCountdownText;

        // References
        private CarController playerCar;
        private RaceManager raceManager;

        // Units toggle
        private bool useMPH = true;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            raceManager = RaceManager.Instance;

            if (checkpointFlash != null)
                checkpointFlash.SetActive(false);

            if (countdownPanel != null)
                countdownPanel.SetActive(false);

            if (manualGearPanel != null)
                manualGearPanel.SetActive(false);
        }

        private void Update()
        {
            if (playerCar == null)
            {
                FindPlayerCar();
                return;
            }

            UpdatePositionDisplay();
            UpdateTimerDisplay();
            UpdateSpeedDisplay();
            UpdateGearDisplay();
            UpdateCheckpointFlash();
            UpdateTimerWarning();
            UpdateCountdown();
        }

        private void FindPlayerCar()
        {
            var cars = FindObjectsByType<CarController>(FindObjectsSortMode.None);
            foreach (var car in cars)
            {
                if (car.IsPlayer)
                {
                    playerCar = car;
                    break;
                }
            }
        }

        // ============================================================
        // DISPLAY UPDATES
        // ============================================================

        private void UpdatePositionDisplay()
        {
            if (positionText == null || playerCar == null) return;

            int pos = playerCar.RacePosition;
            string suffix = GetOrdinalSuffix(pos);
            positionText.text = $"<color=white>POSITION</color>\n<color=yellow>{pos}{suffix}/16</color>";
        }

        private void UpdateTimerDisplay()
        {
            if (timerText == null || raceManager == null) return;

            float time = raceManager.CheckpointTime;
            timerText.text = $"<color=white>TIME</color>\n<color=yellow>{Mathf.CeilToInt(time)}</color>";
        }

        private void UpdateSpeedDisplay()
        {
            if (speedText == null || playerCar == null) return;

            float speed = playerCar.CurrentSpeed;
            if (useMPH) speed *= 0.621371f; // km/h to mph

            speedText.text = $"<color=white>SPEED</color>\n<color=yellow>{Mathf.RoundToInt(speed)}</color>";
            if (speedUnitText != null)
                speedUnitText.text = useMPH ? "mph" : "km/h";
        }

        private void UpdateGearDisplay()
        {
            if (!GameManager.Instance || !GameManager.Instance.IsManualTransmission)
            {
                if (manualGearPanel != null) manualGearPanel.SetActive(false);
                return;
            }

            if (manualGearPanel != null) manualGearPanel.SetActive(true);
            if (gearText != null && playerCar != null)
            {
                int gear = playerCar.CurrentGear;
                gearText.text = gear == 0 ? "R" : gear.ToString();
            }
        }

        private void UpdateCheckpointFlash()
        {
            if (checkpointFlashTimer > 0f)
            {
                checkpointFlashTimer -= Time.deltaTime;
                if (checkpointFlashTimer <= 0f)
                {
                    if (checkpointFlash != null) checkpointFlash.SetActive(false);
                }
            }
        }

        private void UpdateTimerWarning()
        {
            if (raceManager == null || timerText == null) return;

            if (raceManager.IsTimerWarning)
            {
                timerWarning = true;
                float flash = Mathf.Abs(Mathf.Sin(Time.time * warningFlashSpeed));
                timerText.color = Color.Lerp(normalTimerColor, warningTimerColor, flash);
            }
            else if (timerWarning)
            {
                timerWarning = false;
                timerText.color = normalTimerColor;
            }
        }

        private void UpdateCountdown()
        {
            if (raceManager == null) return;

            if (raceManager.CountdownActive)
            {
                if (countdownPanel != null) countdownPanel.SetActive(true);
                float remaining = raceManager.CountdownRemaining;
                int display = Mathf.CeilToInt(remaining);

                if (display <= 0)
                {
                    if (countdownText != null) countdownText.text = "GO!";
                }
                else
                {
                    if (countdownText != null) countdownText.text = display.ToString();
                }
            }
            else
            {
                if (countdownPanel != null && countdownPanel.activeSelf)
                {
                    // Brief "GO!" then hide
                    Invoke(nameof(HideCountdown), 1f);
                }
            }
        }

        private void HideCountdown()
        {
            if (countdownPanel != null) countdownPanel.SetActive(false);
        }

        // ============================================================
        // EVENT HANDLERS
        // ============================================================

        public void OnCheckpointCleared(int checkpointIndex)
        {
            checkpointFlashTimer = checkpointFlashDuration;
            if (checkpointFlash != null)
            {
                checkpointFlash.SetActive(true);
                if (checkpointText != null)
                    checkpointText.text = "CHECKPOINT!";
            }
        }

        public void OnLapCompleted(int lap)
        {
            if (lapCounterText != null)
                lapCounterText.text = $"LAP {lap}/{raceManager.TotalLaps}";

            // Update best lap
            if (bestLapText != null)
                bestLapText.text = raceManager.FormatTime(raceManager.BestLapTime);

            // Update lap time
            if (lapTimeText != null)
                lapTimeText.text = $"<color=white>LAP TIME</color>\n<color=yellow>{raceManager.FormatLapTime()}</color>";
        }

        public void ShowResults(int position, float bestLap, float totalTime)
        {
            if (resultsPanel != null) resultsPanel.SetActive(true);

            if (finalPositionText != null)
                finalPositionText.text = $"{position}{GetOrdinalSuffix(position)} PLACE";

            if (bestLapResultText != null)
                bestLapResultText.text = $"BEST LAP: {raceManager.FormatTime(bestLap)}";

            if (totalTimeText != null)
                totalTimeText.text = $"TOTAL TIME: {raceManager.FormatTime(totalTime)}";

            // Continue countdown — arcade homage
            StartCoroutine(ContinueCountdown());
        }

        private System.Collections.IEnumerator ContinueCountdown()
        {
            for (int i = 10; i >= 0; i--)
            {
                if (continueCountdownText != null)
                    continueCountdownText.text = $"CONTINUE? {i}";
                yield return new WaitForSeconds(1f);
            }

            // Time's up — return to title
            GameManager.Instance?.ReturnToTitle();
        }

        // ============================================================
        // UTILITY
        // ============================================================

        private string GetOrdinalSuffix(int number)
        {
            int lastDigit = number % 10;
            int lastTwo = number % 100;

            if (lastTwo >= 11 && lastTwo <= 13) return "TH";
            return lastDigit switch
            {
                1 => "ST",
                2 => "ND",
                3 => "RD",
                _ => "TH"
            };
        }
    }
}
