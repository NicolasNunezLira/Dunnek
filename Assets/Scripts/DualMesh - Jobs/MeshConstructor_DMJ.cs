using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Jobs;
using Unity.Collections;
using Unity.Burst;

namespace DunefieldModel_DualMeshJobs
{
    public partial class DualMeshConstructor
    {
        public int xResolution, zResolution;

        public float size, terrainScale1, terrainScale2, terrainScale3,
            terrainAmplitude1, terrainAmplitude2, terrainAmplitude3,
            sandScale1, sandScale2, sandScale3, sandAmplitude1, sandAmplitude2, sandAmplitude3;

        public Material terainMaterial, sandMaterial;

        public NativeArray<float> sandElev, terrainElev;

        public GameObject terrainGO, sandGO;
        public Transform parentTransform;


        public DualMeshConstructor(int xResolution, int zResolution, float size, float terrainScale1, float terrainScale2, float terrainScale3,
            float terrainAmplitude1, float terrainAmplitude2, float terrainAmplitude3,
            float sandScale1, float sandScale2, float sandScale3, float sandAmplitude1, float sandAmplitude2, float sandAmplitude3,
            Material terainMaterial, Material sandMaterial, Transform parentTransform = null)
        {
            this.xResolution = xResolution;
            this.zResolution = zResolution;
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
        }

        public void Initialize(out GameObject terrainGO, out GameObject sandGO,
            out NativeArray<float> terrainElev, out NativeArray<float> sandElev)
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
                GenerateMesh(terrainScale1, terrainAmplitude1, terrainScale2, terrainAmplitude2, terrainScale3, terrainAmplitude3, false));
            if (parentTransform != null)
                terrainGO.transform.parent = parentTransform;

            sandGO = CreateMeshObject("SandMesh", sandMaterial,
                GenerateMesh(sandScale1, sandAmplitude1, sandScale2, sandAmplitude2, sandScale3, sandAmplitude3));
            if (parentTransform != null)
                sandGO.transform.parent = parentTransform;
            GenerateMesh(sandScale1, sandAmplitude1, sandScale2, sandAmplitude2, sandScale3, sandAmplitude3);
            sandGO.transform.parent = parentTransform;

            // Adjust the terrain mesh to be above the sand mesh
            // This assumes the terrain mesh is higher than the sand mesh
            float terrainMinY = GetMinYFromMesh(terrainGO.GetComponent<MeshFilter>().mesh);
            float terrainMaxY = GetMaxYFromMesh(terrainGO.GetComponent<MeshFilter>().mesh);
            float sandMinY = GetMinYFromMesh(sandGO.GetComponent<MeshFilter>().mesh);
            //float sandMaxY = GetMaxYFromMesh(sandGO.GetComponent<MeshFilter>().mesh);

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

            sandElev = MeshToHeightMap(sandGO.GetComponent<MeshFilter>().mesh, xResolution, zResolution);
            terrainElev = MeshToHeightMap(terrainGO.GetComponent<MeshFilter>().mesh, xResolution, zResolution);

