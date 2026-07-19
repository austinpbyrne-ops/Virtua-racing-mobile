using UnityEngine;
using System.Collections.Generic;

// ============================================================
// GhostCarSystem — Records and plays back ghost car data
// for Time Trial mode. Stores positions, rotations, 
// and timestamps for smooth playback.
// ============================================================

namespace VirtuaRacing
{
    public class GhostCarSystem : MonoBehaviour
    {
        [Header("Ghost Car")]
        [SerializeField] private GameObject ghostCarPrefab;
        [SerializeField] private Material ghostMaterial;
        [SerializeField] private Color ghostColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        [SerializeField] private float ghostAlpha = 0.4f;
        
        [Header("Recording")]
        [SerializeField] private float recordInterval = 0.033f; // ~30fps recording
        [SerializeField] private int maxRecordedFrames = 7200;  // ~4 minutes at 30fps
        
        // State
        private enum GhostState { Idle, Recording, Playing }
        private GhostState currentState = GhostState.Idle;
        
        private CarController playerCar;
        private GameObject ghostCarInstance;
        private Transform ghostTransform;
        
        // Recorded data
        private List<GhostFrame> recordedFrames = new List<GhostFrame>();
        private float recordTimer;
        private int playbackIndex;
        private float playbackTimer;
        private float totalRecordedTime;
        
        // Best lap tracking
        private float bestLapTime = float.MaxValue;
        private List<GhostFrame> bestLapFrames = new List<GhostFrame>();
        
        public float BestLapTime => bestLapTime;
        
        [System.Serializable]
        private struct GhostFrame
        {
            public Vector3 position;
            public Quaternion rotation;
            public Vector3 velocity;
            public float timestamp;
            public float steerInput;
            public float throttleInput;
            public float brakeInput;
            
            public GhostFrame(Transform t, Rigidbody rb, CarController car, float time)
            {
                position = t.position;
                rotation = t.rotation;
                velocity = rb != null ? rb.velocity : Vector3.zero;
                timestamp = time;
                steerInput = car != null ? car.SteerInput : 0;
                throttleInput = car != null ? car.ThrottleInput : 0;
                brakeInput = car != null ? car.BrakeInput : 0;
            }
        }
        
        private void Start()
        {
            playerCar = FindObjectOfType<CarController>();
        }
        
        public void StartRecording()
        {
            recordedFrames.Clear();
            recordTimer = 0f;
            totalRecordedTime = 0f;
            currentState = GhostState.Recording;
            
            // Destroy old ghost if exists
            if (ghostCarInstance != null)
                Destroy(ghostCarInstance);
            
            Debug.Log("[GhostCar] Recording started");
        }
        
        public void StopRecording()
        {
            currentState = GhostState.Idle;
            
            // Check if this lap is the best
            if (totalRecordedTime < bestLapTime)
            {
                bestLapTime = totalRecordedTime;
                bestLapFrames = new List<GhostFrame>(recordedFrames);
                
                // Notify game manager
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.RecordGhostLap(
                        GetPositionList(bestLapFrames),
                        GetRotationList(bestLapFrames),
                        bestLapTime
                    );
                }
                
                Debug.Log($"[GhostCar] New best lap: {bestLapTime:F3}s");
            }
            
            Debug.Log("[GhostCar] Recording stopped");
        }
        
        public void StartPlayback()
        {
            if (bestLapFrames.Count == 0)
            {
                Debug.Log("[GhostCar] No ghost data to play back");
                return;
            }
            
            // Create ghost car
            if (ghostCarPrefab != null)
            {
                ghostCarInstance = Instantiate(ghostCarPrefab);
                
                // Make it semi-transparent
                var renderers = ghostCarInstance.GetComponentsInChildren<MeshRenderer>();
                foreach (var r in renderers)
                {
                    if (ghostMaterial != null)
                    {
                        r.material = ghostMaterial;
                    }
                    else
                    {
                        // Set alpha via material color
                        Color c = r.material.color;
                        c.a = ghostAlpha;
                        r.material.color = c;
                        
                        // Make material transparent
                        r.material.SetFloat("_Surface", 1); // Transparent
                        r.material.SetFloat("_Blend", 0);   // Alpha
                    }
                    
                    // Disable collider on ghost
                    var colliders = ghostCarInstance.GetComponentsInChildren<Collider>();
                    foreach (var col in colliders)
                        col.enabled = false;
                }
                
                ghostTransform = ghostCarInstance.transform;
            }
            
            playbackIndex = 0;
            playbackTimer = 0f;
            currentState = GhostState.Playing;
            
            Debug.Log("[GhostCar] Playback started");
        }
        
