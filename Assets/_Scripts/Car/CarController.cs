using UnityEngine;
using VRacer.Core;

namespace VRacer.Car
{
    /// <summary>
    /// Arcade-style F1 car controller.
    /// NOT a simulation — fast pickup, responsive steering,
    /// forgiving handling. "On rails" with room for skill.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class CarController : MonoBehaviour
    {
        [Header("Car Identity")]
        [SerializeField] private int carNumber = 1;
        [SerializeField] private string driverName = "PLAYER";
        [SerializeField] private Color carColor = Color.red;
        [SerializeField] private bool isPlayer = false;

        [Header("Engine")]
        [SerializeField] private float maxSpeed = 320f;        // km/h
        [SerializeField] private float acceleration = 150f;     // km/h per second
        [SerializeField] private float engineBraking = 30f;
        [SerializeField] private AnimationCurve powerCurve;     // power vs speed ratio

        [Header("Transmission")]
        [SerializeField] private int currentGear = 1;
        [SerializeField] private int maxGears = 4;
        [SerializeField] private float[] gearRatios = { 3.5f, 2.2f, 1.5f, 1.0f, 0.8f }; // R,1,2,3,4
        [SerializeField] private float shiftUpRPM = 0.85f;
        [SerializeField] private float shiftDownRPM = 0.35f;
        [SerializeField] private float shiftDelay = 0.2f;
        private float lastShiftTime = -1f;

        [Header("Steering")]
        [SerializeField] private float maxSteerAngle = 35f;
        [SerializeField] private float steerSpeed = 8f;
        [SerializeField] private float steerReturnSpeed = 6f;
        [SerializeField] private float highSpeedSteerFalloff = 0.5f; // Less steering at high speed

        [Header("Braking")]
        [SerializeField] private float brakeForce = 200f;       // km/h per second
        [SerializeField] private float handbrakeForce = 150f;

        [Header("Grip & Drift")]
        [SerializeField] private float baseGrip = 0.95f;
        [SerializeField] private float driftGrip = 0.60f;
        [SerializeField] private float driftThreshold = 0.7f;   // lateral force ratio to trigger drift
        [SerializeField] private float driftRecoverySpeed = 2f;

        [Header("Damage")]
        [SerializeField] private float maxDamage = 100f;
        [SerializeField] private float currentDamage = 0f;
        [SerializeField] private float collisionDamageMultiplier = 10f;
        [SerializeField] private float damageSpeedReduction = 0.3f; // 30% max speed loss at full damage

        [Header("Race State")]
        [SerializeField] private int currentLap = 0;
        [SerializeField] private int racePosition = 1;
        [SerializeField] private int lastCheckpoint = -1;
        [SerializeField] private float lapStartTime = 0f;

        // Internal state
        private Rigidbody rb;
        private float currentSpeed;       // km/h
        private float currentSteerAngle;
        private float targetSteerAngle;
        private float currentGrip;
        private bool isDrifting;
        private float driftRecovery;

        // Component refs
        private CarDamage carDamage;
        private CarAudio carAudio;

        // Input values (set by InputManager)
        [HideInInspector] public float inputSteer = 0f;     // -1 to 1
        [HideInInspector] public float inputAccelerate = 0f; // 0 to 1
        [HideInInspector] public float inputBrake = 0f;      // 0 to 1
        [HideInInspector] public bool inputShiftUp = false;
        [HideInInspector] public bool inputShiftDown = false;

        // Public accessors
        public int CarNumber => carNumber;
        public float CurrentSpeed => currentSpeed;
        public float CurrentSpeedMS => currentSpeed / 3.6f;
        public int CurrentGear => currentGear;
        public int RacePosition => racePosition;
        public bool IsPlayer => isPlayer;
        public float DamagePercent => currentDamage / maxDamage;
        public bool IsDrifting => isDrifting;
        public int CurrentLap => currentLap;
        public float MaxSpeed => maxSpeed * (1f - DamagePercent * damageSpeedReduction);

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            carDamage = GetComponent<CarDamage>();
            carAudio = GetComponent<CarAudio>();

            if (powerCurve == null || powerCurve.keys.Length == 0)
            {
                powerCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0.3f);
            }

