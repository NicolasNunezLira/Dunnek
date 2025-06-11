using UnityEngine;
using DunefieldModel_DualMesh;
using System.Collections.Generic;

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
    public float criticalSlopeThreshold = 2f;

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

    private DualMeshConstructor dualMeshConstructor;
    private Dictionary<(int, int), Vector2Int> criticalSlopes;

    void Start()
    {
        slopeFinder = new FindSlopeMooreDeterministic();

        // Initialize the terrain and sand meshes
        dualMeshConstructor = new DualMeshConstructor(resolution, size, terrainScale1, terrainScale2, terrainScale3, terrainAmplitude1, terrainAmplitude2, terrainAmplitude3,
            sandScale1, sandScale2, sandScale3, sandAmplitude1, sandAmplitude2, sandAmplitude3, terrainMaterial, sandMaterial, criticalSlopes, criticalSlopeThreshold, this.transform);

        dualMeshConstructor.Initialize(out terrainGO, out sandGO, out terrainElev, out sandElev);
        sandGO.GetComponent<MeshFilter>().mesh.MarkDynamic();

        // Initialize the sand mesh to be above the terrain mesh
        duneModel = new ModelDM(slopeFinder, sandElev, terrainElev, resolution + 1, resolution + 1, slope, (int)windDirection.x, (int)windDirection.y,
            heightVariation, heightVariation, hopLength, shadowSlope, avalancheSlope);
    }

    int frameCount = 0;
    void Update()
    {
        if (frameCount < 360)
        {
            // Cálculo de la avalancha
            duneModel.AvalancheInit();
        }
        else
        {

            // Update del modelo de dunas
            duneModel.Tick(grainsPerStep, (int)windDirection.x, (int)windDirection.y, heightVariation, heightVariation);


            dualMeshConstructor.ApplyHeightMapToMesh(sandGO.GetComponent<MeshFilter>().mesh, sandElev);

            if (frameCount % 100 == 0) duneModel.AvalancheInit();
        }
        frameCount++;
    }
}
