using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace DunefieldModel_DualMesh
{
    [System.Serializable]
    public partial class DualMeshConstructor
    {
        public int xResolution, zResolution, simXResolution, simZResolution;

        public float size, terrainScale1, terrainScale2, terrainScale3,
            terrainAmplitude1, terrainAmplitude2, terrainAmplitude3,
            sandScale1, sandScale2, sandScale3, sandAmplitude1, sandAmplitude2, sandAmplitude3, criticalSlopeThreshold;

        public Material terainMaterial, sandMaterial;

        //public float[,] sandElev, terrainElev, terrainShadow;
        public NativeGrid sand, terrain, terrainShadow;

        public GameObject terrainGO, sandGO;
        public Transform parentTransform;
        public bool planicie;

        private int xDOF => xResolution + 1;
        private int zDOF => zResolution + 1;
        private int simXDOF => simXResolution + 1;
        private int simZDOF => simZResolution + 1;

        public Dictionary<(int, int), Vector2Int> criticalSlopes;

        //public ResourceSystem.ResourceManager resourceManager;

        public DualMeshConstructor(
            int xResolution, int zResolution, // Visual Mesh resolutions
            int simXResolution, int simZResolution, // Simulation mesh resolution
            float size, // size for visual mesh
            float terrainScale1, float terrainScale2, float terrainScale3,
            float terrainAmplitude1, float terrainAmplitude2, float terrainAmplitude3,
            float sandScale1, float sandScale2, float sandScale3,
            float sandAmplitude1, float sandAmplitude2, float sandAmplitude3,
            Material terainMaterial, Material sandMaterial,
            Dictionary<(int, int), Vector2Int> criticalSlopes, float criticalSlopeThreshold,
            ref bool planicie, Transform parentTransform = null
            )
        {
            this.xResolution = xResolution;
            this.zResolution = zResolution;
            this.simXResolution = simXResolution;
            this.simZResolution = simZResolution;

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
            out NativeGrid sand, out NativeGrid terrain, out NativeGrid terrainShadow)
        {
            /// <summary>
            /// Initializes the terrain and sand meshes, creating GameObjects for each.
            /// /// </summary>
            /// /// <param name="terrainGO">Output GameObject for the terrain mesh.</param>
            /// <param name="sandGO">Output GameObject for the sand mesh.</param>
            /// /// <param name="terrainElev">Output height map for the terrain mesh.</param>
            /// <param name="sandElev">Output height map for the sand mesh.</param>

            sand = CreateSimulationMeshes(sandScale1, sandAmplitude1, sandScale2, sandAmplitude2, sandScale3, sandAmplitude3);
            terrain = !planicie ? CreateSimulationMeshes(terrainScale1, terrainAmplitude1, terrainScale2, terrainAmplitude2, terrainScale3, terrainAmplitude3) :
                       GenerateMountain();

            // Creación del terreno
            terrainGO = CreateMeshObject("TerrainMesh", terainMaterial,
                GenerateMesh(terrain));
            if (parentTransform != null)
                terrainGO.transform.parent = parentTransform;

            sandGO = CreateMeshObject("SandMesh", sandMaterial,
                GenerateMesh(sand));
            if (parentTransform != null)
                sandGO.transform.parent = parentTransform;


            ApplyOffset(terrainGO, sandGO, terrain);

            RegularizeMesh(sandGO.GetComponent<MeshFilter>().mesh, terrainGO.GetComponent<MeshFilter>().mesh);

            sandGO.GetComponent<MeshCollider>().sharedMesh = terrainGO.GetComponent<MeshFilter>().mesh;
            terrainGO.GetComponent<MeshCollider>().sharedMesh = terrainGO.GetComponent<MeshFilter>().mesh;

            sandGO.GetComponent<MeshFilter>().mesh.MarkDynamic();
            terrainGO.GetComponent<MeshFilter>().mesh.MarkDynamic();
            
            terrainShadow = terrain.Clone(Allocator.Persistent);
        }
    }
}