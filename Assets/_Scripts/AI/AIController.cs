using UnityEngine;
using System.Collections.Generic;
using VRacer.Track;

namespace VRacer.AI
{
    /// <summary>
    /// AI opponent car controller.
    /// Follows waypoints with realistic racing lines.
    /// Makes mistakes, collides, and varies speed — not perfect.
    /// </summary>
    [RequireComponent(typeof(CarController))]
    public class AIController : MonoBehaviour
    {
        [Header("AI Personality")]
        [SerializeField] private AIDifficulty difficulty = AIDifficulty.Normal;
        [SerializeField] private float skillLevel = 0.5f;      // 0 = terrible, 1 = perfect

        [Header("Driving Parameters")]
        [SerializeField] private float lookAheadDistance = 50f;
        [SerializeField] private float corneringSlowdown = 0.7f;
        [SerializeField] private float brakingDistance = 40f;
        [SerializeField] private float mistakeProbability = 0.05f;  // per second

        [Header("Passing Behavior")]
        [SerializeField] private float passingAggression = 0.5f;
        [SerializeField] private float safeFollowDistance = 15f;

        private CarController carController;
        private TrackDefinition currentTrack;
        private List<Transform> waypoints;
        private int currentWaypointIndex = 0;
        private int targetWaypointIndex = 1;

        // Racing state
        private float currentMaxSpeed;
        private bool isBraking;
        private bool isRecovering;
        private float recoveryTimer;

        // Avoidance
        private CarController nearestOpponent;
        private float nearestOpponentDistance = float.MaxValue;

        private void Awake()
        {
            carController = GetComponent<CarController>();
        }

        private void Start()
        {
            currentTrack = FindFirstObjectByType<TrackDefinition>();
            if (currentTrack != null)
            {
                waypoints = currentTrack.Waypoints;
            }

            // Difficulty affects skill
            skillLevel = difficulty switch
            {
                AIDifficulty.Easy => 0.3f,
                AIDifficulty.Normal => 0.55f,
                AIDifficulty.Hard => 0.75f,
                AIDifficulty.Expert => 0.9f,
                _ => 0.5f
            };

            currentMaxSpeed = carController.MaxSpeed * skillLevel;
        }

        private void Update()
        {
            if (waypoints == null || waypoints.Count < 2) return;
            if (!RaceManager.Instance || !RaceManager.Instance.RaceActive) return;

            if (isRecovering)
            {
                recoveryTimer -= Time.deltaTime;
                if (recoveryTimer <= 0f) isRecovering = false;
                return;
            }

            // Random mistakes
            if (Random.value < mistakeProbability * Time.deltaTime)
            {
                MakeMistake();
            }

            // Find current waypoint progress
            UpdateWaypointProgress();

            // Scan for opponents
            ScanForOpponents();

            // Calculate steering
            float steer = CalculateSteering();

            // Calculate throttle/brake
            float throttle = CalculateThrottle();
            float brake = CalculateBrake();

            // Apply to car controller
            carController.inputSteer = steer;
            carController.inputAccelerate = throttle;
            carController.inputBrake = brake;

            // Auto transmission
            AutoShift();
        }

        // ============================================================
        // WAYPOINT NAVIGATION
        // ============================================================

        private void UpdateWaypointProgress()
        {
            // Find nearest waypoint (that we haven't passed yet)
            float minDist = float.MaxValue;
            int nearestIdx = currentWaypointIndex;

            for (int i = 0; i < waypoints.Count; i++)
            {
                if (waypoints[i] == null) continue;
                float dist = Vector3.Distance(transform.position, waypoints[i].position);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearestIdx = i;
                }
            }

            currentWaypointIndex = nearestIdx;
            targetWaypointIndex = (currentWaypointIndex + (int)(lookAheadDistance / 10f)) % waypoints.Count;
        }

        private float CalculateSteering()
        {
            if (waypoints[targetWaypointIndex] == null) return 0f;

            // Get direction to target waypoint
            Vector3 targetPos = waypoints[targetWaypointIndex].position;
            Vector3 toTarget = targetPos - transform.position;
            Vector3 localTarget = transform.InverseTransformPoint(targetPos);

            // Calculate desired steering angle
            float angle = Mathf.Atan2(localTarget.x, localTarget.z) * Mathf.Rad2Deg;
            float steer = Mathf.Clamp(angle / 35f, -1f, 1f);

            // Add noise based on skill level (lower skill = more wobble)
            steer += Random.Range(-0.05f, 0.05f) * (1f - skillLevel);

            return steer;
        }

