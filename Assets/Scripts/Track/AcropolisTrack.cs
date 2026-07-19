using UnityEngine;
using System.Collections.Generic;

// ============================================================
// AcropolisTrack — Expert circuit: tight hairpins,
// elevation changes, urban canyon with tall buildings,
// sunset atmosphere, multiple tunnel transitions.
// ============================================================

namespace VirtuaRacing
{
    [ExecuteInEditMode]
    public class AcropolisTrack : MonoBehaviour
    {
        [Header("Track Reference")]
        [SerializeField] private TrackBuilder trackBuilder;
        
        [Header("Scenery Prefabs")]
        [SerializeField] private GameObject skyscraperPrefab;
        [SerializeField] private GameObject buildingPrefab;
        [SerializeField] private GameObject stripedWallPrefab;
        [SerializeField] private GameObject tunnelEntrancePrefab;
        [SerializeField] private GameObject tunnelInteriorPrefab;
        [SerializeField] private GameObject mountainPrefab;
        [SerializeField] private GameObject curbPrefab;
        
        [Header("Sky Settings (Acropolis = Sunset)")]
        [SerializeField] private Color sunsetTopColor = new Color(0.5f, 0.1f, 0.05f);
        [SerializeField] private Color sunsetHorizonColor = new Color(0.9f, 0.4f, 0.1f);
        [SerializeField] private Material skyMaterial;
        
        public void BuildTrack()
        {
            if (trackBuilder == null)
            {
                trackBuilder = GetComponent<TrackBuilder>();
                if (trackBuilder == null)
                {
                    Debug.LogError("AcropolisTrack requires a TrackBuilder!");
                    return;
                }
            }
            
            DefineTrackSegments();
            trackBuilder.BuildTrack();
            ConfigureSunsetSky();
            PlaceScenery();
        }
        
        private void DefineTrackSegments()
        {
            var sectors = new List<TrackSector>();
            
            // Acropolis: tight, technical, with elevation changes
            // Sector 1: Start straight through urban canyon
            var s1 = new TrackSector { name = "Start Canyon" };
            s1.segments.Add(new TrackSegment { type = TrackSegment.SegmentType.Straight, length = 120f });
            sectors.Add(s1);
            
            // Sector 2: Hairpin right (signature tight turn)
            var s2 = new TrackSector { name = "Hairpin Right" };
            s2.segments.Add(new TrackSegment { type = TrackSegment.SegmentType.CurveRight, length = 60f, radius = 50f });
            sectors.Add(s2);
            
            // Sector 3: Uphill straight → tunnel
            var s3 = new TrackSector { name = "Tunnel Approach" };
            s3.segments.Add(new TrackSegment { type = TrackSegment.SegmentType.Hill, length = 140f, elevation = 15f });
            sectors.Add(s3);
            
            // Sector 4: Tunnel section
            var s4 = new TrackSector { name = "Tunnel" };
            s4.segments.Add(new TrackSegment { type = TrackSegment.SegmentType.Tunnel, length = 60f });
            sectors.Add(s4);
            
            // Sector 5: Downhill with sharp left
            var s5 = new TrackSector { name = "Downhill Left" };
            s5.segments.Add(new TrackSegment { type = TrackSegment.SegmentType.CurveLeft, length = 80f, radius = 80f });
            s5.segments.Add(new TrackSegment { type = TrackSegment.SegmentType.Declined, length = 100f, elevation = -12f });
            sectors.Add(s5);
            
            // Sector 6: Hairpin left
            var s6 = new TrackSector { name = "Hairpin Left" };
            s6.segments.Add(new TrackSegment { type = TrackSegment.SegmentType.CurveLeft, length = 55f, radius = 45f });
            sectors.Add(s6);
            
            // Sector 7: Urban canyon sprint
            var s7 = new TrackSector { name = "Canyon Sprint" };
            s7.segments.Add(new TrackSegment { type = TrackSegment.SegmentType.Straight, length = 180f });
            sectors.Add(s7);
            
            // Sector 8: Final sweep → start/finish
            var s8 = new TrackSector { name = "Final Sweep" };
            s8.segments.Add(new TrackSegment { type = TrackSegment.SegmentType.CurveRight, length = 100f, radius = 150f });
            sectors.Add(s8);
            
            var field = typeof(TrackBuilder).GetField("sectors",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null) field.SetValue(trackBuilder, sectors);
        }
        
