using System.Collections.Generic;
using UnityEngine;

namespace DunefieldModel_DualMesh
{
    [System.Serializable]
    public partial class DualMeshConstructor
    {
        public int resolution;

        public float size, terrainScale1, terrainScale2, terrainScale3,
            terrainAmplitude1, terrainAmplitude2, terrainAmplitude3,
            sandScale1, sandScale2, sandScale3, sandAmplitude1, sandAmplitude2, sandAmplitude3, criticalSlopeThreshold;

        public Material terainMaterial, sandMaterial;

        public float[,] sandElev, terrainElev, terrainShadow;

        public GameObject terrainGO, sandGO;
        public Transform parentTransform;
        public bool planicie;

        public Dictionary<(int, int), Vector2Int> criticalSlopes;

        public DualMeshConstructor(int resolution, float size, float terrainScale1, float terrainScale2, float terrainScale3,
            float terrainAmplitude1, float terrainAmplitude2, float terrainAmplitude3,
            float sandScale1, float sandScale2, float sandScale3, float sandAmplitude1, float sandAmplitude2, float sandAmplitude3,
            Material terainMaterial, Material sandMaterial, Dictionary<(int, int), Vector2Int> criticalSlopes, float criticalSlopeThreshold, ref bool planicie, Transform parentTransform = null
            )
        {
            this.resolution = resolution;
            this.size = size;
            this.terrainScale1 = terrainScale1;
            this.terrainScale2 = terrainScale2;
            this.terrainScale3 = terrainScale3;
            this.terrainAmplitude1 = terrainAmplitude1;
            this.terrainAmplitude2 = terrainAmplitude2;
            this.terrainAmplitude3 = terrainAmplitude3;
            this.sandScale1 = sandScale1;
            this.sandScale2 = sandScale2;
            this.sandScale3 = sandScale3;
            this.sandAmplitude1 = sandAmplitude1;
            this.sandAmplitude2 = sandAmplitude2;
            this.sandAmplitude3 = sandAmplitude3;

            this.terainMaterial = terainMaterial;
            this.sandMaterial = sandMaterial;
            this.parentTransform = parentTransform;
            this.planicie = planicie;

            this.criticalSlopes = criticalSlopes;
            this.criticalSlopeThreshold = criticalSlopeThreshold;
        }

        public void Initialize(out GameObject terrainGO, out GameObject sandGO,
            out float[,] terrainElev, out float[,] sandElev, out float[,] terrainShadow)
        {
            /// <summary>
            /// Initializes the terrain and sand meshes, creating GameObjects for each.
            /// /// </summary>
            /// /// <param name="terrainGO">Output GameObject for the terrain mesh.</param>
            /// <param name="sandGO">Output GameObject for the sand mesh.</param>
            /// /// <param name="terrainElev">Output height map for the terrain mesh.</param>
            /// <param name="sandElev">Output height map for the sand mesh.</param>


            // Creación del terreno
            terrainGO = CreateMeshObject("TerrainMesh", terainMaterial,
                !planicie ? GenerateMesh(terrainScale1, terrainAmplitude1, terrainScale2, terrainAmplitude2, terrainScale3, terrainAmplitude3, false, "terrain") : GenerateMountain());
            if (parentTransform != null)
                terrainGO.transform.parent = parentTransform;

            sandGO = CreateMeshObject("SandMesh", sandMaterial,
                GenerateMesh(sandScale1, sandAmplitude1, sandScale2, sandAmplitude2, sandScale3, sandAmplitude3, false, "sand"));
            if (parentTransform != null)
                sandGO.transform.parent = parentTransform;
            GenerateMesh(sandScale1, sandAmplitude1, sandScale2, sandAmplitude2, sandScale3, sandAmplitude3);
            sandGO.transform.parent = parentTransform;

            // Adjust the terrain mesh to be above the sand mesh
            // This assumes the terrain mesh is higher than the sand mesh
            float terrainMinY = GetMinYFromMesh(terrainGO.GetComponent<MeshFilter>().mesh);
            float terrainMaxY = GetMaxYFromMesh(terrainGO.GetComponent<MeshFilter>().mesh);
            float sandMinY = GetMinYFromMesh(sandGO.GetComponent<MeshFilter>().mesh);

            float offset = (terrainMaxY + terrainMinY) * 0.5f - sandMinY + 0.005f * (terrainMaxY - terrainMinY);
            Mesh terrainMesh = terrainGO.GetComponent<MeshFilter>().mesh;
            Vector3[] vertices = terrainMesh.vertices;
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i].y -= offset;
            }
            terrainMesh.vertices = vertices;
            terrainMesh.RecalculateNormals();
            terrainMesh.RecalculateBounds();


            RegularizeMesh(sandGO.GetComponent<MeshFilter>().mesh, terrainGO.GetComponent<MeshFilter>().mesh);

            sandElev = MeshToHeightMap(sandGO.GetComponent<MeshFilter>().mesh, resolution);
            terrainElev = MeshToHeightMap(terrainGO.GetComponent<MeshFilter>().mesh, resolution);

            terrainShadow = CopyArray(terrainElev);

            sandGO.GetComponent<MeshCollider>().sharedMesh = terrainGO.GetComponent<MeshFilter>().mesh;
            terrainGO.GetComponent<MeshCollider>().sharedMesh = terrainGO.GetComponent<MeshFilter>().mesh;

            sandGO.GetComponent<MeshFilter>().mesh.MarkDynamic();
            terrainGO.GetComponent<MeshFilter>().mesh.MarkDynamic();
        }

    }
}