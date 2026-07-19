using UnityEngine;
using System.Collections.Generic;
using VRacer.Core;

namespace VRacer.GameModes
{
    /// <summary>
    /// Grand Prix mode: race all 3 tracks in sequence.
    /// Cumulative points determine overall winner.
    /// Big Forest → Bay Bridge → Acropolis
    /// </summary>
    public class GrandPrixManager : MonoBehaviour
    {
        public static GrandPrixManager Instance { get; private set; }

        [Header("Track Order")]
        [SerializeField] private int[] trackOrder = { 0, 1, 2 }; // Big Forest, Bay Bridge, Acropolis
        [SerializeField] private int currentTrackIndex = 0;

        [Header("Points System")]
        [SerializeField] private int[] positionPoints = { 25, 18, 15, 12, 10, 8, 6, 4, 2, 1 };

        // State
        private struct DriverStanding
        {
            public string driverName;
            public int points;
            public int bestPosition;
            public float totalTime;
        }

        private List<DriverStanding> standings = new List<DriverStanding>();
        private bool isActive = false;

        private void Awake()
        {
            Instance = this;
        }

        public void StartGrandPrix()
        {
            currentTrackIndex = 0;
            standings.Clear();
            isActive = true;

            // Initialize standings for 16 drivers
            for (int i = 0; i < 16; i++)
            {
                standings.Add(new DriverStanding
                {
                    driverName = $"DRIVER {i + 1}",
                    points = 0,
                    bestPosition = 16,
                    totalTime = 0f
                });
            }

            // Start first track
            LoadNextTrack();
        }

        public void OnTrackComplete(Dictionary<int, float> raceResults)
        {
            if (!isActive) return;

            // Award points
            int pos = 1;
            foreach (var result in raceResults)
            {
                int driverIndex = result.Key;
                float time = result.Value;

                if (driverIndex < standings.Count)
                {
                    var standing = standings[driverIndex];
                    standing.points += GetPointsForPosition(pos);
                    standing.totalTime += time;
                    if (pos < standing.bestPosition) standing.bestPosition = pos;
                    standings[driverIndex] = standing;
                }
                pos++;
            }

            currentTrackIndex++;

            if (currentTrackIndex >= trackOrder.Length)
            {
                // Grand Prix complete!
                FinishGrandPrix();
            }
            else
            {
                LoadNextTrack();
            }
        }

        private void LoadNextTrack()
        {
            if (currentTrackIndex < trackOrder.Length)
            {
                // Tell GameManager to load the next track
                Debug.Log($"[Grand Prix] Loading track: {trackOrder[currentTrackIndex]}");
            }
        }

        private void FinishGrandPrix()
        {
            isActive = false;

            // Sort standings by points
            standings.Sort((a, b) => b.points.CompareTo(a.points));

            Debug.Log("=== GRAND PRIX FINAL STANDINGS ===");
            for (int i = 0; i < standings.Count; i++)
            {
                var s = standings[i];
                Debug.Log($"{i + 1}. {s.driverName} — {s.points} pts (Best: {s.bestPosition})");
            }

            // Show results
            GameManager.Instance?.OnRaceFinished(
                standings[0].bestPosition, 0f, standings[0].totalTime
            );
        }

        private int GetPointsForPosition(int position)
        {
            int index = Mathf.Clamp(position - 1, 0, positionPoints.Length - 1);
            return positionPoints[index];
        }

        public List<(string name, int points)> GetStandings()
        {
            var result = new List<(string, int)>();
            foreach (var s in standings)
            {
                result.Add((s.driverName, s.points));
            }
            return result;
        }

        public bool IsActive => isActive;
        public int CurrentTrackIndex => currentTrackIndex;
    }
}
