using UnityEngine;
using VRacer.Car;

namespace VRacer.Camera
{
    /// <summary>
    /// V.R. View System — 4 camera angles from the original arcade:
    /// 1. Close Chase (tight behind car, default arcade feel)
    /// 2. Far Chase (pulled back, wider view)
    /// 3. Nose/Bumper Cam (low to ground, sense of speed)
    /// 4. Cockpit/Hood Cam (over-the-nose view)
    /// </summary>
    public class CameraManager : MonoBehaviour
    {
        public static CameraManager Instance { get; private set; }

        public enum CameraView
        {
            CloseChase = 0,
            FarChase = 1,
            BumperCam = 2,
            HoodCam = 3
        }

        [Header("Current View")]
        [SerializeField] private CameraView currentView = CameraView.CloseChase;

        [Header("Camera References")]
        [SerializeField] private UnityEngine.Camera mainCamera;

        [Header("Close Chase Settings")]
        [SerializeField] private Vector3 closeChaseOffset = new Vector3(0f, 2.5f, -6f);
        [SerializeField] private float closeChaseFOV = 70f;

        [Header("Far Chase Settings")]
        [SerializeField] private Vector3 farChaseOffset = new Vector3(0f, 4f, -12f);
        [SerializeField] private float farChaseFOV = 60f;

        [Header("Bumper Cam Settings")]
        [SerializeField] private Vector3 bumperOffset = new Vector3(0f, 0.3f, 1.5f);
        [SerializeField] private float bumperFOV = 85f;

        [Header("Hood Cam Settings")]
        [SerializeField] private Vector3 hoodOffset = new Vector3(0f, 1.2f, 2.5f);
        [SerializeField] private float hoodFOV = 75f;

        [Header("Camera Smoothing")]
        [SerializeField] private float positionSmoothTime = 0.1f;
        [SerializeField] private float rotationSmoothTime = 0.08f;

        [Header("Look-Ahead")]
        [SerializeField] private float lookAheadDistance = 10f;
        [SerializeField] private float lookAheadSpeed = 3f;

        // Internal state
        private CarController targetCar;
        private Vector3 currentVelocity;
        private Vector3 rotationVelocity;
        private Vector3 smoothedLookAhead;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (mainCamera == null)
                mainCamera = UnityEngine.Camera.main;
        }

        private void LateUpdate()
        {
            if (targetCar == null)
            {
                FindPlayerCar();
                return;
            }

            // Calculate target position based on current view
            Vector3 targetPosition = CalculateTargetPosition();
            Quaternion targetRotation = CalculateTargetRotation(targetPosition);

            // Smooth camera movement
            transform.position = Vector3.SmoothDamp(
                transform.position, targetPosition,
                ref currentVelocity, positionSmoothTime
            );
            transform.rotation = Quaternion.Slerp(
                transform.rotation, targetRotation,
                rotationSmoothTime
            );
        }

        private Vector3 CalculateTargetPosition()
        {
            Vector3 offset = currentView switch
            {
                CameraView.CloseChase => closeChaseOffset,
                CameraView.FarChase => farChaseOffset,
                CameraView.BumperCam => bumperOffset,
                CameraView.HoodCam => hoodOffset,
                _ => closeChaseOffset
            };

            // Look-ahead: camera leads slightly in the direction of travel
            Vector3 lookAhead = Vector3.zero;
            if (currentView != CameraView.BumperCam)
            {
                lookAhead = targetCar.transform.forward * lookAheadDistance *
                    (targetCar.CurrentSpeed / targetCar.MaxSpeed);
                smoothedLookAhead = Vector3.Lerp(smoothedLookAhead, lookAhead,
                    lookAheadSpeed * Time.deltaTime);
            }

            // World-space offset
            Vector3 worldOffset = targetCar.transform.TransformDirection(offset);

            return targetCar.transform.position + worldOffset + smoothedLookAhead;
        }

        private Quaternion CalculateTargetRotation(Vector3 targetPosition)
        {
            // Look at the car from behind (or ahead for bumper cam)
            Vector3 lookTarget = targetCar.transform.position;
            if (currentView != CameraView.BumperCam)
            {
                lookTarget += targetCar.transform.forward * 5f + Vector3.up * 1f;
            }

            return Quaternion.LookRotation(lookTarget - targetPosition);
        }

        private void FindPlayerCar()
        {
            var cars = FindObjectsByType<CarController>(FindObjectsSortMode.None);
            foreach (var car in cars)
            {
                if (car.IsPlayer)
                {
                    targetCar = car;
                    break;
                }
            }
        }

        // ============================================================
        // PUBLIC API
        // ============================================================

        public void CycleCameraView()
        {
            currentView = (CameraView)(((int)currentView + 1) % 4);
            UpdateCameraFOV();
            Debug.Log($"[Camera] View: {currentView}");
        }

        public void SetCameraView(CameraView view)
        {
            currentView = view;
            UpdateCameraFOV();
        }

        private void UpdateCameraFOV()
        {
            if (mainCamera == null) return;

            mainCamera.fieldOfView = currentView switch
            {
                CameraView.CloseChase => closeChaseFOV,
                CameraView.FarChase => farChaseFOV,
                CameraView.BumperCam => bumperFOV,
                CameraView.HoodCam => hoodFOV,
                _ => closeChaseFOV
            };
        }

        public CameraView GetCurrentView() => currentView;
    }
}