        public void StopPlayback()
        {
            currentState = GhostState.Idle;
            
            if (ghostCarInstance != null)
                Destroy(ghostCarInstance);
            
            ghostTransform = null;
        }
        
        private void Update()
        {
            switch (currentState)
            {
                case GhostState.Recording:
                    UpdateRecording();
                    break;
                    
                case GhostState.Playing:
                    UpdatePlayback();
                    break;
            }
        }
        
        private void UpdateRecording()
        {
            if (playerCar == null) return;
            
            recordTimer += Time.deltaTime;
            totalRecordedTime += Time.deltaTime;
            
            if (recordTimer >= recordInterval)
            {
                recordTimer -= recordInterval;
                
                Rigidbody rb = playerCar.GetComponent<Rigidbody>();
                recordedFrames.Add(new GhostFrame(playerCar.transform, rb, playerCar, totalRecordedTime));
                
                // Trim if exceeding max frames
                if (recordedFrames.Count > maxRecordedFrames)
                {
                    recordedFrames.RemoveAt(0);
                }
            }
        }
        
        private void UpdatePlayback()
        {
            if (ghostTransform == null || bestLapFrames.Count == 0)
            {
                StopPlayback();
                return;
            }
            
            playbackTimer += Time.deltaTime;
            
            // Find current frame
            while (playbackIndex < bestLapFrames.Count - 1 && 
                   bestLapFrames[playbackIndex + 1].timestamp <= playbackTimer)
            {
                playbackIndex++;
            }
            
            // If we've reached the end
            if (playbackIndex >= bestLapFrames.Count - 1)
            {
                // Loop the ghost
                playbackTimer = 0f;
                playbackIndex = 0;
            }
            
            // Interpolate between frames for smooth motion
            if (playbackIndex < bestLapFrames.Count - 1)
            {
                GhostFrame current = bestLapFrames[playbackIndex];
                GhostFrame next = bestLapFrames[playbackIndex + 1];
                
                float duration = next.timestamp - current.timestamp;
                float t = duration > 0 ? (playbackTimer - current.timestamp) / duration : 0;
                t = Mathf.Clamp01(t);
                
                ghostTransform.position = Vector3.Lerp(current.position, next.position, t);
                ghostTransform.rotation = Quaternion.Slerp(current.rotation, next.rotation, t);
            }
            else
            {
                GhostFrame frame = bestLapFrames[playbackIndex];
                ghostTransform.position = frame.position;
                ghostTransform.rotation = frame.rotation;
            }
        }
        
        private List<Vector3> GetPositionList(List<GhostFrame> frames)
        {
            var list = new List<Vector3>();
            foreach (var f in frames) list.Add(f.position);
            return list;
        }
        
        private List<Quaternion> GetRotationList(List<GhostFrame> frames)
        {
            var list = new List<Quaternion>();
            foreach (var f in frames) list.Add(f.rotation);
            return list;
        }
        
        /// <summary>
        /// Save ghost data to PlayerPrefs (compressed format for mobile)
        /// </summary>
        public void SaveGhostData(string trackKey)
        {
            if (bestLapFrames.Count == 0) return;
            
            // Store key frames only (every 10th frame) to save space
            var keyFrames = new List<GhostFrame>();
            for (int i = 0; i < bestLapFrames.Count; i += 10)
                keyFrames.Add(bestLapFrames[i]);
            
            string json = JsonUtility.ToJson(new GhostDataWrapper { 
                bestTime = bestLapTime, 
                frameCount = keyFrames.Count 
            });
            
            PlayerPrefs.SetString($"Ghost_{trackKey}", json);
            PlayerPrefs.SetFloat($"GhostTime_{trackKey}", bestLapTime);
            PlayerPrefs.Save();
        }
        
        [System.Serializable]
        private class GhostDataWrapper
        {
            public float bestTime;
            public int frameCount;
        }
    }
}
