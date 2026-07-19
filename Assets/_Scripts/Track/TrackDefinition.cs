using UnityEngine;
using System.Collections.Generic;

namespace VRacer.Track
{
    /// <summary>
    /// Defines a complete racing circuit built from track segments.
    /// Each segment is a straight or curved piece with track surface 
    /// and roadside scenery.
    /// </summary>
    public class TrackDefinition : MonoBehaviour
    {
        [Header("Track Identity")]
        [SerializeField] private string trackName = "Big Forest";
        [SerializeField] private TrackDifficulty difficulty = TrackDifficulty.Beginner;
        [SerializeField] private float trackLength = 3500f; // meters

        [Header("Sky")]
        [SerializeField] private Color skyTopColor = new Color(0.1f, 0.3f, 0.8f);
        [SerializeField] private Color skyHorizonColor = new Color(0.5f, 0.7f, 1.0f);
        [SerializeField] private Color sunColor = Color.white;
        [SerializeField] private Vector3 sunDirection = new Vector3(50f, 60f, 0f);

        [Header("Checkpoints")]
        [SerializeField] private List<Transform> checkpointPositions;

        [Header("AI Racing Line")]
        [SerializeField] private List<Transform> waypoints;       // AI waypoints
        [SerializeField] private List<Transform> racingLinePoints; // Ideal racing line (smoother, more points)

        [Header("Grid Positions")]
        [SerializeField] private List<Transform> gridPositions;    // 16 starting positions

        public string TrackName => trackName;
        public TrackDifficulty Difficulty => difficulty;
        public float TrackLength => trackLength;
        public Color SkyTopColor => skyTopColor;
        public Color SkyHorizonColor => skyHorizonColor;
        public Color SunColor => sunColor;
        public Vector3 SunDirection => sunDirection;
        public List<Transform> Waypoints => waypoints;
        public List<Transform> RacingLinePoints => racingLinePoints;
        public List<Transform> GridPositions => gridPositions;
        public List<Transform> CheckpointPositions => checkpointPositions;

        public int LapCount => difficulty switch
        {
            TrackDifficulty.Beginner => 5,
            TrackDifficulty.Intermediate => 4,
            TrackDifficulty.Expert => 3,
            _ => 5
        };

        private void OnDrawGizmos()
        {
            if (waypoints == null || waypoints.Count < 2) return;

            // Draw waypoint network
            Gizmos.color = Color.cyan;
            for (int i = 0; i < waypoints.Count; i++)
            {
                if (waypoints[i] == null) continue;
                Gizmos.DrawWireSphere(waypoints[i].position, 1f);

                int next = (i + 1) % waypoints.Count;
                if (waypoints[next] != null)
                {
                    Gizmos.DrawLine(waypoints[i].position, waypoints[next].position);
                }
            }

            // Draw racing line (green, tighter)
            if (racingLinePoints != null && racingLinePoints.Count > 1)
            {
                Gizmos.color = Color.green;
                for (int i = 0; i < racingLinePoints.Count - 1; i++)
                {
                    if (racingLinePoints[i] == null || racingLinePoints[i + 1] == null) continue;
                    Gizmos.DrawLine(racingLinePoints[i].position, racingLinePoints[i + 1].position);
                }
            }
        }
    }

    public enum TrackDifficulty
    {
        Beginner,
        Intermediate,
        Expert
    }
}
