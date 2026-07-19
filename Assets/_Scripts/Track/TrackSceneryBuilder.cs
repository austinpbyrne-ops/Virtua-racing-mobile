using UnityEngine;
using System.Collections.Generic;

namespace VRacer.Track
{
    /// <summary>
    /// Places all trackside scenery objects for a track.
    /// Big Forest: Ferris wheel, roller coaster, trees, pit building, grandstands
    /// Bay Bridge: Suspension bridge, tunnel, palm trees, water, red structures
    /// Acropolis: Skyscrapers, tunnel sections, striped wall, urban canyon
    /// </summary>
    public class TrackSceneryBuilder : MonoBehaviour
    {
        [Header("Track Reference")]
        [SerializeField] private TrackDefinition trackDefinition;

        [Header("Scenery Prefabs")]
        [SerializeField] private GameObject treePrefab;
        [SerializeField] private GameObject buildingPrefab;
        [SerializeField] private GameObject ferrisWheelPrefab;
        [SerializeField] private GameObject rollerCoasterPrefab;
        [SerializeField] private GameObject grandstandPrefab;
        [SerializeField] private GameObject bridgeTowerPrefab;
        [SerializeField] private GameObject bridgeCablePrefab;
        [SerializeField] private GameObject skyscraperPrefab;
        [SerializeField] private GameObject tunnelPrefab;
        [SerializeField] private GameObject palmTreePrefab;
        [SerializeField] private GameObject waterPlanePrefab;
        [SerializeField] private GameObject distanceMarkerPrefab;
        [SerializeField] private GameObject flagPrefab;

        [Header("Placement Data (Big Forest)")]
        [SerializeField] private SceneryPlacement[] bigForestPlacements;

        [Header("Placement Data (Bay Bridge)")]
        [SerializeField] private SceneryPlacement[] bayBridgePlacements;

        [Header("Placement Data (Acropolis)")]
        [SerializeField] private SceneryPlacement[] acropolisPlacements;

        [System.Serializable]
        public class SceneryPlacement
        {
            public string objectName;
            public SceneryType type;
            public Vector3 position;
            public Vector3 rotation;
            public Vector3 scale = Vector3.one;
            public Color tintColor = Color.white;
        }

        public enum SceneryType
        {
            Tree,
            Building,
            FerrisWheel,
            RollerCoaster,
            Grandstand,
            BridgeTower,
            BridgeCable,
            Skyscraper,
            Tunnel,
            PalmTree,
            WaterPlane,
            DistanceMarker,
            Flag
        }

        [ContextMenu("Build Scenery")]
        public void BuildScenery()
        {
            if (trackDefinition == null) return;

            SceneryPlacement[] placements = trackDefinition.TrackName switch
            {
                "Big Forest" => bigForestPlacements,
                "Bay Bridge" => bayBridgePlacements,
                "Acropolis" => acropolisPlacements,
                _ => null
            };

            if (placements == null)
            {
                Debug.LogWarning($"[Scenery] No placement data for track: {trackDefinition.TrackName}");
                return;
            }

            // Clear existing scenery children
            foreach (Transform child in transform)
            {
                if (child.name.StartsWith("Scenery_"))
                    DestroyImmediate(child.gameObject);
            }

            // Place each scenery object
            foreach (var placement in placements)
            {
                PlaceSceneryObject(placement);
            }

            Debug.Log($"[Scenery] Built {placements.Length} objects for {trackDefinition.TrackName}");
        }

        private void PlaceSceneryObject(SceneryPlacement placement)
        {
            GameObject prefab = GetPrefabForType(placement.type);
            if (prefab == null)
            {
                // Create simple primitive as placeholder
                prefab = CreatePrimitiveForType(placement.type);
            }

            GameObject instance = Instantiate(prefab, transform);
            instance.name = $"Scenery_{placement.objectName}";
            instance.transform.localPosition = placement.position;
            instance.transform.localEulerAngles = placement.rotation;
            instance.transform.localScale = placement.scale;

            // Apply tint
            var renderer = instance.GetComponentInChildren<MeshRenderer>();
            if (renderer != null)
            {
                // Use material property block for flat-shaded look
                var mpb = new MaterialPropertyBlock();
                mpb.SetColor("_BaseColor", placement.tintColor);
                renderer.SetPropertyBlock(mpb);
            }
        }

