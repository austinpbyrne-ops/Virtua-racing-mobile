using UnityEngine;
using System.Collections.Generic;

namespace VRacer.Track
{
    /// <summary>
    /// Procedurally places track surface polygons along waypoints.
    /// Creates the road mesh from segments: straights and curved pieces.
    /// Track surface is flat-shaded polygon strips with road markings
    /// painted as colored polygon faces.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class TrackMeshBuilder : MonoBehaviour
    {
        [Header("Track Parameters")]
        [SerializeField] private float trackWidth = 12f;
        [SerializeField] private float curbWidth = 1.5f;
        [SerializeField] private int segmentsPerCurve = 16;
        [SerializeField] private float roadMarkingWidth = 0.3f;

        [Header("Road Colors")]
        [SerializeField] private Color asphaltColor = new Color(0.3f, 0.3f, 0.35f);
        [SerializeField] private Color curbColor = new Color(0.8f, 0.2f, 0.2f);      // Red-orange curbs
        [SerializeField] private Color roadLineColor = Color.white;
        [SerializeField] private Color grassColor = new Color(0.2f, 0.6f, 0.2f);

        [Header("References")]
        [SerializeField] private TrackDefinition trackDefinition;

        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private Mesh trackMesh;

        private void Awake()
        {
            meshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();

            if (trackDefinition == null)
                trackDefinition = GetComponent<TrackDefinition>();
        }

        [ContextMenu("Build Track Mesh")]
        public void BuildTrackMesh()
        {
            if (trackDefinition == null || trackDefinition.Waypoints == null || trackDefinition.Waypoints.Count < 3)
            {
                Debug.LogError("Track needs at least 3 waypoints");
                return;
            }

            List<Vector3> waypoints = new List<Vector3>();
            foreach (var wp in trackDefinition.Waypoints)
            {
                if (wp != null) waypoints.Add(wp.position);
            }

            if (waypoints.Count < 3) return;

            // Build mesh data
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            List<Color> colors = new List<Color>();
            List<Vector3> normals = new List<Vector3>();

            for (int i = 0; i < waypoints.Count; i++)
            {
                int next = (i + 1) % waypoints.Count;
                Vector3 start = waypoints[i];
                Vector3 end = waypoints[next];
                Vector3 forward = (end - start).normalized;
                Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

                // Track surface vertices (left curb, left road, center, right road, right curb)
                Vector3 leftEdge = start - right * (trackWidth / 2f);
                Vector3 leftRoad = start - right * (trackWidth / 2f - curbWidth);
                Vector3 center = start;
                Vector3 rightRoad = start + right * (trackWidth / 2f - curbWidth);
                Vector3 rightEdge = start + right * (trackWidth / 2f);

                int baseVert = vertices.Count;

                // Add row vertices
                vertices.Add(leftEdge);  vertices.Add(leftRoad);
                vertices.Add(center);    vertices.Add(rightRoad);
                vertices.Add(rightEdge);

                colors.Add(curbColor);   colors.Add(asphaltColor);
                colors.Add(asphaltColor); colors.Add(asphaltColor);
                colors.Add(curbColor);

                normals.Add(Vector3.up); normals.Add(Vector3.up);
                normals.Add(Vector3.up); normals.Add(Vector3.up);
                normals.Add(Vector3.up);

                // Build triangles connecting to next row
                if (i < waypoints.Count - 1)
                {
                    int nextBase = baseVert + 5;
                    for (int s = 0; s < 4; s++)
                    {
                        // Two triangles per quad strip segment
                        triangles.Add(baseVert + s);
                        triangles.Add(nextBase + s);
                        triangles.Add(nextBase + s + 1);

                        triangles.Add(baseVert + s);
                        triangles.Add(nextBase + s + 1);
                        triangles.Add(baseVert + s + 1);
                    }
                }
            }

            // Close the loop (last row to first row)
            int lastBase = (waypoints.Count - 1) * 5;
            for (int s = 0; s < 4; s++)
            {
                triangles.Add(lastBase + s);
                triangles.Add(s);
                triangles.Add(s + 1);

                triangles.Add(lastBase + s);
                triangles.Add(s + 1);
                triangles.Add(lastBase + s + 1);
            }

            // Create mesh
            if (trackMesh == null)
            {
                trackMesh = new Mesh { name = "TrackMesh" };
            }

            trackMesh.Clear();
            trackMesh.SetVertices(vertices);
            trackMesh.SetTriangles(triangles, 0);
            trackMesh.SetColors(colors);
            trackMesh.SetNormals(normals);
            trackMesh.RecalculateBounds();

            meshFilter.mesh = trackMesh;
        }

        private void OnDrawGizmosSelected()
        {
            if (trackDefinition == null || trackDefinition.Waypoints == null) return;

            Gizmos.color = Color.grey;
            foreach (var wp in trackDefinition.Waypoints)
            {
                if (wp == null) continue;
                Vector3 right = Vector3.Cross(Vector3.up, wp.forward).normalized;
                Gizmos.DrawLine(wp.position - right * trackWidth / 2f, wp.position + right * trackWidth / 2f);
            }
        }
    }
}
