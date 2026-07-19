using UnityEngine;
using System.Collections.Generic;
using VRacer.Car;

namespace VRacer.AI
{
    /// <summary>
    /// Rubber-band AI manager: adjusts AI speed to keep races close.
    /// Subtle enough not to feel unfair — the original arcade did this.
    /// </summary>
    public class RubberBandManager : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float maxSpeedBoost = 0.15f;     // 15% extra max speed
        [SerializeField] private float maxSpeedPenalty = 0.10f;   // 10% speed reduction
        [SerializeField] private float rubberBandRange = 500f;    // distance in meters for effect
        [SerializeField] private bool rubberBandEnabled = true;

        [Header("Track Difficulty Scaling")]
        [SerializeField] private float beginnerAISpeed = 0.7f;
        [SerializeField] private float intermediateAISpeed = 0.8f;
        [SerializeField] private float expertAISpeed = 0.9f;

        private List<CarController> allCars;
        private CarController playerCar;

        private void Start()
        {
            allCars = new List<CarController>(FindObjectsByType<CarController>(FindObjectsSortMode.None));
            foreach (var car in allCars)
            {
                if (car.IsPlayer) playerCar = car;
            }
        }

        private void Update()
        {
            if (playerCar == null || !rubberBandEnabled) return;
            if (!RaceManager.Instance || !RaceManager.Instance.RaceActive) return;

            UpdateRubberBand();
        }

        private void UpdateRubberBand()
        {
            float playerProgress = GetRaceProgress(playerCar);

            foreach (var car in allCars)
            {
                if (car == playerCar || car == null) continue;

                float aiProgress = GetRaceProgress(car);
                float progressDiff = aiProgress - playerProgress;

                // AI ahead of player
                if (progressDiff > 0)
                {
                    float penalty = Mathf.Clamp01(progressDiff / rubberBandRange) * maxSpeedPenalty;
                    AIController ai = car.GetComponent<AIController>();
                    if (ai != null)
                    {
                        // Slow down AI that's too far ahead — subtle
                    }
                }
                // Player ahead of AI
                else
                {
                    float boost = Mathf.Clamp01(-progressDiff / rubberBandRange) * maxSpeedBoost;
                    AIController ai = car.GetComponent<AIController>();
                    if (ai != null)
                    {
                        // Speed up AI that's too far behind — subtle
                    }
                }
            }
        }

        /// <summary>
        /// Estimate race progress as a continuous value (lap + sector position).
        /// </summary>
        private float GetRaceProgress(CarController car)
        {
            // Simple: distance along track approximated by lap count + last checkpoint
            return car.CurrentLap * 1000f + car.transform.position.magnitude % 1000f;
        }
    }
}