        private GameObject GetPrefabForType(SceneryType type)
        {
            return type switch
            {
                SceneryType.Tree => treePrefab,
                SceneryType.Building => buildingPrefab,
                SceneryType.FerrisWheel => ferrisWheelPrefab,
                SceneryType.RollerCoaster => rollerCoasterPrefab,
                SceneryType.Grandstand => grandstandPrefab,
                SceneryType.BridgeTower => bridgeTowerPrefab,
                SceneryType.BridgeCable => bridgeCablePrefab,
                SceneryType.Skyscraper => skyscraperPrefab,
                SceneryType.Tunnel => tunnelPrefab,
                SceneryType.PalmTree => palmTreePrefab,
                SceneryType.WaterPlane => waterPlanePrefab,
                SceneryType.DistanceMarker => distanceMarkerPrefab,
                SceneryType.Flag => flagPrefab,
                _ => null
            };
        }

        private GameObject CreatePrimitiveForType(SceneryType type)
        {
            GameObject go = type switch
            {
                SceneryType.Tree => CreateTree(),
                SceneryType.Building => GameObject.CreatePrimitive(PrimitiveType.Cube),
                SceneryType.FerrisWheel => GameObject.CreatePrimitive(PrimitiveType.Cylinder),
                SceneryType.RollerCoaster => GameObject.CreatePrimitive(PrimitiveType.Cube),
                SceneryType.Grandstand => CreateGrandstand(),
                SceneryType.BridgeTower => CreateBridgeTower(),
                SceneryType.Skyscraper => CreateSkyscraper(),
                SceneryType.Tunnel => CreateTunnel(),
                SceneryType.PalmTree => CreatePalmTree(),
                SceneryType.WaterPlane => GameObject.CreatePrimitive(PrimitiveType.Plane),
                _ => GameObject.CreatePrimitive(PrimitiveType.Cube)
            };

            // Remove collider (scenery doesn't need physics)
            var collider = go.GetComponent<Collider>();
            if (collider != null) DestroyImmediate(collider);

            return go;
        }

        // ============================================================
        // PRIMITIVE SCENERY CREATION
        // These create simple polygon shapes matching the Model 1 aesthetic
        // ============================================================

        private GameObject CreateTree()
        {
            GameObject tree = new GameObject("Tree_Primitive");

            // Trunk: brown cylinder
            GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Trunk";
            trunk.transform.SetParent(tree.transform);
            trunk.transform.localPosition = Vector3.up * 0.5f;
            trunk.transform.localScale = new Vector3(0.2f, 0.5f, 0.2f);
            var trunkRenderer = trunk.GetComponent<MeshRenderer>();
            // Brown trunk

            // Canopy: green pyramid (using cylinder scaled to look roughly conical)
            GameObject canopy = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            canopy.name = "Canopy";
            canopy.transform.SetParent(tree.transform);
            canopy.transform.localPosition = Vector3.up * 1.2f;
            canopy.transform.localScale = new Vector3(0.8f, 0.5f, 0.8f);
            var canopyRenderer = canopy.GetComponent<MeshRenderer>();

            return tree;
        }

        private GameObject CreateGrandstand()
        {
            GameObject gs = new GameObject("Grandstand_Primitive");

            // Horizontal color strips stacked
            Color[] stripColors = { Color.green, Color.yellow, Color.red, Color.blue };
            for (int i = 0; i < 4; i++)
            {
                GameObject strip = GameObject.CreatePrimitive(PrimitiveType.Cube);
                strip.name = $"Strip_{i}";
                strip.transform.SetParent(gs.transform);
                strip.transform.localPosition = new Vector3(0, i * 0.3f, 0);
                strip.transform.localScale = new Vector3(3f, 0.25f, 2f);
            }

            return gs;
        }