            //criticalSlopes = InitializeCriticalSlopes(criticalSlopeThreshold);
        }

        Mesh GenerateMesh(float scale1, float amplitude1, float scale2, float amplitude2, float scale3, float amplitude3, bool onlySand = false)
        {
            // Generate the terrain mesh
            Mesh mesh = new Mesh();
            Vector3[] vertices = new Vector3[(xResolution + 1) * (zResolution + 1)];
            Vector2[] uv = new Vector2[vertices.Length];
            int[] triangles = new int[xResolution * zResolution * 6];

            for (int i = 0, z = 0; z <= zResolution; z++)
            {
                for (int x = 0; x <= zResolution; x++, i++)
                {
                    float xPos = (float)x / xResolution * size;
                    float yPos = 2 * GetMultiScalePerlinHeight(x, z, scale1, amplitude1, scale2, amplitude2, scale3, amplitude3);/// resolution * size;
                    if (onlySand && z > 50 && z < 70 && x > 100 && x < 120)
                    {
                        yPos = 32;
                    }
                    /*if (!onlySand && z > 150 && z < 270 && x > 100 && x < 220)
                    {
                        yPos = 32;
                    } */
                    float zPos = (float)z / zResolution * size;
                    vertices[i] = new Vector3(xPos, yPos, zPos);
                    uv[i] = new Vector2((float)x / xResolution,
                        (float)z / zResolution);
                }
            }

            for (int ti = 0, vi = 0, z = 0; z < zResolution; z++, vi++)
            {
                for (int x = 0; x < xResolution; x++, ti += 6, vi++)
                {
                    triangles[ti] = vi;
                    triangles[ti + 1] = vi + xResolution + 1;
                    triangles[ti + 2] = vi + 1;

                    triangles[ti + 3] = vi + 1;
                    triangles[ti + 4] = vi + xResolution + 1;
                    triangles[ti + 5] = vi + xResolution + 2;
                }
            }

            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        GameObject CreateMeshObject(string name, Material material, Mesh mesh)
        {
            GameObject obj = new GameObject(name);
            obj.AddComponent<MeshFilter>();
            obj.AddComponent<MeshRenderer>();

            //Mesh mesh = GenerateMesh(heightMap);
            obj.GetComponent<MeshFilter>().mesh = mesh;
            obj.GetComponent<MeshRenderer>().material = material;

            return obj;
        }


        float GetMultiScalePerlinHeight(int x, int z, float scale1, float amplitude1,
        float scale2, float amplitude2, float scale3, float amplitude3)
        {
            float h1 = Mathf.PerlinNoise(x * scale1, z * scale1) * amplitude1;
            float h2 = Mathf.PerlinNoise(x * scale2, z * scale2) * amplitude2;
            float h3 = Mathf.PerlinNoise(x * scale3, z * scale3) * amplitude3;
            return h1 + h2 + h3;
        }

        float GetMaxYFromMesh(Mesh mesh)
        {
            float maxY = float.MinValue;
            foreach (Vector3 vertex in mesh.vertices)
            {
                if (vertex.y > maxY)
                    maxY = vertex.y;
            }
            return maxY;
        }

        float GetMinYFromMesh(Mesh mesh)
        {
            float minY = float.MaxValue;
            foreach (Vector3 vertex in mesh.vertices)
            {
                if (vertex.y < minY)
                    minY = vertex.y;
            }
            return minY;
        }

        public NativeArray<float> MeshToHeightMap(Mesh mesh, int xResolution, int zResolution)
        {
            NativeArray<float> heightMap = new NativeArray<float>((xResolution + 1) * (zResolution + 1), Allocator.Persistent);
            Vector3[] vertices = mesh.vertices;

            for (int z = 0; z <= zResolution; z++)
            {
                for (int x = 0; x <= xResolution; x++)
                {
                    int index = z * (xResolution + 1) + x;
                    heightMap[index] = vertices[index].y;
                }
            }

            return heightMap;
        }

        public void ApplyHeightMapToMesh(Mesh mesh, NativeArray<float> heightMap, int xResolution, int zResolution)
        {
            Vector3[] vertices = mesh.vertices;

            for (int z = 0; z <= zResolution; z++)
            {
                for (int x = 0; x <= xResolution; x++)
                {
                    int index = z * (xResolution + 1) + x;
                    Vector3 v = vertices[index];
                    v.y = heightMap[index];
                    vertices[index] = v;
                }
            }

            mesh.vertices = vertices;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }

        public void RegularizeMesh(Mesh sandMesh, Mesh terrainMesh)
        {
            // Regularize the sand mesh to match the terrain mesh
            Vector3[] sandVertices = sandMesh.vertices;
            Vector3[] terrainVertices = terrainMesh.vertices;

            for (int i = 0; i < sandVertices.Length; i++)
            {
                if (sandVertices[i].y < terrainVertices[i].y)
                {
                    sandVertices[i].y = terrainVertices[i].y * (1f - 0.05f);
                }
            }

            sandMesh.vertices = sandVertices;
            sandMesh.RecalculateNormals();
            sandMesh.RecalculateBounds();
        }
    }
}