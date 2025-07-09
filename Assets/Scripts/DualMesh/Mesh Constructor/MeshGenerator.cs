using UnityEngine;
using Unity.Collections;

namespace DunefieldModel_DualMesh
{
    public partial class DualMeshConstructor
    {
        #region Perlings Meshes

        public NativeGrid CreateSimulationMeshes
        (
            float scale1, float amplitude1,
            float scale2, float amplitude2,
            float scale3, float amplitude3,
            bool cube = false,
            string materialCube = null
        )
        {
            NativeGrid mesh = new NativeGrid(simXResolution, simZResolution, Allocator.Persistent);
            for (int x = 0; x < simXResolution; x++)
            {
                for (int z = 0; z < simZResolution; z++)
                {
                    if (cube && materialCube == "terrain" && z > 150 - 40 && z < 170 - 40 && x > 100 && x < 120)
                    {
                        mesh[x, z] = 25; continue;
                    }

                    if (cube && materialCube == "sand" && z > 150 - 40 && z < 270 - 40 && x > 100 && x < 220)
                    {
                        mesh[x, z] = 5; continue;
                    }
                    
                    mesh[x, z] = 2 * GetMultiScalePerlinHeight(
                        x, z,
                        scale1, amplitude1,
                        scale2, amplitude2,
                        scale3, amplitude3
                    );
                }
            }
            return mesh;
        }
        Mesh GenerateMesh(NativeGrid grid)
        {
            // Generate the terrain mesh
            Mesh mesh = new Mesh();
            Vector3[] vertices = new Vector3[(xResolution + 1) * (zResolution + 1)];
            Vector2[] uv = new Vector2[vertices.Length];
            int[] triangles = new int[xResolution * zResolution * 6];

            for (int i = 0, z = 0; z <= zResolution; z++)
            {
                for (int x = 0; x <= xResolution; x++, i++)
                {
                    float xPos = (float)x / xResolution * size;
                    float yPos = grid[x, z];
                    float zPos = (float)z / zResolution * size;
                    vertices[i] = new Vector3(xPos, yPos, zPos);
                    uv[i] = new Vector2((float)x / xResolution,
                        (float)z / zResolution);
                }
            }

            for (int ti = 0, vi = 0, z = 0; z < zResolution; z++)
            {
                for (int x = 0; x < xResolution; x++, ti += 6, vi++)
                {
                    triangles[ti] = vi;
                    triangles[ti + 1] = vi + zResolution + 1;
                    triangles[ti + 2] = vi + 1;

                    triangles[ti + 3] = vi + 1;
                    triangles[ti + 4] = vi + zResolution + 1;
                    triangles[ti + 5] = vi + zResolution + 2;
                }
                vi++;
            }

            if ((xResolution + 1) * (zResolution + 1) > 65000)
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

        NativeGrid GenerateMountain()
        {
            float xCenter = size / 2;
            float zCenter = size / 2;

            NativeGrid grid = new NativeGrid(simXResolution, simZResolution, Allocator.Persistent);
            for (int x = 0; x < simXResolution; x++)
            {
                for (int z = 0; z < simZResolution; z++)
                {
                    grid[x, z] = GetTruncatedConeHeight(x, z, xCenter, zCenter, size / 3, size / 8, 5f);
                }
            }

            return grid;                    
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