        private float CalculateThrottle()
        {
            if (waypoints[targetWaypointIndex] == null) return 0f;

            // Predict corner sharpness
            float cornerSharpness = PredictCornerSharpness();

            // Slow down for tight corners
            float speedFactor = 1f;
            if (cornerSharpness > 30f)
            {
                speedFactor = Mathf.Lerp(1f, corneringSlowdown, cornerSharpness / 90f);
            }

            // Brake for tight corners
            if (cornerSharpness > 60f && carController.CurrentSpeed > carController.MaxSpeed * 0.5f)
            {
                return 0f; // Off throttle for braking
            }

            // Avoid hitting car in front
            if (nearestOpponent != null && nearestOpponentDistance < safeFollowDistance)
            {
                speedFactor *= 0.7f;
            }

            return Mathf.Clamp01(speedFactor * skillLevel);
        }

        private float CalculateBrake()
        {
            float cornerSharpness = PredictCornerSharpness();
            float speedRatio = carController.CurrentSpeed / carController.MaxSpeed;

            if (cornerSharpness > 70f && speedRatio > 0.6f)
            {
                return Mathf.Clamp01(cornerSharpness / 90f);
            }

            // Brake if opponent too close
            if (nearestOpponent != null && nearestOpponentDistance < safeFollowDistance * 0.5f)
            {
                return 0.5f;
            }

            return 0f;
        }

        /// <summary>
        /// Predict how sharp the upcoming corner is by checking angle
        /// between the next few waypoints.
        /// </summary>
        private float PredictCornerSharpness()
        {
            float maxAngle = 0f;
            int lookAhead = 3;

            for (int i = 0; i < lookAhead; i++)
            {
                int idx1 = (currentWaypointIndex + i) % waypoints.Count;
                int idx2 = (currentWaypointIndex + i + 1) % waypoints.Count;
                int idx3 = (currentWaypointIndex + i + 2) % waypoints.Count;

                if (waypoints[idx1] == null || waypoints[idx2] == null || waypoints[idx3] == null)
                    continue;

                Vector3 dir1 = (waypoints[idx2].position - waypoints[idx1].position).normalized;
                Vector3 dir2 = (waypoints[idx3].position - waypoints[idx2].position).normalized;
                float angle = Vector3.Angle(dir1, dir2);
                if (angle > maxAngle) maxAngle = angle;
            }

            return maxAngle;
        }

        // ============================================================
        // OPPONENT SCANNING
        // ============================================================

        private void ScanForOpponents()
        {
            nearestOpponent = null;
            nearestOpponentDistance = float.MaxValue;

            var allCars = FindObjectsByType<CarController>(FindObjectsSortMode.None);
            foreach (var car in allCars)
            {
                if (car == carController) continue;
                float dist = Vector3.Distance(transform.position, car.transform.position);
                if (dist < nearestOpponentDistance)
                {
                    nearestOpponentDistance = dist;
                    nearestOpponent = car;
                }
            }
        }

        // ============================================================
        // TRANSMISSION (Auto)
        // ============================================================

        private void AutoShift()
        {
            if (GameManager.Instance != null && GameManager.Instance.IsManualTransmission) return;

            float speedRatio = carController.CurrentSpeed / carController.MaxSpeed;
            int currentGear = carController.CurrentGear;

            if (speedRatio > 0.8f && currentGear < 4)
            {
                carController.inputShiftUp = true;
            }
            else if (speedRatio < 0.3f && currentGear > 1)
            {
                carController.inputShiftDown = true;
            }
        }

        // ============================================================
        // MISTAKES (for realism — AI shouldn't be perfect)
        // ============================================================

        private void MakeMistake()
        {
            int mistakeType = Random.Range(0, 4);
            switch (mistakeType)
            {
                case 0: // Late braking — overshoot corner
                    isBraking = false;
                    Invoke(nameof(StartRecovery), 0.5f);
                    break;
                case 1: // Slight wobble
                    carController.inputSteer += Random.Range(-0.3f, 0.3f);
                    break;
                case 2: // Lift off throttle briefly
                    carController.inputAccelerate = 0f;
                    Invoke(nameof(RestoreThrottle), 0.3f);
                    break;
                case 3: // Take a bad line
                    targetWaypointIndex = (targetWaypointIndex + 2) % waypoints.Count;
                    break;
            }
        }

        private void StartRecovery()
        {
            isRecovering = true;
            recoveryTimer = 0.5f;
        }

        private void RestoreThrottle()
        {
            carController.inputAccelerate = 1f;
        }

        private void OnDrawGizmos()
        {
            if (waypoints == null || targetWaypointIndex >= waypoints.Count)
                return;

            Gizmos.color = Color.red;
            if (waypoints[targetWaypointIndex] != null)
            {
                Gizmos.DrawWireSphere(waypoints[targetWaypointIndex].position, 2f);
                Gizmos.DrawLine(transform.position, waypoints[targetWaypointIndex].position);
            }
        }
    }

    public enum AIDifficulty
    {
        Easy,
        Normal,
        Hard,
        Expert
    }
}
