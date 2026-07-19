using UnityEngine;
using UnityEngine.InputSystem;
using VRacer.Car;
using VRacer.Core;

namespace VRacer.Input
{
    /// <summary>
    /// Unified input manager supporting:
    /// - Tilt steering (accelerometer)
    /// - On-screen touch controls
    /// - Bluetooth gamepad
    /// Designed for the "XArcade Edition" — works with arcade sticks too.
    /// </summary>
    public class InputManager : MonoBehaviour
    {
        public static InputManager Instance { get; private set; }

        [Header("Tilt Settings")]
        [SerializeField] private bool tiltEnabled = true;
        [SerializeField] private float tiltSensitivity = 2.0f;
        [SerializeField] private float tiltDeadZone = 0.05f;
        [SerializeField] private float tiltSmoothing = 5f;
        [SerializeField] private AnimationCurve tiltCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Calibration")]
        [SerializeField] private Vector3 calibratedZero;
        [SerializeField] private bool isCalibrated = false;

        [Header("Touch Zones (Screen-Space)")]
        [SerializeField] private Rect accelerateZone = new Rect(0.5f, 0.4f, 0.5f, 0.6f);
        [SerializeField] private Rect brakeZone = new Rect(0.5f, 0.0f, 0.5f, 0.4f);
        [SerializeField] private Rect steerZone = new Rect(0.0f, 0.0f, 0.5f, 1.0f);

        [Header("Virtual Steering Wheel")]
        [SerializeField] private bool useVirtualWheel = false;
        [SerializeField] private float wheelSensitivity = 0.5f;

        // Input state
        private float smoothedTilt;
        private float smoothedSteerInput;
        private bool isTouchingAccelerate;
        private bool isTouchingBrake;
        private float touchSteerValue;
        private Vector2 lastTouchPosition;
        private int accelerateFingerId = -1;
        private int brakeFingerId = -1;
        private int steerFingerId = -1;

        // Gamepad state
        private float gamepadSteer;
        private float gamepadThrottle;
        private float gamepadBrake;

        // Target car
        private CarController targetCar;

        // Input Actions
        private PlayerInputActions inputActions;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            inputActions = new PlayerInputActions();
        }

        private void Start()
        {
            // Auto-calibrate tilt on start
            if (SystemInfo.supportsAccelerometer)
            {
                CalibrateTilt();
            }
        }

        private void OnEnable()
        {
            inputActions.Enable();

            // Gamepad bindings
            inputActions.Driving.Steer.performed += OnGamepadSteer;
            inputActions.Driving.Steer.canceled += OnGamepadSteer;
            inputActions.Driving.Accelerate.performed += OnGamepadAccelerate;
            inputActions.Driving.Accelerate.canceled += OnGamepadAccelerate;
            inputActions.Driving.Brake.performed += OnGamepadBrake;
            inputActions.Driving.Brake.canceled += OnGamepadBrake;
            inputActions.Driving.ShiftUp.performed += OnShiftUp;
            inputActions.Driving.ShiftDown.performed += OnShiftDown;
            inputActions.Driving.CameraCycle.performed += OnCameraCycle;
            inputActions.UI.Tap.performed += OnAnyTap;
        }

        private void OnDisable()
        {
            inputActions.Driving.Steer.performed -= OnGamepadSteer;
            inputActions.Driving.Steer.canceled -= OnGamepadSteer;
            inputActions.Driving.Accelerate.performed -= OnGamepadAccelerate;
            inputActions.Driving.Accelerate.canceled -= OnGamepadAccelerate;
            inputActions.Driving.Brake.performed -= OnGamepadBrake;
            inputActions.Driving.Brake.canceled -= OnGamepadBrake;
            inputActions.Driving.ShiftUp.performed -= OnShiftUp;
            inputActions.Driving.ShiftDown.performed -= OnShiftDown;
            inputActions.Driving.CameraCycle.performed -= OnCameraCycle;
            inputActions.UI.Tap.performed -= OnAnyTap;

            inputActions.Disable();
        }

        private void Update()
        {
            // Tilt input
            if (tiltEnabled && SystemInfo.supportsAccelerometer)
            {
                UpdateTiltInput();
            }

            // Touch input
            UpdateTouchInput();

            // Apply final steering to car
            if (targetCar != null && RaceManager.Instance != null && RaceManager.Instance.RaceActive)
            {
                ApplyInputToCar();
            }

            // Any input dismisses attract mode
            if (AnyInputThisFrame())
            {
                GameManager.Instance?.OnAnyInput();
            }
        }

        // ============================================================
        // TILT STEERING
        // ============================================================

        private void UpdateTiltInput()
        {
            Vector3 rawAccel = UnityEngine.Input.acceleration;

            if (!isCalibrated)
            {
                CalibrateTilt();
                return;
            }

            // Remove calibration offset
            Vector3 calibrated = rawAccel - calibratedZero;

            // Use X-axis tilt for steering
            float tiltX = calibrated.x;

            // Dead zone
            if (Mathf.Abs(tiltX) < tiltDeadZone) tiltX = 0f;

            // Apply curve
            float sign = Mathf.Sign(tiltX);
            tiltX = sign * tiltCurve.Evaluate(Mathf.Abs(tiltX) / 1f);

            // Sensitivity
            tiltX *= tiltSensitivity;

            // Smooth
            smoothedTilt = Mathf.Lerp(smoothedTilt, Mathf.Clamp(tiltX, -1f, 1f), tiltSmoothing * Time.deltaTime);
        }

