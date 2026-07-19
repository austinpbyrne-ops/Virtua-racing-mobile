using UnityEngine;

// ============================================================
// F1CarMeshGenerator — Procedurally generates a flat-shaded
// F1-style car mesh (~300 polygons) from code.
// No external 3D models needed for MVP.
// ============================================================

namespace VirtuaRacing
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class F1CarMeshGenerator : MonoBehaviour
    {
        [Header("Car Dimensions")]
        [SerializeField] private float bodyLength = 4.5f;
        [SerializeField] private float bodyWidth = 1.8f;
        [SerializeField] private float bodyHeight = 0.9f;
        [SerializeField] private float wheelRadius = 0.35f;
        [SerializeField] private float wheelWidth = 0.25f;
        [SerializeField] private float wheelBase = 2.8f;
        [SerializeField] private float trackWidth = 1.4f; // Width between wheels
        
        [Header("Colors (Vertex Colors for flat shader)")]
        [SerializeField] private Color noseConeColor = Color.red;
        [SerializeField] private Color bodyColor = Color.white;
        [SerializeField] private Color cockpitColor = Color.black;
        [SerializeField] private Color rearWingColor = new Color(0.2f, 0.2f, 0.25f);
        [SerializeField] private Color wheelColor = new Color(0.15f, 0.15f, 0.15f);
        [SerializeField] private Color suspensionColor = new Color(0.3f, 0.3f, 0.3f);
        
        [Header("Materials")]
        [SerializeField] private Material bodyMaterial;
        [SerializeField] private Material wheelMaterial;
        
        private void Awake()
        {
            GenerateCarMesh();
        }
        
        [ContextMenu("Generate Car Mesh")]
        public void GenerateCarMesh()
        {
            MeshFilter mf = GetComponent<MeshFilter>();
            MeshRenderer mr = GetComponent<MeshRenderer>();
            
            // Combine all sub-meshes
            Mesh combinedMesh = new Mesh { name = "F1_Car" };
            
            CombineInstance[] combine = new CombineInstance[6]; // Body + 4 wheels + wing
            
            combine[0].mesh = BuildCarBody();
            combine[0].transform = Matrix4x4.identity;
            
            combine[1].mesh = BuildWheel();
            combine[1].transform = Matrix4x4.TRS(
                new Vector3(-wheelBase * 0.5f, -bodyHeight * 0.3f, trackWidth * 0.5f),
                Quaternion.identity, Vector3.one);
            
            combine[2].mesh = BuildWheel();
            combine[2].transform = Matrix4x4.TRS(
                new Vector3(-wheelBase * 0.5f, -bodyHeight * 0.3f, -trackWidth * 0.5f),
                Quaternion.identity, Vector3.one);
            
            combine[3].mesh = BuildWheel();
            combine[3].transform = Matrix4x4.TRS(
                new Vector3(wheelBase * 0.5f, -bodyHeight * 0.3f, trackWidth * 0.5f),
                Quaternion.identity, Vector3.one);
            
            combine[4].mesh = BuildWheel();
            combine[4].transform = Matrix4x4.TRS(
                new Vector3(wheelBase * 0.5f, -bodyHeight * 0.3f, -trackWidth * 0.5f),
                Quaternion.identity, Vector3.one);
            
            combine[5].mesh = BuildRearWing();
            combine[5].transform = Matrix4x4.identity;
            
            combinedMesh.CombineMeshes(combine, false);
            combinedMesh.RecalculateNormals();
            
            // CRITICAL: Split vertices for flat shading (no shared normals)
            combinedMesh = SplitVerticesForFlatShading(combinedMesh);
            
            mf.mesh = combinedMesh;
            
            if (mr != null && bodyMaterial != null)
                mr.material = bodyMaterial;
        }
        
        private Mesh BuildCarBody()
        {
            // Build the F1 car body as a series of box-like sections
            List<Vector3> verts = new List<Vector3>();
            List<Color> colors = new List<Color>();
            List<int> tris = new List<int>();
            
            float halfW = bodyWidth * 0.5f;
            float halfH = bodyHeight * 0.5f;
            float halfL = bodyLength * 0.5f;
            
            // ---- NOSE CONE (front triangular wedge) ----
            // Nose tip point
            Vector3 noseTip = new Vector3(0, 0, halfL * 1.3f);
            
            AddQuadFace(verts, colors, tris,
                new Vector3(-halfW * 0.3f, -halfH, halfL * 0.6f),  // nose bottom left
                new Vector3(halfW * 0.3f, -halfH, halfL * 0.6f),   // nose bottom right
                noseTip, noseTip, noseConeColor);
            
            AddQuadFace(verts, colors, tris,
                new Vector3(-halfW * 0.3f, -halfH, halfL * 0.6f),
                noseTip,
                new Vector3(-halfW * 0.3f, 0, halfL * 0.6f),  // nose top
                noseTip, noseConeColor);
            
            AddQuadFace(verts, colors, tris,
                new Vector3(halfW * 0.3f, -halfH, halfL * 0.6f),
                new Vector3(halfW * 0.3f, 0, halfL * 0.6f),
                noseTip, noseTip, noseConeColor);
            
            // ---- MAIN BODY (center monocoque) ----
            Vector3 bl = new Vector3(-halfW, -halfH, -halfL);
            Vector3 br = new Vector3(halfW, -halfH, -halfL);
            Vector3 fl = new Vector3(-halfW, -halfH, halfL * 0.5f);
            Vector3 fr = new Vector3(halfW, -halfH, halfL * 0.5f);
            Vector3 blt = new Vector3(-halfW, 0, -halfL);
            Vector3 brt = new Vector3(halfW, 0, -halfL);
            Vector3 flt = new Vector3(-halfW, 0, halfL * 0.5f);
            Vector3 frt = new Vector3(halfW, 0, halfL * 0.5f);
            
            // Side pods (wider at rear)
            float sidePodW = halfW * 1.2f;
            Vector3 sbl = new Vector3(-sidePodW, -halfH * 0.8f, -halfL * 0.8f);
            Vector3 sbr = new Vector3(sidePodW, -halfH * 0.8f, -halfL * 0.8f);
            Vector3 sfl = new Vector3(-halfW, -halfH * 0.8f, halfL * 0.3f);
            Vector3 sfr = new Vector3(halfW, -halfH * 0.8f, halfL * 0.3f);
            
            // Bottom face
            AddQuadFace(verts, colors, tris, bl, fl, br, fr, bodyColor);
            
            // Top main body (with cockpit cutout)
            AddQuadFace(verts, colors, tris, blt, brt, flt, frt, bodyColor);
            
            // Left side
            AddQuadFace(verts, colors, tris, bl, blt, fl, flt, bodyColor);
            
            // Right side
            AddQuadFace(verts, colors, tris, br, fr, brt, frt, bodyColor);
            
            // ---- COCKPIT OPENING (black) ----
            float cockpitDepth = halfL * 0.3f;
            Vector3 cbl = new Vector3(-halfW * 0.4f, 0, -halfL * 0.3f);
            Vector3 cbr = new Vector3(halfW * 0.4f, 0, -halfL * 0.3f);
            Vector3 cfl = new Vector3(-halfW * 0.35f, 0, halfL * 0.2f);
            Vector3 cfr = new Vector3(halfW * 0.35f, 0, halfL * 0.2f);
            
            AddQuadFace(verts, colors, tris, cbl, cbr, cfl, cfr, cockpitColor);
            
            // ---- ENGINE COVER (rear section) ----
            Vector3 ebl = new Vector3(-halfW * 0.6f, halfH * 0.5f, -halfL * 0.9f);
            Vector3 ebr = new Vector3(halfW * 0.6f, halfH * 0.5f, -halfL * 0.9f);
            Vector3 efl = new Vector3(-halfW * 0.5f, halfH * 0.3f, -halfL * 0.3f);
            Vector3 efr = new Vector3(halfW * 0.5f, halfH * 0.3f, -halfL * 0.3f);
            
            AddQuadFace(verts, colors, tris, ebl, ebr, efl, efr, bodyColor);
            
            Mesh mesh = new Mesh { name = "F1_Body" };
            mesh.SetVertices(verts);
            mesh.SetColors(colors);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            
            return mesh;
        }
        
        private Mesh BuildWheel()
        {
            // Simple octagonal wheel (8-sided cylinder) for that low-poly look
            int sides = 8;
            List<Vector3> verts = new List<Vector3>();
            List<Color> colors = new List<Color>();
            List<int> tris = new List<int>();
            
            float halfWidth = wheelWidth * 0.5f;
            
            // Create rim vertices
            for (int i = 0; i < sides; i++)
            {
                float angle = i * Mathf.PI * 2f / sides;
                float x = Mathf.Cos(angle) * wheelRadius;
                float y = Mathf.Sin(angle) * wheelRadius;
                
                verts.Add(new Vector3(x, y, -halfWidth));
                verts.Add(new Vector3(x, y, halfWidth));
                colors.Add(wheelColor);
                colors.Add(wheelColor);
            }
            
            // Side faces
            for (int i = 0; i < sides; i++)
            {
                int next = (i + 1) % sides;
                int i0 = i * 2, i1 = i * 2 + 1;
                int n0 = next * 2, n1 = next * 2 + 1;
                
                tris.Add(i0); tris.Add(n0); tris.Add(i1);
                tris.Add(i1); tris.Add(n0); tris.Add(n1);
            }
            
            // End caps (octagonal)
            int centerFront = verts.Count;
            verts.Add(Vector3.forward * halfWidth); colors.Add(wheelColor);
            int centerBack = verts.Count;
            verts.Add(Vector3.back * halfWidth); colors.Add(wheelColor);
            
            for (int i = 0; i < sides; i++)
            {
                int next = (i + 1) % sides;
                tris.Add(centerFront); tris.Add(i * 2); tris.Add(next * 2);
                tris.Add(centerBack); tris.Add(next * 2 + 1); tris.Add(i * 2 + 1);
            }
            
            Mesh mesh = new Mesh { name = "F1_Wheel" };
            mesh.SetVertices(verts);
            mesh.SetColors(colors);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            
            return mesh;
        }
        
        private Mesh BuildRearWing()
        {
            List<Vector3> verts = new List<Vector3>();
            List<Color> colors = new List<Color>();
            List<int> tris = new List<int>();
            
            float halfL = bodyLength * 0.5f;
            float wingSpan = bodyWidth * 1.2f;
            float wingChord = 0.4f;
            float wingHeight = bodyHeight * 0.6f;
            float endplateHeight = wingHeight * 1.2f;
            
            Vector3 wingCenter = new Vector3(0, bodyHeight * 0.4f, -halfL);
            
            // Main wing element
            Vector3 wbl = wingCenter + new Vector3(-wingSpan * 0.5f, -wingChord * 0.5f, 0);
            Vector3 wbr = wingCenter + new Vector3(wingSpan * 0.5f, -wingChord * 0.5f, 0);
            Vector3 wfl = wingCenter + new Vector3(-wingSpan * 0.5f, wingChord * 0.5f, 0);
            Vector3 wfr = wingCenter + new Vector3(wingSpan * 0.5f, wingChord * 0.5f, 0);
            
            AddQuadFace(verts, colors, tris, wbl, wbr, wfl, wfr, rearWingColor);
            
            // Left endplate
            Vector3 epl_b = wbl + Vector3.down * endplateHeight;
            AddQuadFace(verts, colors, tris, wbl, wfl, epl_b, epl_b - Vector3.forward * 0.01f, rearWingColor);
            
            // Right endplate
            Vector3 epr_b = wbr + Vector3.down * endplateHeight;
            AddQuadFace(verts, colors, tris, wbr, epr_b, wfr, epr_b - Vector3.forward * 0.01f, rearWingColor);
            
            Mesh mesh = new Mesh { name = "F1_RearWing" };
            mesh.SetVertices(verts);
            mesh.SetColors(colors);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            
            return mesh;
        }
        
        private void AddQuadFace(List<Vector3> verts, List<Color> colors, List<int> tris,
            Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3, Color color)
        {
            int baseIdx = verts.Count;
            verts.Add(v0); verts.Add(v1); verts.Add(v2); verts.Add(v3);
            colors.Add(color); colors.Add(color); colors.Add(color); colors.Add(color);
            
            tris.Add(baseIdx); tris.Add(baseIdx + 1); tris.Add(baseIdx + 2);
            tris.Add(baseIdx + 1); tris.Add(baseIdx + 3); tris.Add(baseIdx + 2);
        }
        
        /// <summary>
        /// Splits all shared vertices so every face has its own normals.
        /// This is CRITICAL for the flat-shaded look.
        /// </summary>
        private Mesh SplitVerticesForFlatShading(Mesh source)
        {
            Mesh result = new Mesh { name = source.name + "_Flat" };
            
            Vector3[] srcVerts = source.vertices;
            Color[] srcColors = source.colors;
            int[] srcTris = source.triangles;
            
            Vector3[] newVerts = new Vector3[srcTris.Length];
            Color[] newColors = new Color[srcTris.Length];
            Vector3[] newNormals = new Vector3[srcTris.Length];
            int[] newTris = new int[srcTris.Length];
            
            for (int i = 0; i < srcTris.Length; i += 3)
            {
                int i0 = srcTris[i], i1 = srcTris[i + 1], i2 = srcTris[i + 2];
                
                // Compute face normal
                Vector3 edge1 = srcVerts[i1] - srcVerts[i0];
                Vector3 edge2 = srcVerts[i2] - srcVerts[i0];
                Vector3 faceNormal = Vector3.Cross(edge1, edge2).normalized;
                
                // Each vertex in this triangle gets its own copy with face normal
                for (int j = 0; j < 3; j++)
                {
                    int srcIdx = srcTris[i + j];
                    newVerts[i + j] = srcVerts[srcIdx];
                    newColors[i + j] = (srcColors != null && srcColors.Length > srcIdx) ? 
                        srcColors[srcIdx] : Color.white;
                    newNormals[i + j] = faceNormal;
                    newTris[i + j] = i + j;
                }
            }
            
            result.vertices = newVerts;
            result.colors = newColors;
            result.normals = newNormals;
            result.triangles = newTris;
            result.RecalculateBounds();
            
            return result;
        }
    }
}
