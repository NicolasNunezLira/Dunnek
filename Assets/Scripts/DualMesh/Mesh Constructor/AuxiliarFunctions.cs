using System.Collections.Generic;
using UnityEngine;

namespace DunefieldModel_DualMesh
{
    public partial class DualMeshConstructor
    {

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

        /*
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
        */

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