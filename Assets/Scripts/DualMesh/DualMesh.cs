using UnityEngine;
using DunefieldModel_DualMesh;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class DualMesh : MonoBehaviour
{
    [Header("Plane Settings")]
    [Range(31, 511)]
    [Tooltip("The number of subdivisions along each axis.")]
    public int resolution = 127;

    [Header("Mesh Settings")]
    [Tooltip("The size of the plane in world units.")]
    public float size = 10f;

    [Header("Terrain Settings")]
    [Tooltip("Meterial.")]
    public Material terrainMaterial;

    [Tooltip("Perlin noise parameters.")]
    public float terrainScale1 = 0.003f;
    public float terrainAmplitude1 = 8f;
    public float terrainScale2 = 0.01f;
    public float terrainAmplitude2 = 4f;
    public float terrainScale3 = 0.05f;
    public float terrainAmplitude3 = 2f;

    [Header("Sand Settings")]
    [Tooltip("Meterial.")]
    public Material sandMaterial;

    [Tooltip("Perlin noise parameters.")]
    public float sandScale1 = 0.02f;
    public float sandAmplitude1 = 1.5f;
    public float sandScale2 = 0.06f;
    public float sandAmplitude2 = 0.7f;
    public float sandScale3 = 0.1f;
    public float sandAmplitude3 = 0.3f;

    public float[,] terrainElev, sandElev;

    [Header("Simulation Settings")]
    [Tooltip("The height variation of the terrain.")]
    public float heightVariation = 0.1f;

    [Tooltip("The slope of the terrain.")]
    public float slope = 0.2f;

    [Tooltip("The slope for avalanches.")]
    public float avalancheSlope = .5f;

    [Tooltip("The hop length for the simulation.")]
    public int hopLength = 1;

    [Tooltip("Shadow slope for the simulation.")]
    public float shadowSlope = 0.803847577f; // 3 * tan(15 degrees) ~ 0.803847577f

    [Tooltip("The direction of the wind.")]
    public Vector2 windDirection = new Vector2(1, 0);
    [Tooltip("The number of grains per step.")]
    public int grainsPerStep = 5000;

    private GameObject terrainGO, sandGO;

    private ModelDM duneModel;
    private FindSlopeMooreDeterministic slopeFinder;

    void Start()
    {
        // Creación del terreno
        terrainGO = CreateMeshObject("TerrainMesh", terrainMaterial,
            GenerateMesh(terrainScale1, terrainAmplitude1, terrainScale2, terrainAmplitude2, terrainScale3, terrainAmplitude3, false));

        terrainGO.transform.parent = this.transform;

        sandGO = CreateMeshObject("SandMesh", sandMaterial,
            GenerateMesh(sandScale1, sandAmplitude1, sandScale2, sandAmplitude2, sandScale3, sandAmplitude3));
        sandGO.transform.parent = this.transform;

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

        // Initialize the dune model
        sandElev = MeshToHeightMap(sandGO.GetComponent<MeshFilter>().mesh, resolution);
        terrainElev = MeshToHeightMap(terrainGO.GetComponent<MeshFilter>().mesh, resolution);
        slopeFinder = new FindSlopeMooreDeterministic();
        duneModel = new ModelDM(slopeFinder, sandElev, terrainElev, resolution + 1, resolution + 1, slope, (int)windDirection.x, (int)windDirection.y,
            heightVariation, heightVariation, hopLength, shadowSlope, avalancheSlope);
    }

    int frameCount = 0;
    void Update()
    {
        if (frameCount < 60)
        {
            // Cálculo de la avalancha
            duneModel.AvalancheInit();
        }
        else
        {

            // Update del modelo de dunas
            duneModel.Tick(grainsPerStep, (int)windDirection.x, (int)windDirection.y, heightVariation, heightVariation);


            ApplyHeightMapToMesh(sandGO.GetComponent<MeshFilter>().mesh, sandElev);
        }
        frameCount++;
    }


    // Funciones 

    Mesh GenerateMesh(float scale1, float amplitude1, float scale2, float amplitude2, float scale3, float amplitude3, bool onlySand = false)
    {
        // Generate the terrain mesh
        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[(resolution + 1) * (resolution + 1)];
        Vector2[] uv = new Vector2[vertices.Length];
        int[] triangles = new int[resolution * resolution * 6];
        //float[,] Elev = new float[resolution + 1, resolution + 1];

        for (int i = 0, z = 0; z <= resolution; z++)
        {
            for (int x = 0; x <= resolution; x++, i++)
            {
                float xPos = (float)x / resolution * size;
                float yPos = 2 * GetMultiScalePerlinHeight(x, z, scale1, amplitude1, scale2, amplitude2, scale3, amplitude3);/// resolution * size;
                if (onlySand && z > 50 && z < 70 && x > 100 && x < 120)
                {
                    yPos = 32;
                }


                /*if (!onlySand && z > 150 && z < 270 && x > 100 && x < 220)
                {
                    yPos = 32;
                } */
                float zPos = (float)z / resolution * size;
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
                triangles[ti + 1] = vi + resolution + 1;
                triangles[ti + 2] = vi + 1;

                triangles[ti + 3] = vi + 1;
                triangles[ti + 4] = vi + resolution + 1;
                triangles[ti + 5] = vi + resolution + 2;
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

    float[,] MeshToHeightMap(Mesh mesh, int resolution)
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

    void ApplyHeightMapToMesh(Mesh mesh, float[,] heightMap)
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
    
    void RegularizeMesh(Mesh sandMesh, Mesh terrainMesh)
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
