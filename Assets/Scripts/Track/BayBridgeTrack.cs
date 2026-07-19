using UnityEngine;
using System.Collections.Generic;

// ============================================================
// BayBridgeTrack — Defines the complete Bay Bridge circuit:
// signature suspension bridge, tunnel, coastal scenery.
// Intermediate difficulty.
// ============================================================

namespace VirtuaRacing
{
    [ExecuteInEditMode]
    public class BayBridgeTrack : MonoBehaviour
    {
        [Header("Track Reference")]
        [SerializeField] private TrackBuilder trackBuilder;
        
        [Header("Scenery Prefabs")]
        [SerializeField] private GameObject suspensionBridgePrefab;
        [SerializeField] private GameObject bridgeTowerPrefab;
        [SerializeField] private GameObject waterPlanePrefab;
        [SerializeField] private GameObject tunnelEntrancePrefab;
        [SerializeField] private GameObject palmTreePrefab;
        [SerializeField] private GameObject mountainPrefab;
        [SerializeField] private GameObject redStructurePrefab;
        [SerializeField] private GameObject barrierPrefab;
        
        public void BuildTrack()
        {
            if (trackBuilder == null)
            {
                trackBuilder = GetComponent<TrackBuilder>();
                if (trackBuilder == null)
                {
                    Debug.LogError("BayBridgeTrack requires a TrackBuilder!");
                    return;
                }
            }
            
            DefineTrackSegments();
            trackBuilder.BuildTrack();
            PlaceScenery();
        }
        
        private void DefineTrackSegments()
        {
            var sectors = new List<TrackSector>();
            
            // Bay Bridge has a more complex layout with the bridge as centerpiece
            // Sector 1: Start straight → approach bridge
            var s1 = new TrackSector { name = "Start Straight" };
            s1.segments.Add(new TrackSegment { type = TrackSegment.SegmentType.Straight, length = 150f });
            sectors.Add(s1);
            
            // Sector 2: Right curve → bridge approach
            var s2 = new TrackSector { name = "Bridge Approach" };
            s2.segments.Add(new TrackSegment { type = TrackSegment.SegmentType.CurveRight, length = 100f, radius = 200f });
            sectors.Add(s2);
            
            // Sector 3: THE BRIDGE (long straight, signature feature)
            var s3 = new TrackSector { name = "Suspension Bridge" };
            s3.segments.Add(new TrackSegment { type = TrackSegment.SegmentType.Straight, length = 300f });
            sectors.Add(s3);
            
            // Sector 4: Post-bridge left curve
            var s4 = new TrackSector { name = "Post-Bridge Left" };
            s4.segments.Add(new TrackSegment { type = TrackSegment.SegmentType.CurveLeft, length = 110f, radius = 180f });
            sectors.Add(s4);
            
            // Sector 5: Tunnel section
            var s5 = new TrackSector { name = "Tunnel" };
            s5.segments.Add(new TrackSegment { type = TrackSegment.SegmentType.Straight, length = 80f });
            sectors.Add(s5);
            
            // Sector 6: Coastal curves
            var s6 = new TrackSector { name = "Coastal Curves" };
            s6.segments.Add(new TrackSegment { type = TrackSegment.SegmentType.CurveRight, length = 90f, radius = 150f });
            s6.segments.Add(new TrackSegment { type = TrackSegment.SegmentType.CurveLeft, length = 90f, radius = 160f });
            sectors.Add(s6);
            
            // Sector 7: Return to start
            var s7 = new TrackSector { name = "Return" };
            s7.segments.Add(new TrackSegment { type = TrackSegment.SegmentType.CurveRight, length = 130f, radius = 220f });
            sectors.Add(s7);
            
            // Assign sectors
            var field = typeof(TrackBuilder).GetField("sectors",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null) field.SetValue(trackBuilder, sectors);
        }
        