        private void ConfigureSunsetSky()
        {
            if (skyMaterial != null)
            {
                skyMaterial.SetColor("_TopColor", sunsetTopColor);
                skyMaterial.SetColor("_HorizonColor", sunsetHorizonColor);
                RenderSettings.skybox = skyMaterial;
            }
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
            
            // ---- SKYSCRAPERS & BUILDINGS (urban canyon effect) ----
            for (int i = 0; i < 60; i++)
            {
                int idx = (i * waypoints.Count / 60) % waypoints.Count;
                float side = (i % 2 == 0) ? -1f : 1f;
                float distance = 15f + Random.Range(5f, 40f);
                
                Vector3 pos = waypoints[idx] + Vector3.right * side * distance;
                pos.y = -5f;
                
                // Mix of skyscrapers and smaller buildings
                GameObject prefab = (i % 3 == 0) ? skyscraperPrefab : buildingPrefab;
                float height = (i % 3 == 0) ? Random.Range(8f, 20f) : Random.Range(3f, 8f);
                
                PlaceObject(prefab, pos, 
                    Quaternion.LookRotation(Vector3.forward * -side),
                    new Vector3(Random.Range(2f, 4f), height, Random.Range(2f, 4f)),
                    sceneryParent, $"Building_{i}");
            }
            
            // ---- STRIPED WALL (signature landmark, right side) ----
            int wallIdx = waypoints.Count / 3;
            Vector3 wallForward = (waypoints[(wallIdx + 1) % waypoints.Count] - waypoints[wallIdx]).normalized;
            PlaceObject(stripedWallPrefab, 
                waypoints[wallIdx] + Vector3.right * 18f + Vector3.up * 2f,
                Quaternion.LookRotation(Vector3.left),
                new Vector3(1, 6, 20), sceneryParent, "StripedWall");
            
            // ---- TUNNEL ENTRANCES ----
            int tunnelIdx = waypoints.Count / 2;
            // Tunnel entrance at apex of uphill straight
            Vector3 tunnelDir = (waypoints[(tunnelIdx + 1) % waypoints.Count] - waypoints[tunnelIdx]).normalized;
            PlaceObject(tunnelEntrancePrefab, waypoints[tunnelIdx], 
                Quaternion.LookRotation(tunnelDir),
                Vector3.one * 3.5f, sceneryParent, "TunnelEntrance");
            
            // Tunnel interior
            int interiorIdx = tunnelIdx + waypoints.Count / 12;
            PlaceObject(tunnelInteriorPrefab, waypoints[interiorIdx % waypoints.Count],
                Quaternion.LookRotation(tunnelDir),
                Vector3.one * 2.5f, sceneryParent, "TunnelInterior");
            
            // ---- CURBS & SHOULDERS (beige strips) ----
            for (int i = 0; i < waypoints.Count; i += waypoints.Count / 15)
            {
                PlaceObject(curbPrefab, waypoints[i] + Vector3.left * 6.2f,
                    Quaternion.identity, Vector3.one, sceneryParent, $"Curb_{i}");
            }
            
            // ---- DISTANT MOUNTAINS (dark, silhouetted at sunset) ----
            for (int i = 0; i < 4; i++)
            {
                Vector3 mountainPos = waypoints[i * waypoints.Count / 4] + Vector3.back * 250f;
                mountainPos.x += Random.Range(-80f, 80f);
                mountainPos.y = -20f;
                
                PlaceObject(mountainPrefab, mountainPos, Quaternion.identity,
                    new Vector3(Random.Range(5f, 10f), Random.Range(8f, 15f), Random.Range(3f, 6f)),
                    sceneryParent, $"Mountain_{i}");
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
