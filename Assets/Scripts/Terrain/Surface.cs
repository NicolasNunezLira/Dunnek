using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ProceduralPlane : MonoBehaviour
{
    [Header("Plane Settings")]
    [Range(1, 100)]
    [Tooltip("The number of subdivisions along each axis.")]
    public int resolution = 10;

    [Header("Mesh Settings")]
    [Tooltip("The size of the plane in world units.")]
    public float size = 10f;

    void Start()
    {
        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[(resolution + 1) * (resolution + 1)];
        Vector2[] uv = new Vector2[vertices.Length];
        int[] triangles = new int[resolution * resolution * 6];

        for (int i = 0, z = 0; z <= resolution; z++)
        {
            for (int x = 0; x <= resolution; x++, i++)
            {
                float xPos = (float)x / resolution * size;
                float zPos = (float)z / resolution * size;
                vertices[i] = new Vector3(xPos, 0, zPos);
                uv[i] = new Vector2((float)x / resolution, (float)z / resolution);
            }
        }

        for (int ti = 0, vi = 0, z = 0; z < resolution; z++, vi++)
        {
            for (int x = 0; x < resolution; x++, ti += 6, vi++)
            {
                triangles[ti] = vi;
                triangles[ti + 3] = triangles[ti + 2] = vi + 1;
                triangles[ti + 4] = triangles[ti + 1] = vi + resolution + 1;
                triangles[ti + 5] = vi + resolution + 2;
            }
        }

        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        GetComponent<MeshFilter>().mesh = mesh;
    }
}