            currentGrip = baseGrip;
        }

        private void FixedUpdate()
        {
            if (!RaceManager.Instance || !RaceManager.Instance.RaceActive)
            {
                // Still allow physics but no input
                rb.linearVelocity *= 0.99f; // gentle slow-down
                return;
            }

            // Speed in km/h
            currentSpeed = rb.linearVelocity.magnitude * 3.6f;

            // Apply steering
            UpdateSteering();

            // Apply acceleration/braking
            UpdateEngine();

            // Apply transmission (manual mode)
            UpdateTransmission();

            // Apply grip and drift physics
            ApplyGrip();

            // Apply damage effects
            ApplyDamageEffects();
        }

        // ============================================================
        // STEERING
        // ============================================================

        private void UpdateSteering()
        {
            targetSteerAngle = inputSteer * maxSteerAngle;

            // Reduce steering at high speeds
            float speedRatio = Mathf.Clamp01(currentSpeed / 200f);
            float speedFactor = Mathf.Lerp(1f, highSpeedSteerFalloff, speedRatio);
            targetSteerAngle *= speedFactor;

            // Smooth steering
            float steerRate = Mathf.Abs(targetSteerAngle) > Mathf.Abs(currentSteerAngle) ? steerSpeed : steerReturnSpeed;
            currentSteerAngle = Mathf.MoveTowards(currentSteerAngle, targetSteerAngle, steerRate * Time.fixedDeltaTime * 60f);

            // Apply rotation
            if (currentSpeed > 1f)
            {
                float turnRadius = 15f; // wheelbase approximation
                float angularVelocity = (currentSpeed / 3.6f) / turnRadius * Mathf.Sin(currentSteerAngle * Mathf.Deg2Rad);
                rb.angularVelocity = new Vector3(0f, angularVelocity, 0f);
            }
        }

        // ============================================================
        // ENGINE & ACCELERATION
        // ============================================================

        private void UpdateEngine()
        {
            if (inputAccelerate > 0.01f)
            {
                float effectiveMaxSpeed = MaxSpeed;
                float speedRatio = Mathf.Clamp01(currentSpeed / effectiveMaxSpeed);
                float powerFactor = powerCurve.Evaluate(speedRatio);
                float gearRatio = GetCurrentGearRatio();

                float force = acceleration * inputAccelerate * powerFactor * gearRatio;
                rb.AddForce(transform.forward * force * (1000f / 3.6f) * Time.fixedDeltaTime, ForceMode.Acceleration);
            }
            else
            {
                // Engine braking when off throttle
                float brakeAmount = engineBraking * (1000f / 3.6f) * Time.fixedDeltaTime;
                Vector3 forwardVelocity = Vector3.Project(rb.linearVelocity, transform.forward);
                if (forwardVelocity.magnitude > 0.5f)
                {
                    rb.AddForce(-forwardVelocity.normalized * brakeAmount, ForceMode.Acceleration);
                }
            }

            // Braking
            if (inputBrake > 0.01f)
            {
                Vector3 forwardVelocity = Vector3.Project(rb.linearVelocity, transform.forward);
                if (forwardVelocity.magnitude > 0.1f)
                {
                    float brakeAmount = brakeForce * inputBrake * (1000f / 3.6f) * Time.fixedDeltaTime;
                    rb.AddForce(-forwardVelocity.normalized * brakeAmount, ForceMode.Acceleration);
                }
            }

            // Speed cap
            if (currentSpeed > MaxSpeed)
            {
                rb.linearVelocity = rb.linearVelocity.normalized * (MaxSpeed / 3.6f);
            }
        }

        // ============================================================
        // TRANSMISSION
        // ============================================================

        private void UpdateTransmission()
        {
            if (!GameManager.Instance || !GameManager.Instance.IsManualTransmission) return;
            if (Time.time - lastShiftTime < shiftDelay) return;

            float speedRatio = currentSpeed / MaxSpeed;

            if (inputShiftUp && currentGear < maxGears)
            {
                currentGear++;
                lastShiftTime = Time.time;
                inputShiftUp = false;
            }
            else if (inputShiftDown && currentGear > 0) // 0 = reverse
            {
                currentGear--;
                lastShiftTime = Time.time;
                inputShiftDown = false;
            }

            // Auto-upshift if over-revving (even in manual, as a safety)
            if (speedRatio > shiftUpRPM && currentGear < maxGears)
            {
                // Don't auto-shift in manual — let the player blow the engine (arcade-style, no mechanical damage)
            }
        }

        private float GetCurrentGearRatio()
        {
            int index = currentGear + 1; // +1 because gearRatios[0] is reverse
            if (index < 0) index = 0;
            if (index >= gearRatios.Length) index = gearRatios.Length - 1;
            return gearRatios[index];
        }

        // ============================================================
        // GRIP & DRIFT
        // ============================================================

        private void ApplyGrip()
        {
            // Calculate lateral force
            Vector3 forwardDir = transform.forward;
            Vector3 lateralDir = transform.right;
            float lateralSpeed = Vector3.Dot(rb.linearVelocity, lateralDir);

            // Check if drifting
            float lateralRatio = Mathf.Abs(lateralSpeed) / Mathf.Max(currentSpeed / 3.6f, 0.1f);
            isDrifting = lateralRatio > driftThreshold && currentSpeed > 30f;

            currentGrip = isDrifting
                ? Mathf.MoveTowards(currentGrip, driftGrip, Time.fixedDeltaTime * 1f)
                : Mathf.MoveTowards(currentGrip, baseGrip, Time.fixedDeltaTime * driftRecoverySpeed);

            // Apply lateral friction
            float gripForce = Mathf.Abs(lateralSpeed) * currentGrip * 100f;
            rb.AddForce(-lateralDir * Mathf.Sign(lateralSpeed) * gripForce * Time.fixedDeltaTime, ForceMode.Acceleration);
        }

        // ============================================================
        // COLLISIONS & DAMAGE
        // ============================================================

        private void OnCollisionEnter(Collision collision)
        {
            float impactSpeed = collision.relativeVelocity.magnitude;
            float damage = impactSpeed * collisionDamageMultiplier;
            ApplyDamage(damage);

            // Bounce off walls
            if (collision.contacts.Length > 0)
            {
                Vector3 normal = collision.contacts[0].normal;
                Vector3 reflection = Vector3.Reflect(rb.linearVelocity.normalized, normal);
                rb.linearVelocity = reflection * Mathf.Min(currentSpeed / 3.6f, impactSpeed * 0.5f);
            }
        }

        public void ApplyDamage(float amount)
        {
            currentDamage = Mathf.Min(currentDamage + amount, maxDamage);
            carDamage?.ApplyDamage(currentDamage / maxDamage);
        }

        private void ApplyDamageEffects()
        {
            // Damage reduces top speed (handled in MaxSpeed property)
        }

        // ============================================================
        // RACE STATE (managed by RaceManager + CheckpointManager)
        // ============================================================

        public void OnCheckpointPassed(int checkpointIndex)
        {
            lastCheckpoint = checkpointIndex;
        }

        public void OnLapCompleted()
        {
            currentLap++;
        }

        public void SetRacePosition(int position)
        {
            racePosition = position;
        }

        public void ResetRaceState()
        {
            currentLap = 0;
            racePosition = 16;
            lastCheckpoint = -1;
            currentDamage = 0f;
            lapStartTime = Time.time;
        }

        // ============================================================
        // VISUAL
        // ============================================================

        public Color GetCarColor() => carColor;
        public string GetDriverName() => driverName;

        private void OnDrawGizmos()
        {
            // Draw forward vector
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, transform.forward * 3f);

            // Draw velocity
            if (rb != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawRay(transform.position, rb.linearVelocity.normalized * 3f);
            }
        }
    }
}
