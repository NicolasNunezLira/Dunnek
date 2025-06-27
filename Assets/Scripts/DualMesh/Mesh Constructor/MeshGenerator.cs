using System.Collections.Generic;
using UnityEngine;

namespace DunefieldModel_DualMesh
{
    public partial class DualMeshConstructor
    {
        #region Perlings Meshes
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
        #endregion

        #region Truncated cone Meshes

        Mesh GenerateMountain()
        {
            // Generate the terrain mesh
            Mesh mesh = new Mesh();
            Vector3[] vertices = new Vector3[(resolution + 1) * (resolution + 1)];
            Vector2[] uv = new Vector2[vertices.Length];
            int[] triangles = new int[resolution * resolution * 6];
            float xCenter = size / 2;
            float zCenter = size / 2;

            for (int i = 0, z = 0; z <= resolution; z++)
            {
                for (int x = 0; x <= resolution; x++, i++)
                {
                    float xPos = (float)x / resolution * size;
                    float zPos = (float)z / resolution * size;
                    float yPos = GetTruncatedConeHeight(xPos, zPos, xCenter, zCenter, size / 3, size / 8, 5f);
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

        float GetTruncatedConeHeight(float x, float z, float xCenter, float zCenter, float radius, float flatRadius, float maxHeight)
        {
            float dx = x - xCenter;
            float dz = z - zCenter;
            float r = Mathf.Sqrt(dx * dx + dz * dz);

            if (r <= flatRadius)
                return maxHeight;
            else if (r > radius)
                return 0f;
            else
                return Mathf.Lerp(0, maxHeight, (radius - r) / (radius - flatRadius));
        }
        #endregion

    }
}