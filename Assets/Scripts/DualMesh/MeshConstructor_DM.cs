using UnityEngine;

namespace DunefieldModel_DualMesh
{
    public partial class DualMeshConstructor
    {
        public int resolution;

        public float size, terrainScale1, terrainScale2, terrainScale3,
            terrainAmplitude1, terrainAmplitude2, terrainAmplitude3,
            sandScale1, sandScale2, sandScale3, sandAmplitude1, sandAmplitude2, sandAmplitude3;

        public Material terainMaterial, sandMaterial;

        public float[,] sandElev, terrainElev;

        public GameObject terrainGO, sandGO;

        public DualMeshConstructor(int resolution, float size, float terrainScale1, float terrainScale2, float terrainScale3,
            float terrainAmplitude1, float terrainAmplitude2, float terrainAmplitude3,
            float sandScale1, float sandScale2, float sandScale3, float sandAmplitude1, float sandAmplitude2, float sandAmplitude3,
            Material terainMaterial, Material sandMaterial)
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
        } 
    }
}