        private void PlaceScenery()
        {
            if (trackBuilder == null) return;
            var waypoints = trackBuilder.GetWaypoints();
            if (waypoints.Count == 0) return;
            
            Transform sceneryParent = transform.Find("Scenery");
            if (sceneryParent == null)
            {
                sceneryParent = new GameObject("Scenery").transform;
                sceneryParent.SetParent(transform);
            }
            
            // ---- SUSPENSION BRIDGE (signature) ----
            int bridgeStart = waypoints.Count / 3;
            int bridgeEnd = bridgeStart + waypoints.Count / 6;
            
            // Bridge towers at each end
            PlaceObject(bridgeTowerPrefab, waypoints[bridgeStart], Quaternion.identity, 
                Vector3.one * 4f, sceneryParent, "BridgeTower_Start");
            PlaceObject(bridgeTowerPrefab, waypoints[bridgeEnd], Quaternion.LookRotation(Vector3.back), 
                Vector3.one * 4f, sceneryParent, "BridgeTower_End");
            
            // Water plane under bridge
            Vector3 bridgeMidpoint = (waypoints[bridgeStart] + waypoints[bridgeEnd]) * 0.5f;
            PlaceObject(waterPlanePrefab, bridgeMidpoint - Vector3.up * 15f, Quaternion.identity,
                new Vector3(30f, 1f, 300f), sceneryParent, "WaterPlane");
            
            // ---- TUNNEL ----
            int tunnelIdx = waypoints.Count * 2 / 3;
            PlaceObject(tunnelEntrancePrefab, waypoints[tunnelIdx], 
                Quaternion.LookRotation(waypoints[(tunnelIdx + 1) % waypoints.Count] - waypoints[tunnelIdx]),
                Vector3.one * 3f, sceneryParent, "Tunnel");
            
            // ---- MOUNTAINS (distant background) ----
            for (int i = 0; i < 5; i++)
            {
                Vector3 mountainPos = waypoints[i * waypoints.Count / 5] + Vector3.back * 200f;
                mountainPos.x += Random.Range(-100f, 100f);
                mountainPos.y = -10f;
                
                PlaceObject(mountainPrefab, mountainPos, Quaternion.identity,
                    new Vector3(Random.Range(3f, 8f), Random.Range(4f, 10f), Random.Range(2f, 5f)),
                    sceneryParent, $"Mountain_{i}");
            }
            
            // ---- PALM TREES ----
            for (int i = 0; i < 25; i++)
            {
                int idx = (i * waypoints.Count / 25) % waypoints.Count;
                float side = (i % 3 == 0) ? -1f : 1f;
                Vector3 pos = waypoints[idx] + Vector3.right * side * Random.Range(20f, 40f);
                PlaceObject(palmTreePrefab, pos, Quaternion.identity,
                    Vector3.one * Random.Range(0.8f, 1.5f), sceneryParent, $"Palm_{i}");
            }
            
            // ---- RED TRACKSIDE STRUCTURE (grandstand/landmark) ----
            Vector3 redStructPos = waypoints[waypoints.Count / 2] + Vector3.right * 30f;
            PlaceObject(redStructurePrefab, redStructPos, Quaternion.LookRotation(Vector3.left),
                Vector3.one * 2.5f, sceneryParent, "RedStructure");
            
            // ---- TRACK BARRIERS (orange-red) ----
            for (int i = 0; i < waypoints.Count; i += waypoints.Count / 20)
            {
                PlaceObject(barrierPrefab, waypoints[i] + Vector3.left * 6.5f, 
                    Quaternion.identity, Vector3.one * 0.5f, sceneryParent, $"Barrier_L_{i}");
            }
        }
        
        private void PlaceObject(GameObject prefab, Vector3 pos, Quaternion rot, 
            Vector3 scale, Transform parent, string name)
        {
            if (prefab == null) return;
            var obj = Instantiate(prefab, pos, rot, parent);
            obj.transform.localScale = scale;
            obj.name = name;
        }
    }
}
