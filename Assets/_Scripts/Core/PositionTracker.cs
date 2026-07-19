using UnityEngine;
using System.Collections.Generic;
using VRacer.Car;

namespace VRacer.Core
{
    /// <summary>
    /// Tracks real-time race positions of all 16 cars.
    /// Position is determined by: current lap > last checkpoint index > distance to next checkpoint.
    /// Updates the race position on each car's CarController.
    /// </summary>
    public class PositionTracker : MonoBehaviour
    {
        public static PositionTracker Instance { get; private set; }

        private List<CarController> allCars;
        private List<Track.CheckpointTrigger> checkpoints;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            allCars = new List<CarController>(FindObjectsByType<CarController>(FindObjectsSortMode.None));
            checkpoints = new List<Track.CheckpointTrigger>(
                FindObjectsByType<Track.CheckpointTrigger>(FindObjectsSortMode.None)
            );
            checkpoints.Sort((a, b) => a.CheckpointIndex.CompareTo(b.CheckpointIndex));
        }

        private void Update()
        {
            if (!RaceManager.Instance || !RaceManager.Instance.RaceActive) return;

            UpdatePositions();
        }

        private void UpdatePositions()
        {
            // Calculate race progress for each car
            var progressList = new List<(CarController car, float progress)>();
            for (int i = allCars.Count - 1; i >= 0; i--)
            {
                if (allCars[i] == null)
                {
                    allCars.RemoveAt(i);
                    continue;
                }
                float progress = CalculateProgress(allCars[i]);
                progressList.Add((allCars[i], progress));
            }

            // Sort by progress (descending)
            progressList.Sort((a, b) => b.progress.CompareTo(a.progress));

            // Assign positions
            for (int i = 0; i < progressList.Count; i++)
            {
                progressList[i].car.SetRacePosition(i + 1);
            }
        }

        private float CalculateProgress(CarController car)
        {
            // Progress = lap * 10000 + checkpointIndex * 1000 + distanceToNextCheckpoint
            float progress = car.CurrentLap * 10000f;

            // Find nearest checkpoint
            float minDist = float.MaxValue;
            int nearestIdx = 0;

            for (int i = 0; i < checkpoints.Count; i++)
            {
                if (checkpoints[i] == null) continue;
                float dist = Vector3.Distance(car.transform.position, checkpoints[i].transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearestIdx = i;
                }
            }

            progress += nearestIdx * 1000f;

            // Distance to next checkpoint (higher = further along, which is good for progress within the sector)
            int nextIdx = (nearestIdx + 1) % checkpoints.Count;
            if (checkpoints[nextIdx] != null)
            {
                float distToNext = Vector3.Distance(car.transform.position, checkpoints[nextIdx].transform.position);
                progress += (1000f - Mathf.Min(distToNext, 999f)); // Invert so closer to next = higher progress
            }

            return progress;
        }
    }
}