        private GameObject CreateBridgeTower()
        {
            GameObject tower = new GameObject("BridgeTower_Primitive");

            // Tall rectangular prism
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(tower.transform);
            body.transform.localScale = new Vector3(1f, 10f, 1f);

            // Small pyramid cap on top
            GameObject cap = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cap.name = "Cap";
            cap.transform.SetParent(tower.transform);
            cap.transform.localPosition = Vector3.up * 5f;
            cap.transform.localScale = new Vector3(1.2f, 0.5f, 1.2f);

            return tower;
        }

        private GameObject CreateSkyscraper()
        {
            GameObject building = new GameObject("Skyscraper_Primitive");

            // Tall rectangular prism
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(building.transform);
            body.transform.localScale = new Vector3(2f, 15f, 2f);

            return building;
        }

        private GameObject CreateTunnel()
        {
            GameObject tunnel = new GameObject("Tunnel_Primitive");

            // U-shaped tunnel: three cubes forming walls and roof
            // Left wall
            GameObject left = GameObject.CreatePrimitive(PrimitiveType.Cube);
            left.name = "LeftWall";
            left.transform.SetParent(tunnel.transform);
            left.transform.localPosition = new Vector3(-6f, 2f, 0f);
            left.transform.localScale = new Vector3(1f, 4f, 20f);

            // Right wall
            GameObject right = GameObject.CreatePrimitive(PrimitiveType.Cube);
            right.name = "RightWall";
            right.transform.SetParent(tunnel.transform);
            right.transform.localPosition = new Vector3(6f, 2f, 0f);
            right.transform.localScale = new Vector3(1f, 4f, 20f);

            // Roof
            GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roof.name = "Roof";
            roof.transform.SetParent(tunnel.transform);
            roof.transform.localPosition = new Vector3(0f, 4f, 0f);
            roof.transform.localScale = new Vector3(12f, 0.5f, 20f);

            return tunnel;
        }

        private GameObject CreatePalmTree()
        {
            GameObject palm = new GameObject("PalmTree_Primitive");

            // Trunk: brown cylinder
            GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Trunk";
            trunk.transform.SetParent(palm.transform);
            trunk.transform.localScale = new Vector3(0.15f, 2f, 0.15f);

            // Fronds: flat green cubes fanning out
            for (int i = 0; i < 5; i++)
            {
                GameObject frond = GameObject.CreatePrimitive(PrimitiveType.Cube);
                frond.name = $"Frond_{i}";
                frond.transform.SetParent(palm.transform);
                float angle = (i * 72f + 30f) * Mathf.Deg2Rad;
                frond.transform.localPosition = new Vector3(Mathf.Cos(angle) * 0.5f, 2f, Mathf.Sin(angle) * 0.5f);
                frond.transform.localRotation = Quaternion.Euler(45f, i * 72f, 0f);
                frond.transform.localScale = new Vector3(0.1f, 0.8f, 0.05f);
            }

            return palm;
        }

        // ============================================================
        // SCENERY DEFINITION DATA
        // These define exact placements for each track
        // ============================================================