        public void CalibrateTilt()
        {
            calibratedZero = UnityEngine.Input.acceleration;
            isCalibrated = true;
            Debug.Log($"[Input] Tilt calibrated: {calibratedZero}");
        }

        // ============================================================
        // TOUCH CONTROLS
        // ============================================================

        private void UpdateTouchInput()
        {
            if (UnityEngine.Input.touchCount == 0)
            {
                isTouchingAccelerate = false;
                isTouchingBrake = false;
                if (steerFingerId >= 0) steerFingerId = -1;
                return;
            }

            foreach (Touch touch in UnityEngine.Input.touches)
            {
                Vector2 normalizedPos = new Vector2(
                    touch.position.x / Screen.width,
                    touch.position.y / Screen.height
                );
                Vector2 normalizedPosYFlipped = new Vector2(normalizedPos.x, 1f - normalizedPos.y);

                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        HandleTouchBegan(touch.fingerId, normalizedPosYFlipped);
                        break;
                    case TouchPhase.Moved:
                    case TouchPhase.Stationary:
                        HandleTouchMoved(touch.fingerId, touch.position, normalizedPosYFlipped);
                        break;
                    case TouchPhase.Ended:
                    case TouchPhase.Canceled:
                        HandleTouchEnded(touch.fingerId);
                        break;
                }
            }
        }

        private void HandleTouchBegan(int fingerId, Vector2 normPos)
        {
            if (accelerateZone.Contains(normPos))
            {
                isTouchingAccelerate = true;
                accelerateFingerId = fingerId;
            }
            else if (brakeZone.Contains(normPos))
            {
                isTouchingBrake = true;
                brakeFingerId = fingerId;
            }
            else if (useVirtualWheel && steerZone.Contains(normPos))
            {
                steerFingerId = fingerId;
                lastTouchPosition = normPos;
            }
        }

        private void HandleTouchMoved(int fingerId, Vector2 screenPos, Vector2 normPos)
        {
            if (fingerId == steerFingerId && useVirtualWheel)
            {
                float delta = (normPos.x - lastTouchPosition.x) * wheelSensitivity * 2f;
                touchSteerValue = Mathf.Clamp(touchSteerValue + delta, -1f, 1f);
                lastTouchPosition = normPos;
            }
        }

        private void HandleTouchEnded(int fingerId)
        {
            if (fingerId == accelerateFingerId)
            {
                isTouchingAccelerate = false;
                accelerateFingerId = -1;
            }
            if (fingerId == brakeFingerId)
            {
                isTouchingBrake = false;
                brakeFingerId = -1;
            }
            if (fingerId == steerFingerId)
            {
                steerFingerId = -1;
                touchSteerValue = 0f; // Spring return to center
            }
        }

        // ============================================================
        // GAMEPAD INPUT
        // ============================================================

        private void OnGamepadSteer(InputAction.CallbackContext ctx)
        {
            gamepadSteer = ctx.ReadValue<float>();
        }

        private void OnGamepadAccelerate(InputAction.CallbackContext ctx)
        {
            gamepadThrottle = ctx.ReadValue<float>();
        }

        private void OnGamepadBrake(InputAction.CallbackContext ctx)
        {
            gamepadBrake = ctx.ReadValue<float>();
        }

        private void OnShiftUp(InputAction.CallbackContext ctx)
        {
            if (targetCar != null)
                targetCar.inputShiftUp = true;
        }

        private void OnShiftDown(InputAction.CallbackContext ctx)
        {
            if (targetCar != null)
                targetCar.inputShiftDown = true;
        }

        private void OnCameraCycle(InputAction.CallbackContext ctx)
        {
            CameraManager.Instance?.CycleCameraView();
        }

        private void OnAnyTap(InputAction.CallbackContext ctx)
        {
            GameManager.Instance?.OnAnyInput();
        }

        // ============================================================
        // APPLY TO CAR
        // ============================================================

        private void ApplyInputToCar()
        {
            if (targetCar == null) return;

            // Determine active steering source
            float steerInput = 0f;

            // Priority: Gamepad > Tilt > Virtual Wheel
            if (Mathf.Abs(gamepadSteer) > 0.01f)
            {
                steerInput = gamepadSteer;
            }
            else if (tiltEnabled && SystemInfo.supportsAccelerometer)
            {
                steerInput = smoothedTilt;
            }
            else if (useVirtualWheel)
            {
                steerInput = touchSteerValue;
            }

            smoothedSteerInput = Mathf.Lerp(smoothedSteerInput, steerInput, 10f * Time.deltaTime);

            targetCar.inputSteer = smoothedSteerInput;

            // Throttle: Gamepad trigger > Touch accelerate zone
            targetCar.inputAccelerate = Mathf.Max(gamepadThrottle, isTouchingAccelerate ? 1f : 0f);

            // Brake: Gamepad brake > Touch brake zone
            targetCar.inputBrake = Mathf.Max(gamepadBrake, isTouchingBrake ? 1f : 0f);
        }

        private bool AnyInputThisFrame()
        {
            return UnityEngine.Input.anyKeyDown ||
                   (UnityEngine.Input.touchCount > 0 &&
                    UnityEngine.Input.GetTouch(0).phase == TouchPhase.Began);
        }

        // ============================================================
        // PUBLIC METHODS
        // ============================================================

        public void SetTargetCar(CarController car)
        {
            targetCar = car;
        }

        public void SetTiltEnabled(bool enabled)
        {
            tiltEnabled = enabled;
        }

        public void SetTiltSensitivity(float sensitivity)
        {
            tiltSensitivity = sensitivity;
        }
    }
}
