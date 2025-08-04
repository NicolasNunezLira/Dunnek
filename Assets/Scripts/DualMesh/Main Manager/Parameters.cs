using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public partial class DualMesh : MonoBehaviour
{
    #region Program Parameters
    [Header("User Interface controllers")]
    [Tooltip("Construction UI controller")]
    public UIController uiController;

    [Header("Plane Settings")]
    [Range(31, 511)]
    [Tooltip("The number of subdivisions of visual mesh along each axis.")]
    public int xResolution = 127, zResolution = 127;
    
    [Header("Mesh Settings")]
    [Tooltip("The size of the plane in world units.")]
    public float size = 10f;

    [Header("Simulation behavior")]
    [Tooltip("Is the simulaiton toroidal?")]
    public bool openEnded = false;

    
    [Header("Testeo escena inicial")]
    [Tooltip("Comenzar con planicie?")]
    [SerializeField] public bool planicie;

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

    [Header("Simulation Settings")]
    [Tooltip("The height variation of the terrain.")]
    public float heightVariation = 0.1f;

    [Tooltip("The slope of the terrain.")]
    public float slope = 0.2f;

    [Tooltip("The hop length for the simulation.")]
    public int hopLength = 1;

    [Tooltip("Shadow slope for the simulation.")]
    public float shadowSlope = 0.803847577f; // 3 * tan(15 degrees) ~ 0.803847577f

    [Tooltip("The direction of the wind.")]
    public Vector2 windDirection = new Vector2(1, 0);
    [Tooltip("The number of grains per step.")]
    public int grainsPerStep = 5000;

    [Tooltip("Settions for avalanches.")]
    //public int avalancheChecksPerFrame = 500;
    public float avalancheSlope = .5f;
    public float criticalSlopeThreshold = 2f;
    public int maxCellsPerFrame = 50;
    public float avalancheTransferRate = 0.6f;
    public float conicShapeFactor = 0.8f;
    public float minAvalancheAmount = 0.01f;

    [Header("Constructions Settings")]
    [Tooltip("Time in seconds to pulled down a construction after being built")]
    [SerializeField] public float pulledDownTime = 5f;
    #endregion

}