        [ContextMenu("Generate Big Forest Placements")]
        public void GenerateBigForestPlacements()
        {
            bigForestPlacements = new SceneryPlacement[]
            {
                // Ferris wheel - left side, visible from multiple track sections
                new SceneryPlacement { objectName = "FerrisWheel", type = SceneryType.FerrisWheel,
                    position = new Vector3(-80, 0, 100), rotation = Vector3.zero, scale = new Vector3(8, 12, 1),
                    tintColor = new Color(0.8f, 0.1f, 0.1f) }, // Red frame

                // Roller coaster - around the Ferris wheel
                new SceneryPlacement { objectName = "RollerCoaster1", type = SceneryType.RollerCoaster,
                    position = new Vector3(-90, 5, 110), rotation = new Vector3(0, 45, 0), scale = new Vector3(10, 1, 2),
                    tintColor = new Color(0.8f, 0.1f, 0.1f) },

                // Pit building (long structure with garage openings)
                new SceneryPlacement { objectName = "PitBuilding", type = SceneryType.Building,
                    position = new Vector3(0, 0, 40), rotation = Vector3.zero, scale = new Vector3(30, 4, 8),
                    tintColor = new Color(0.5f, 0.1f, 0.1f) }, // Dark red lower

                // Grandstands - right side of main straight
                new SceneryPlacement { objectName = "Grandstand1", type = SceneryType.Grandstand,
                    position = new Vector3(20, 0, 50), rotation = new Vector3(0, -20, 0), scale = Vector3.one,
                    tintColor = Color.white },

                // Trees along the track
                new SceneryPlacement { objectName = "Tree_L1", type = SceneryType.Tree,
                    position = new Vector3(-15, 0, 80), tintColor = new Color(0.1f, 0.5f, 0.1f) },
                new SceneryPlacement { objectName = "Tree_L2", type = SceneryType.Tree,
                    position = new Vector3(-18, 0, 120), tintColor = new Color(0.15f, 0.55f, 0.15f) },
                new SceneryPlacement { objectName = "Tree_R1", type = SceneryType.Tree,
                    position = new Vector3(15, 0, 150), tintColor = new Color(0.1f, 0.5f, 0.1f) },

                // Distance markers
                new SceneryPlacement { objectName = "Marker_6", type = SceneryType.DistanceMarker,
                    position = new Vector3(-10, 0.1f, 200), tintColor = Color.white },
                new SceneryPlacement { objectName = "Marker_8", type = SceneryType.DistanceMarker,
                    position = new Vector3(-10, 0.1f, 400), tintColor = Color.white },
                new SceneryPlacement { objectName = "Marker_10", type = SceneryType.DistanceMarker,
                    position = new Vector3(-10, 0.1f, 600), tintColor = Color.white },

                // Flags on pit straight
                new SceneryPlacement { objectName = "Flag_France", type = SceneryType.Flag,
                    position = new Vector3(8, 3, 42), scale = new Vector3(0.5f, 0.8f, 0.1f),
                    tintColor = Color.white },
                new SceneryPlacement { objectName = "Flag_USA", type = SceneryType.Flag,
                    position = new Vector3(12, 3, 42), scale = new Vector3(0.5f, 0.8f, 0.1f),
                    tintColor = Color.white },
                new SceneryPlacement { objectName = "Flag_Germany", type = SceneryType.Flag,
                    position = new Vector3(16, 3, 42), scale = new Vector3(0.5f, 0.8f, 0.1f),
                    tintColor = Color.white },
            };

            Debug.Log("[Scenery] Generated Big Forest placements");
        }

        [ContextMenu("Generate Bay Bridge Placements")]
        public void GenerateBayBridgePlacements()
        {
            bayBridgePlacements = new SceneryPlacement[]
            {
                // Suspension bridge towers
                new SceneryPlacement { objectName = "BridgeTower1", type = SceneryType.BridgeTower,
                    position = new Vector3(-6, 0, 300), scale = new Vector3(2, 12, 1),
                    tintColor = new Color(0.3f, 0.3f, 0.35f) },
                new SceneryPlacement { objectName = "BridgeTower2", type = SceneryType.BridgeTower,
                    position = new Vector3(6, 0, 300), scale = new Vector3(2, 12, 1),
                    tintColor = new Color(0.3f, 0.3f, 0.35f) },

                // Water plane under bridge
                new SceneryPlacement { objectName = "Water", type = SceneryType.WaterPlane,
                    position = new Vector3(0, -2, 300), scale = new Vector3(20, 1, 50),
                    tintColor = new Color(0.1f, 0.2f, 0.5f) },

                // Tunnel
                new SceneryPlacement { objectName = "Tunnel", type = SceneryType.Tunnel,
                    position = new Vector3(0, 0, 500), scale = Vector3.one,
                    tintColor = new Color(0.2f, 0.2f, 0.2f) },

                // Palm trees
                new SceneryPlacement { objectName = "Palm1", type = SceneryType.PalmTree,
                    position = new Vector3(-12, 0, 150), tintColor = new Color(0.1f, 0.4f, 0.1f) },
                new SceneryPlacement { objectName = "Palm2", type = SceneryType.PalmTree,
                    position = new Vector3(12, 0, 160), tintColor = new Color(0.1f, 0.4f, 0.1f) },

                // Red trackside structure
                new SceneryPlacement { objectName = "RedStructure", type = SceneryType.Building,
                    position = new Vector3(25, 0, 200), scale = new Vector3(5, 6, 8),
                    tintColor = new Color(0.9f, 0.1f, 0.1f) },

                // Distant mountains
                new SceneryPlacement { objectName = "Mountain1", type = SceneryType.Building,
                    position = new Vector3(-100, 0, 500), scale = new Vector3(30, 12, 10),
                    tintColor = new Color(0.15f, 0.15f, 0.2f) },
                new SceneryPlacement { objectName = "Mountain2", type = SceneryType.Building,
                    position = new Vector3(100, 0, 520), scale = new Vector3(40, 15, 12),
                    tintColor = new Color(0.12f, 0.12f, 0.18f) },
            };

            Debug.Log("[Scenery] Generated Bay Bridge placements");
        }

