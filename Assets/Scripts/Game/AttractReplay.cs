using UnityEngine;
using System.Collections.Generic;

// ============================================================
// AttractReplay — AI-driven replay for attract mode.
// Records and plays back race footage with camera cycling.
// Shows "INSERT COIN" / "TAP TO START" overlays.
// ============================================================

namespace VirtuaRacing
{
    public class AttractReplay : MonoBehaviour
    {
        [Header("Replay Settings")]
        [SerializeField] private float replayDuration = 60f;
        [SerializeField] private float cameraCycleInterval = 8f;
        [SerializeField] private bool loopReplay = true;
        
        [Header("Attract Mode UI")]
        [SerializeField] private GameObject attractOverlay;
        [SerializeField] private UnityEngine.UI.Text insertCoinText;
        [SerializeField] private UnityEngine.UI.Text tapToStartText;
        [SerializeField] private float textPulseSpeed = 1.5f;
        
        [Header("Camera Views (cycle through all 4 V.R. views)")]
        [SerializeField] private Camera replayCamera;
        [SerializeField] private CameraView[] cycleViews = { 
            CameraView.CloseChase, CameraView.NoseCam, 
            CameraView.FarChase, CameraView.CockpitCam 
        };
        
        // State
        private enum ReplayState { Idle, Recording, Playing }
        private ReplayState state = ReplayState.Idle;
        
        private List<ReplayFrame> recordedFrames = new List<ReplayFrame>();
        private int currentFrame;
        private float playbackTimer;
        private float cameraTimer;
        private int currentViewIndex;
        private float attractTimer;
        
        private CarController playerCar;
        private VRCameraSystem cameraSystem;
        
        [System.Serializable]
        private struct ReplayFrame
        {
            public Vector3 position;
            public Quaternion rotation;
            public Vector3 velocity;
            public Vector3 cameraPosition;
            public Quaternion cameraRotation;
            public float timestamp;
        }
        
        private void Start()
        {
            playerCar = FindObjectOfType<CarController>();
            cameraSystem = FindObjectOfType<VRCameraSystem>();
            
            if (replayCamera == null)
                replayCamera = Camera.main;
            
            // Start recording for attract mode replay
            StartRecording();
        }
        
        public void StartRecording()
        {
            recordedFrames.Clear();
            state = ReplayState.Recording;
            
            Debug.Log("[AttractReplay] Recording replay data...");
        }
        
        public void StopRecording()
        {
            state = ReplayState.Idle;
            Debug.Log($"[AttractReplay] Recording complete. {recordedFrames.Count} frames.");
        }
        
        public void StartPlayback()
        {
            if (recordedFrames.Count < 2)
            {
                Debug.LogWarning("[AttractReplay] Not enough frames to replay");
                return;
            }
            
            currentFrame = 0;
            playbackTimer = 0f;
            cameraTimer = 0f;
            currentViewIndex = 0;
            attractTimer = 0f;
            state = ReplayState.Playing;
            
            // Show attract overlay
            if (attractOverlay != null)
                attractOverlay.SetActive(true);
            
            Debug.Log("[AttractReplay] Playing attract mode replay");
        }
        
        public void StopPlayback()
        {
            state = ReplayState.Idle;
            
            if (attractOverlay != null)
                attractOverlay.SetActive(false);
        }
        
        private void Update()
        {
            switch (state)
            {
                case ReplayState.Recording:
                    UpdateRecording();
                    break;
                    
                case ReplayState.Playing:
                    UpdatePlayback();
                    break;
            }
        }
        
        private void UpdateRecording()
        {
            if (playerCar == null) return;
            
            recordedFrames.Add(new ReplayFrame
            {
                position = playerCar.transform.position,
                rotation = playerCar.transform.rotation,
                velocity = playerCar.GetComponent<Rigidbody>()?.velocity ?? Vector3.zero,
                cameraPosition = Camera.main != null ? Camera.main.transform.position : Vector3.zero,
                cameraRotation = Camera.main != null ? Camera.main.transform.rotation : Quaternion.identity,
                timestamp = Time.timeSinceLevelLoad
            });
            
            // Limit recording to ~2 minutes
            if (recordedFrames.Count > 3600) // ~60 sec at 60fps
            {
                recordedFrames.RemoveAt(0);
            }
        }
        
