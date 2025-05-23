//using DunefieldModel;
using DunefieldModel_8D;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ProceduralPlane : MonoBehaviour
{
    [Header("Plane Settings")]
    [Range(31, 255)]
    [Tooltip("The number of subdivisions along each axis.")]
    public int resolution = 127;

    [Header("Mesh Settings")]
    [Tooltip("The size of the plane in world units.")]
    public float size = 10f;

    [Header("Perlin Noise Layers")]
    [Tooltip("The scale of the first layer of Perlin noise.")]
    public float scale1 = 0.01f;
    public float amplitude1 = 5f;
    public float scale2 = 0.05f;
    public float amplitude2 = 2f;
    public float scale3 = 0.15f;
    public float amplitude3 = 1f;

    [Header("Simulation Settings")]
    [Tooltip("The height variation of the terrain.")]
    public float heightVariation = 0.1f;

    [Tooltip("The slope of the terrain.")]
    public float slope = 0.2f;
    [Tooltip("The direction of the wind.")]
    public Vector2 windDirection = new Vector2(1, 0);
    [Tooltip("The number of grains per step.")]
    public int grainsPerStep = 5000;

    private Model8D duneModel;
    private FindSlopeMooreDeterministic slopeFinder;

    public float[,] Elev;

    public Mesh mesh;

    void Start()
    {
        // Generate the terrain mesh
        mesh = new Mesh();
        Vector3[] vertices = new Vector3[(resolution + 1) * (resolution + 1)];
        Vector2[] uv = new Vector2[vertices.Length];
        int[] triangles = new int[resolution * resolution * 6];
        Elev = new float[resolution + 1, resolution + 1];

        for (int i = 0, z = 0; z <= resolution; z++)
        {
            for (int x = 0; x <= resolution; x++, i++)
            {
                float xPos = (float)x;// / resolution * size;
                float yPos = 2 * GetMultiScalePerlinHeight(x, z);// / resolution * size;
                float zPos = (float)z;// / resolution * size;
                Elev[x, z] = yPos;
                vertices[i] = new Vector3(xPos, yPos, zPos);
                uv[i] = new Vector2((float)x / resolution,
                    (float)z / resolution);
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

        // Initialize the dune model
        slopeFinder = new FindSlopeMooreDeterministic();
        duneModel = new Model8D(slopeFinder, Elev, resolution + 1, resolution + 1, slope, (int)windDirection.x, (int)windDirection.y);
        duneModel.shadowInit();
    }

    void Update()
    {
        duneModel.Tick(grainsPerStep, (int)windDirection.x, (int)windDirection.y, heightVariation, heightVariation);
        //Elev = SmoothHeights(Elev, 4, 4);
        
        Vector3[] vertices = mesh.vertices;
        Vector3[] vertices2 = new Vector3[(int)vertices.Length];

        for (int y = 0; y <= resolution; y++)
        {
            for (int x = 0; x <= resolution; x++)
            {
                int index = y * resolution + x;
                float h = Elev[x, y];

                vertices2[index] = new Vector3(vertices[index].x, h, vertices[index].z);
            }
        }

        mesh.vertices = vertices2;
        mesh.RecalculateNormals();
    }

    float GetMultiScalePerlinHeight(int x, int z)
    {
        float h1 = Mathf.PerlinNoise(x * scale1, z * scale1) * amplitude1;
        float h2 = Mathf.PerlinNoise(x * scale2, z * scale2) * amplitude2;
        float h3 = Mathf.PerlinNoise(x * scale3, z * scale3) * amplitude3;
        return h1 + h2 + h3;
    }
    
    float[,] SmoothHeights(float[,] original, int width, int height)
    {
        float[,] smoothed = new float[width, height];

        for (int x = 1; x < width - 1; x++)
        {
            for (int y = 1; y < height - 1; y++)
            {
                float sum = 0f;
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        sum += original[x + dx, y + dy];
                    }
                }
                smoothed[x, y] = sum / 9f;  // Promedio de vecinos 3x3
            }
        }

        return smoothed;
    }
}

