using UnityEngine;
using DunefieldModel_DualMesh;
using System.Collections.Generic;
using System;
using System.Xml;
using Unity.VisualScripting;
using Building;
using UnityEditor.EditorTools;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class DualMesh : MonoBehaviour
{
    #region Program Parameters
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

    [Header("Actions prefab")]
    [Tooltip("Shovel Prefab")]
    [SerializeField] public GameObject shovelPrefabGO;

    [Tooltip("Flat Prefab")]
    [SerializeField] public GameObject sweeperPrefabGO;

    [Tooltip("Deposition Prefab")]
    [SerializeField] public GameObject circlePrefabGO;


    [Tooltip("House Prefab")]
    [SerializeField] public GameObject housePrefabGO;

    [Tooltip("Wall prefab")]
    [SerializeField] public GameObject wallPrefabGO;
    public enum BuildMode
    { Raise, Dig, PlaceHouse, Flat, AddSand };

    [Header("Testeo escena inicial")]
    [Tooltip("Comenzar con planicie?")]
    [SerializeField] public bool planicie;

    #endregion

    // ====================================================================

    #region Variables
    private GameObject terrainGO, sandGO;

    public float[,] terrainElev, sandElev, terrainShadow;
    public bool[,] isConstruible;

    private ModelDM duneModel;
    private FindSlopeMooreDeterministic slopeFinder;

    private DualMeshConstructor dualMeshConstructor;
    private Dictionary<(int, int), Vector2Int> criticalSlopes;

    private int grainsForAvalanche = 0;

    private bool inBuildMode = false, constructed = false;
    private BuildSystem builder;
    private GameObject housePreviewGO, wallPreviewGO, activePreview, shovelPreviewGO, sweeperPreviewGO, circlePreviewGO;

    private BuildMode currentBuildMode = BuildMode.PlaceHouse;


    #endregion

    #region Start

    void Start()
    {
        isConstruible = new bool[resolution + 1, resolution + 1];

        for (int x = 0; x < isConstruible.GetLength(0); x++)
        {
            for (int z = 0; z < isConstruible.GetLength(1); z++)
            {
                isConstruible[x, z] = true;
            }
        }


        slopeFinder = new FindSlopeMooreDeterministic();

        // Initialize the terrain and sand meshes
        dualMeshConstructor = new DualMeshConstructor(resolution, size, terrainScale1, terrainScale2, terrainScale3, terrainAmplitude1, terrainAmplitude2, terrainAmplitude3,
            sandScale1, sandScale2, sandScale3, sandAmplitude1, sandAmplitude2, sandAmplitude3, terrainMaterial, sandMaterial, criticalSlopes, criticalSlopeThreshold, ref planicie, this.transform);

        dualMeshConstructor.Initialize(out terrainGO, out sandGO, out terrainElev, out sandElev, out terrainShadow);

        // Initialize the sand mesh to be above the terrain mesh
        duneModel = new ModelDM(slopeFinder, sandElev, terrainShadow, isConstruible, size, resolution + 1, resolution + 1, slope, (int)windDirection.x, (int)windDirection.y,
            heightVariation, heightVariation, hopLength, shadowSlope, avalancheSlope, maxCellsPerFrame,
            conicShapeFactor, avalancheTransferRate, minAvalancheAmount, false);

        duneModel.InitAvalancheQueue();
        grainsForAvalanche = duneModel.avalancheQueue.Count;

        shovelPreviewGO = Instantiate(shovelPrefabGO);
        shovelPreviewGO.SetActive(false);
        MakePreviewTransparent(shovelPreviewGO);

        housePreviewGO = Instantiate(housePrefabGO);
        housePreviewGO.SetActive(false);
        MakePreviewTransparent(housePreviewGO);

        wallPreviewGO = Instantiate(wallPrefabGO);
        wallPreviewGO.SetActive(false);
        MakePreviewTransparent(wallPreviewGO);

        sweeperPreviewGO = Instantiate(sweeperPrefabGO);
        sweeperPreviewGO.SetActive(false);
        MakePreviewTransparent(sweeperPreviewGO);

        circlePreviewGO = Instantiate(circlePrefabGO);
        circlePreviewGO.SetActive(false);
        MakePreviewTransparent(circlePreviewGO);

        activePreview = housePreviewGO;

        builder = new BuildSystem(
            duneModel, dualMeshConstructor,
            housePrefabGO, wallPrefabGO,
            ref shovelPreviewGO, ref housePreviewGO, ref wallPreviewGO, ref sweeperPreviewGO, ref circlePreviewGO,
            currentBuildMode, terrainElev, ref activePreview, ref isConstruible,
            planicie);
    }
    #endregion

    #region Update

    void Update()

    {
        // Enter/Exit Build Mode
        if (Input.GetKeyDown(KeyCode.C))
        {
            inBuildMode = !inBuildMode;
            // Update the meshcolliders
            sandGO.GetComponent<MeshCollider>().sharedMesh = sandGO.GetComponent<MeshFilter>().mesh; // demasiado caro para realizarlo todos los frames
            terrainGO.GetComponent<MeshCollider>().sharedMesh = terrainGO.GetComponent<MeshFilter>().mesh; // demasiado caro para realizarlo todos los frames
        }

        if (Input.GetKeyDown(KeyCode.Tab) && inBuildMode)
        {
            currentBuildMode = (BuildMode)(((int)currentBuildMode + 1) % System.Enum.GetValues(typeof(BuildMode)).Length);
            builder.currentBuildMode = currentBuildMode;
            builder.UpdateBuildPreviewVisual();
            builder.HideAllPreviews();

        }

        if (inBuildMode)
        {
            builder.HandleBuildPreview();

            if (Input.GetKeyDown(KeyCode.R))
            {
                builder.RotateWallPreview();
            }

            if (Input.GetMouseButtonDown(0))
            {
                constructed = builder.ConfirmBuild();
                inBuildMode = !constructed ? inBuildMode : !inBuildMode;
            }
        }
        else
        {
            builder.HideAllPreviews();
            if (windDirection.x != 0 || windDirection.y != 0) { duneModel.Tick(grainsPerStep, (int)windDirection.x, (int)windDirection.y, heightVariation, heightVariation); }

            for (int i = 0; i < 100; i++)
            {
                grainsForAvalanche = duneModel.RunAvalancheBurst(Math.Max(maxCellsPerFrame, grainsForAvalanche));
            }
            ;
        }

        if (constructed)
        {
            dualMeshConstructor.ApplyHeightMapToMesh(terrainGO.GetComponent<MeshFilter>().mesh, terrainElev);
            constructed = false;
        }


        dualMeshConstructor.ApplyHeightMapToMesh(sandGO.GetComponent<MeshFilter>().mesh, sandElev);

    }
    #endregion

    void MakePreviewTransparent(GameObject obj)
    {
        foreach (var rend in obj.GetComponentsInChildren<Renderer>())
        {
            Material mat = rend.material; // Esto instancia una copia
            Color c = Color.green;
            c.a = 0.3f;
            mat.color = c;
            mat.SetFloat("_Mode", 3); // Transparent
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
        }

        foreach (var col in obj.GetComponentsInChildren<Collider>())
        {
            col.enabled = false;
        }
    }



}
