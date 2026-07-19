using UnityEngine;
using System.Collections.Generic;
using VRacer.Car;

namespace VRacer.Core
{
    /// <summary>
    /// Attract mode / Replay system.
    /// Records race data and plays it back as a demo reel.
    /// Cycles through V.R. camera views during playback.
    /// Shows "INSERT COIN" / "TAP TO START" overlay.
    /// </summary>
    public class ReplaySystem : MonoBehaviour
    {
        [Header("Recording")]
        [SerializeField] private bool isRecording = false;
        [SerializeField] private float recordInterval = 0.033f; // ~30fps recording
        [SerializeField] private float maxRecordTime = 120f;     // 2 minutes max
        private float recordTimer;
        private List<ReplayFrame> recordedFrames;

        [Header("Playback")]
        [SerializeField] private bool isPlaying = false;
        [SerializeField] private int currentFrameIndex = 0;
        [SerializeField] private float playbackSpeed = 1f;
        [SerializeField] private float cameraCycleInterval = 8f; // seconds between camera changes
        private float cameraCycleTimer;

        [Header("Attract Mode")]
        [SerializeField] private GameObject attractOverlay;
        [SerializeField] private GameObject tapToStartText;
        [SerializeField] private float overlayUpdateInterval = 3f;
        private float overlayTimer;
        private string[] attractMessages = {
            "INSERT COIN",
            "TAP TO START",
            "VIRTUA RACING",
            "THE XARCADE EDITION",
            "V.R. VIEW SYSTEM"
        };
        private int attractMessageIndex = 0;

        // References
        private List<CarController> allCars;
        private CarController recordingCar;

        private void Start()
        {
            recordedFrames = new List<ReplayFrame>();
        }

        private void Update()
        {
            if (isRecording)
            {
                UpdateRecording();
            }
            else if (isPlaying)
            {
                UpdatePlayback();
            }
        }

        // ============================================================
        // RECORDING
        // ============================================================

        public void StartRecording(CarController car = null)
        {
            recordingCar = car;
            if (recordingCar == null)
            {
                // Find player car
                var cars = FindObjectsByType<CarController>(FindObjectsSortMode.None);
                foreach (var c in cars)
                {
                    if (c.IsPlayer)
                    {
                        recordingCar = c;
                        break;
                    }
                }
            }

            recordedFrames.Clear();
            recordTimer = 0f;
            isRecording = true;
        }

        public void StopRecording()
        {
            isRecording = false;
            Debug.Log($"[Replay] Recorded {recordedFrames.Count} frames ({recordTimer:F1}s)");
        }

        private void UpdateRecording()
        {
            if (recordingCar == null) return;

            recordTimer += Time.deltaTime;
            if (recordTimer >= maxRecordTime)
            {
                StopRecording();
                return;
            }

            // Record at fixed interval
            if (recordedFrames.Count == 0 ||
                recordTimer - recordedFrames[^1].time >= recordInterval)
            {
                recordedFrames.Add(new ReplayFrame
                {
                    time = recordTimer,
                    position = recordingCar.transform.position,
                    rotation = recordingCar.transform.rotation,
                    speed = recordingCar.CurrentSpeed,
                    steer = recordingCar.inputSteer
                });
            }
        }

        // ============================================================
        // PLAYBACK
        // ============================================================

        public void StartPlayback()
        {
            if (recordedFrames.Count < 2) return;

            currentFrameIndex = 0;
            cameraCycleTimer = 0f;
            isPlaying = true;

            // Show attract overlay
            if (attractOverlay != null) attractOverlay.SetActive(true);
            overlayTimer = 0f;
            attractMessageIndex = 0;
        }

        public void StopPlayback()
        {
            isPlaying = false;
            if (attractOverlay != null) attractOverlay.SetActive(false);
        }

        private void UpdatePlayback()
        {
            if (recordedFrames.Count < 2)
            {
                // Loop
                currentFrameIndex = 0;
                return;
            }

            // Advance frame
            currentFrameIndex = (currentFrameIndex + 1) % recordedFrames.Count;

            // Apply position/rotation to recording car
            ReplayFrame frame = recordedFrames[currentFrameIndex];
            recordingCar.transform.position = frame.position;
            recordingCar.transform.rotation = frame.rotation;

            // Cycle camera views during replay
            cameraCycleTimer += Time.deltaTime;
            if (cameraCycleTimer >= cameraCycleInterval)
            {
                cameraCycleTimer = 0f;
                Camera.CameraManager.Instance?.CycleCameraView();
            }

            // Update attract overlay
            overlayTimer += Time.deltaTime;
            if (overlayTimer >= overlayUpdateInterval)
            {
                overlayTimer = 0f;
                attractMessageIndex = (attractMessageIndex + 1) % attractMessages.Length;
                UpdateAttractOverlay();
            }
        }

        private void UpdateAttractOverlay()
        {
            // Cycle through attract messages
            var text = tapToStartText?.GetComponent<UnityEngine.UI.Text>();
            if (text != null)
            {
                text.text = attractMessages[attractMessageIndex];
            }
        }

        // ============================================================
        // DATA
        // ============================================================

        [System.Serializable]
        public struct ReplayFrame
        {
            public float time;
            public Vector3 position;
            public Quaternion rotation;
            public float speed;
            public float steer;
        }

        public List<ReplayFrame> GetRecordedFrames() => recordedFrames;
        public bool IsRecording => isRecording;
        public bool IsPlaying => isPlaying;
    }
}
