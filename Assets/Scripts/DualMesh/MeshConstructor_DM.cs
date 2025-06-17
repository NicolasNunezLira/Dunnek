using System.Collections.Generic;
using UnityEngine;

namespace DunefieldModel_DualMesh
{
    public partial class DualMeshConstructor
    {
        public int resolution;

        public float size, terrainScale1, terrainScale2, terrainScale3,
            terrainAmplitude1, terrainAmplitude2, terrainAmplitude3,
            sandScale1, sandScale2, sandScale3, sandAmplitude1, sandAmplitude2, sandAmplitude3, criticalSlopeThreshold;

        public Material terainMaterial, sandMaterial;

        public float[,] sandElev, terrainElev, terrainGhost;

        public GameObject terrainGO, sandGO;
        public Transform parentTransform;

        public Dictionary<(int, int), Vector2Int> criticalSlopes;

        public DualMeshConstructor(int resolution, float size, float terrainScale1, float terrainScale2, float terrainScale3,
            float terrainAmplitude1, float terrainAmplitude2, float terrainAmplitude3,
            float sandScale1, float sandScale2, float sandScale3, float sandAmplitude1, float sandAmplitude2, float sandAmplitude3,
            Material terainMaterial, Material sandMaterial, Dictionary<(int, int), Vector2Int> criticalSlopes, float criticalSlopeThreshold, Transform parentTransform = null)
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

            this.criticalSlopes = criticalSlopes;
            this.criticalSlopeThreshold = criticalSlopeThreshold;
        }

        public void Initialize(out GameObject terrainGO, out GameObject sandGO,
            out float[,] terrainElev, out float[,] sandElev)
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
                GenerateMesh(terrainScale1, terrainAmplitude1, terrainScale2, terrainAmplitude2, terrainScale3, terrainAmplitude3, false, "terrain"));
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

            terrainGhost = CopyArray(terrainElev);

            sandGO.GetComponent<MeshCollider>().sharedMesh = terrainGO.GetComponent<MeshFilter>().mesh;
            terrainGO.GetComponent<MeshCollider>().sharedMesh = terrainGO.GetComponent<MeshFilter>().mesh;

            sandGO.GetComponent<MeshFilter>().mesh.MarkDynamic();
            terrainGO.GetComponent<MeshFilter>().mesh.MarkDynamic();
        }

        Mesh GenerateMesh(float scale1, float amplitude1, float scale2, float amplitude2, float scale3, float amplitude3, bool cube = false, string materialCube = null)
        {
            // Generate the terrain mesh
            Mesh mesh = new Mesh();
            Vector3[] vertices = new Vector3[(resolution + 1) * (resolution + 1)];
            Vector2[] uv = new Vector2[vertices.Length];
            int[] triangles = new int[resolution * resolution * 6];

            for (int i = 0, z = 0; z <= resolution; z++)
            {
                for (int x = 0; x <= resolution; x++, i++)
                {
                    float xPos = (float)x / resolution * size;
                    float yPos = 2 * GetMultiScalePerlinHeight(x, z, scale1, amplitude1, scale2, amplitude2, scale3, amplitude3);/// resolution * size;
                    if (cube && materialCube == "terrain" && z > 150 - 40 && z < 170 - 40 && x > 100 && x < 120)
                    {
                        yPos = 25;
                    }

                    if (cube && materialCube == "sand" && z > 150 - 40 && z < 270 - 40 && x > 100 && x < 220)
                    {
                        yPos = 5;
                    }

                    float zPos = (float)z / resolution * size;
                    vertices[i] = new Vector3(xPos, yPos, zPos);
                    uv[i] = new Vector2((float)x / resolution,
                        (float)z / resolution);
                }
            }

            for (int ti = 0, vi = 0, z = 0; z < resolution; z++)
            {
                for (int x = 0; x < resolution; x++, ti += 6, vi++)
                {
                    triangles[ti] = vi;
                    triangles[ti + 1] = vi + resolution + 1;
                    triangles[ti + 2] = vi + 1;

                    triangles[ti + 3] = vi + 1;
                    triangles[ti + 4] = vi + resolution + 1;
                    triangles[ti + 5] = vi + resolution + 2;
                }
                vi++;
            }

            if ((resolution + 1) * (resolution + 1) > 65000)
            {
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
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

            obj.GetComponent<MeshFilter>().mesh = mesh;
            obj.GetComponent<MeshRenderer>().material = material;

            var meshCollider = obj.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = mesh;
            meshCollider.convex = false;

            obj.layer = LayerMask.NameToLayer("Terrain");

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

        public float[,] MeshToHeightMap(Mesh mesh, int resolution)
        {
            float[,] heightMap = new float[resolution + 1, resolution + 1];
            Vector3[] vertices = mesh.vertices;

            for (int z = 0; z <= resolution; z++)
            {
                for (int x = 0; x <= resolution; x++)
                {
                    int index = z * (resolution + 1) + x;
                    heightMap[x, z] = vertices[index].y;
                }
            }

            return heightMap;
        }

        public void ApplyHeightMapToMesh(Mesh mesh, float[,] heightMap)
        {
            Vector3[] vertices = mesh.vertices;
            int resolution = heightMap.GetLength(0) - 1;

            for (int z = 0; z <= resolution; z++)
            {
                for (int x = 0; x <= resolution; x++)
                {
                    int index = z * (resolution + 1) + x;
                    Vector3 v = vertices[index];
                    v.y = heightMap[x, z];
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

        public Dictionary<(int, int), Vector2Int> InitializeCriticalSlopes(float criticalSlopeThreshold)
        {
            criticalSlopes.Clear();
            int width = sandElev.GetLength(0);
            int height = sandElev.GetLength(1);

            for (int x = 1; x < width - 1; x++)
            {
                for (int z = 1; z < height - 1; z++)
                {
                    if (sandElev[x, z] < terrainElev[x, z])
                    {
                        // Si la elevación de la arena es menor que la del terreno, no hay pendiente crítica
                        continue;
                    }

                    float h = sandElev[x, z];

                    // Revisar las 4 direcciones principales
                    Vector2Int[] directions = {
                        new(1, 0), new(-1, 0),
                        new(0, 1), new(0, -1),
                        new(1, 1), new(-1, -1),
                        new(-1, 1), new(1, -1)
                    };

                    float minSlope = float.PositiveInfinity;

                    foreach (var dir in directions)
                    {

                        int xn = x + dir.x;
                        int zn = z + dir.y;

                        if (IsOutside(xn, zn))
                        {
                            continue; // Ignorar fuera de límites
                        }

                        float hNeighbor = sandElev[xn, zn];

                        float slope = h - hNeighbor; // o usar Mathf.Atan para ángulo
                        if (slope > criticalSlopeThreshold && slope < minSlope)
                        {
                            // Registrar punto crítico y dirección
                            criticalSlopes[(x, z)] = dir;
                        }
                    }
                }
            }

            return criticalSlopes;
        }

        public bool IsOutside(int x, int z)
        {
            return x < 0 || x >= sandElev.GetLength(0) || z < 0 || z >= sandElev.GetLength(1);
        }
        
        public int WorldToIndex(float worldCoord) => Mathf.FloorToInt(worldCoord * resolution / size);

        public float[,] CopyArray(float[,] source)
        {
            int rows = source.GetLength(0);
            int cols = source.GetLength(1);
            float[,] copy = new float[rows, cols];

            for (int i = 0; i < rows; i++)
                for (int j = 0; j < cols; j++)
                    copy[i, j] = source[i, j];

            return copy;
        }


    }
}