        private void UpdatePlayback()
        {
            if (recordedFrames.Count == 0) return;
            
            playbackTimer += Time.deltaTime;
            cameraTimer += Time.deltaTime;
            attractTimer += Time.deltaTime;
            
            // Camera cycling
            if (cameraTimer >= cameraCycleInterval)
            {
                cameraTimer = 0f;
                currentViewIndex = (currentViewIndex + 1) % cycleViews.Length;
                
                if (cameraSystem != null)
                    cameraSystem.SetView(cycleViews[currentViewIndex]);
            }
            
            // Advance frame
            while (currentFrame < recordedFrames.Count - 1 &&
                   recordedFrames[currentFrame + 1].timestamp < playbackTimer)
            {
                currentFrame++;
            }
            
            // Loop
            if (currentFrame >= recordedFrames.Count - 1)
            {
                if (loopReplay)
                {
                    currentFrame = 0;
                    playbackTimer = 0f;
                }
                else
                {
                    StopPlayback();
                    return;
                }
            }
            
            // Update car position
            if (playerCar != null && currentFrame < recordedFrames.Count - 1)
            {
                ReplayFrame current = recordedFrames[currentFrame];
                ReplayFrame next = recordedFrames[currentFrame + 1];
                
                float duration = next.timestamp - current.timestamp;
                float t = duration > 0 ? (playbackTimer - current.timestamp) / duration : 0;
                t = Mathf.Clamp01(t);
                
                playerCar.transform.position = Vector3.Lerp(current.position, next.position, t);
                playerCar.transform.rotation = Quaternion.Slerp(current.rotation, next.rotation, t);
            }
            
            // Update attract mode text
            UpdateAttractOverlay();
        }
        
        private void UpdateAttractOverlay()
        {
            if (attractOverlay == null || !attractOverlay.activeSelf) return;
            
            // Cycle between "INSERT COIN" and "TAP TO START"
            float cycle = attractTimer % 8f;
            
            if (insertCoinText != null)
            {
                float alpha = cycle < 4f ? 
                    Mathf.PingPong(cycle * textPulseSpeed, 1f) : 0f;
                Color c = insertCoinText.color;
                c.a = alpha;
                insertCoinText.color = c;
            }
            
            if (tapToStartText != null)
            {
                float alpha = cycle >= 4f ? 
                    Mathf.PingPong((cycle - 4f) * textPulseSpeed, 1f) : 0f;
                Color c = tapToStartText.color;
                c.a = alpha;
                tapToStartText.color = c;
            }
        }
        
        /// <summary>
        /// Pre-record a lap for attract mode (called from editor or boot)
        /// </summary>
        public void LoadPreRecordedReplay(string trackName)
        {
            string key = $"Replay_{trackName}";
            if (PlayerPrefs.HasKey(key))
            {
                string json = PlayerPrefs.GetString(key);
                // Deserialize and start playback
                // (Simplified — full impl would serialize frame data)
            }
            else
            {
                // No pre-recorded replay — record fresh
                StartRecording();
            }
        }
        
        /// <summary>
        /// Generate an AI-driven attract mode lap
        /// Instead of recording real player input, use AI to drive a demo lap
        /// </summary>
        public static void GenerateAIAttractLap(TrackBuilder track, CarController demoCar, AIOpponent demoAI)
        {
            // Simply let the AI drive — recording is handled by this system
            // The AI will follow waypoints and provide a visually appealing demo
            if (demoAI != null)
            {
                // AI already handles driving via FixedUpdate
                Debug.Log("[AttractReplay] AI-driven attract mode active");
            }
        }
    }
}