        [ContextMenu("Generate Acropolis Placements")]
        public void GenerateAcropolisPlacements()
        {
            acropolisPlacements = new SceneryPlacement[]
            {
                // Skyscrapers - clustered on both sides
                new SceneryPlacement { objectName = "Skyscraper_L1", type = SceneryType.Skyscraper,
                    position = new Vector3(-20, 0, 100), scale = new Vector3(4, 18, 3),
                    tintColor = new Color(0.15f, 0.2f, 0.15f) },
                new SceneryPlacement { objectName = "Skyscraper_L2", type = SceneryType.Skyscraper,
                    position = new Vector3(-25, 0, 120), scale = new Vector3(3, 14, 3),
                    tintColor = new Color(0.3f, 0.2f, 0.1f) },
                new SceneryPlacement { objectName = "Skyscraper_R1", type = SceneryType.Skyscraper,
                    position = new Vector3(20, 0, 95), scale = new Vector3(4, 20, 3),
                    tintColor = new Color(0.12f, 0.12f, 0.18f) },
                new SceneryPlacement { objectName = "Skyscraper_R2", type = SceneryType.Skyscraper,
                    position = new Vector3(25, 0, 130), scale = new Vector3(3, 16, 3),
                    tintColor = new Color(0.2f, 0.15f, 0.1f) },

                // Striped wall structure - distinctive Acropolis landmark
                new SceneryPlacement { objectName = "StripedWall", type = SceneryType.Building,
                    position = new Vector3(15, 0, 200), rotation = new Vector3(0, 0, -15), scale = new Vector3(6, 8, 2),
                    tintColor = new Color(0.5f, 0.4f, 0.3f) },

                // Tunnel sections
                new SceneryPlacement { objectName = "Tunnel1", type = SceneryType.Tunnel,
                    position = new Vector3(0, 0, 300), scale = Vector3.one,
                    tintColor = new Color(0.15f, 0.15f, 0.15f) },
                new SceneryPlacement { objectName = "Tunnel2", type = SceneryType.Tunnel,
                    position = new Vector3(0, 0, 500), scale = Vector3.one,
                    tintColor = new Color(0.1f, 0.1f, 0.1f) },

                // Distant hills
                new SceneryPlacement { objectName = "Hill1", type = SceneryType.Building,
                    position = new Vector3(-80, 0, 400), scale = new Vector3(25, 10, 8),
                    tintColor = new Color(0.12f, 0.15f, 0.1f) },
                new SceneryPlacement { objectName = "Hill2", type = SceneryType.Building,
                    position = new Vector3(80, 0, 420), scale = new Vector3(30, 8, 10),
                    tintColor = new Color(0.1f, 0.12f, 0.08f) },
            };

            Debug.Log("[Scenery] Generated Acropolis placements");
        }
    }
}
