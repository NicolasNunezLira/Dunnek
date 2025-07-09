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

        public void ApplyHeightMapToMesh(Mesh mesh, NativeGrid heightMap)
        {
            Vector3[] vertices = mesh.vertices;

            for (int z = 0; z <= zResolution; z++)
            {
                for (int x = 0; x <= xResolution; x++)
                {
                    int index = z * (zResolution + 1) + x;
                    Vector3 v = vertices[index];
                    v.y = heightMap[x, z];
                    vertices[index] = v;
                }
            }

            mesh.vertices = vertices;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }

        public void RegularizeMesh(
            Mesh sandMesh, Mesh terrainMesh
        )
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

        public void ApplyOffset(GameObject terrainGO, GameObject sandGO, NativeGrid terrain)
        {
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
            for (int i = 0; i < terrain.data.Length; i++)
            {
                terrain.data[i] -= offset;
            }
        }
    }

}