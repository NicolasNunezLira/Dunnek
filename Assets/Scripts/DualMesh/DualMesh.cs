using UnityEngine;
using DunefieldModel_DualMesh;
using System.Collections.Generic;
using System;
using Building;
using Data;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public partial class DualMesh : MonoBehaviour
{
    #region Program Parameters
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

    [Header("Testeo escena inicial")]
    [Tooltip("Comenzar con planicie?")]
    [SerializeField] public bool planicie;

    #endregion

    // ====================================================================

    #region Variables

    public int simXResolution => openEnded ? xResolution + 40 : xResolution;
    public int simZResolution => openEnded ? zResolution + 40 : zResolution;

    private GameObject terrainGO, sandGO;

    //public float[,] terrainElev, sandElev, terrainShadow;
    // Native arrays for simulation
    public NativeGrid sand, terrain, terrainShadow;

    public int[,] constructionGrid;

    private ModelDM duneModel;
    private FindSlopeMooreDeterministic slopeFinder;

    private DualMeshConstructor dualMeshConstructor;
    private Dictionary<(int, int), Vector2Int> criticalSlopes;

    private int grainsForAvalanche = 0;

    private bool constructed = false, destructed = false, isHandlingPullDown = false;
    private BuildSystem builder;
    private GameObject housePreviewGO, wallPreviewGO, activePreview, shovelPreviewGO, sweeperPreviewGO, circlePreviewGO;


    public enum PlayingMode { Build, Destroy, Simulation };
    private PlayingMode inMode = PlayingMode.Simulation;
    public enum BuildMode
    { Raise, Dig, PlaceHouse, Flat, AddSand };

    private BuildMode currentBuildMode = BuildMode.PlaceHouse;

    private Dictionary<int, ConstructionData> constructions;
    private int currentConstructionID = 1;

    private bool isPaused = false;


    #endregion

    #region Start

    void Start()
    {
        constructions = new Dictionary<int, ConstructionData>();

        
        constructionGrid = new int[simXResolution + 1, simZResolution + 1];

        for (int x = 0; x < constructionGrid.GetLength(0); x++)
        {
            for (int z = 0; z < constructionGrid.GetLength(1); z++)
            {
                constructionGrid[x, z] = 0;
            }
        }


        slopeFinder = new FindSlopeMooreDeterministic();

        // Initialize the terrain and sand meshes
        dualMeshConstructor = new DualMeshConstructor(
            xResolution, zResolution,
            simXResolution, simZResolution,
            size,
            terrainScale1, terrainScale2, terrainScale3,
            terrainAmplitude1, terrainAmplitude2, terrainAmplitude3,
            sandScale1, sandScale2, sandScale3,
            sandAmplitude1, sandAmplitude2, sandAmplitude3,
            terrainMaterial, sandMaterial,
            criticalSlopes, criticalSlopeThreshold,
            ref planicie, this.transform);

        dualMeshConstructor.Initialize(
            out terrainGO, out sandGO,
            out sand, out terrain, out terrainShadow);

        // Initialize the sand mesh to be above the terrain mesh
        duneModel = new ModelDM(
            slopeFinder,
            sand, terrain, terrainShadow,
            constructionGrid,
            size,
            xResolution + 1, zResolution + 1,
            slope,
            (int)windDirection.x, (int)windDirection.y,
            ref constructions,
            ref currentConstructionID,
            heightVariation, heightVariation,
            hopLength, shadowSlope, avalancheSlope,
            maxCellsPerFrame,
            conicShapeFactor,
            avalancheTransferRate,
            minAvalancheAmount
        );
        duneModel.SetOpenEnded(openEnded);
        duneModel.InitAvalancheQueue();
        grainsForAvalanche = duneModel.avalancheQueue.Count;

        // Prefabs and previews
        AddCollidersToPrefabs();
        CreatePreviews();

        activePreview = housePreviewGO;

        builder = new BuildSystem(
            duneModel,
            dualMeshConstructor,
            constructions,
            currentConstructionID,
            pulledDownTime,
            housePrefabGO,
            wallPrefabGO,
            shovelPreviewGO,
            housePreviewGO,
            wallPreviewGO,
            sweeperPreviewGO,
            circlePreviewGO,
            currentBuildMode,
            terrainShadow,
            activePreview,
            constructionGrid,
            planicie);
    }
    #endregion

    #region Update

    
    void Update()
    {
        //float before = duneModel.TotalSand();
        if (!isPaused)
        {
            #region Handle Input
            // Enter/Exit Build Mode
            if (Input.GetKeyDown(KeyCode.C) && inMode != PlayingMode.Destroy)
            {
                inMode = (inMode == PlayingMode.Build) ? PlayingMode.Simulation : PlayingMode.Build;
                // Update the meshcolliders
                sandGO.GetComponent<MeshCollider>().sharedMesh = sandGO.GetComponent<MeshFilter>().mesh; // demasiado caro para realizarlo todos los frames
                terrainGO.GetComponent<MeshCollider>().sharedMesh = terrainGO.GetComponent<MeshFilter>().mesh; // demasiado caro para realizarlo todos los frames
            }

            if (Input.GetKeyDown(KeyCode.X) && inMode != PlayingMode.Build)
            {
                inMode = (inMode == PlayingMode.Destroy) ? PlayingMode.Simulation : PlayingMode.Destroy;
                // Update the meshcolliders
                sandGO.GetComponent<MeshCollider>().sharedMesh = sandGO.GetComponent<MeshFilter>().mesh; // demasiado caro para realizarlo todos los frames
                terrainGO.GetComponent<MeshCollider>().sharedMesh = terrainGO.GetComponent<MeshFilter>().mesh; // demasiado caro para realizarlo todos los frames
            }
            #endregion

            switch (inMode)
            {
                #region Build Mode
                case PlayingMode.Build:
                    {
                        if (Input.GetKeyDown(KeyCode.Tab) && inMode == PlayingMode.Build)
                        {
                            currentBuildMode = (BuildMode)(((int)currentBuildMode + 1) % System.Enum.GetValues(typeof(BuildMode)).Length);
                            builder.currentBuildMode = currentBuildMode;
                            builder.UpdateBuildPreviewVisual();
                            builder.HideAllPreviews();
                        }

                        builder.HandleBuildPreview();

                        if (Input.GetKeyDown(KeyCode.R))
                        {
                            builder.RotateWallPreview();
                        }

                        if (Input.GetMouseButtonDown(0))
                        {
                            constructed = builder.ConfirmBuild();
                            inMode = !constructed ? inMode : PlayingMode.Simulation;
                        }
                        ;
                        break;
                    }
                #endregion

                #region Destroy Mode
                case PlayingMode.Destroy:
                    {
                        builder.DetectConstructionUnderCursor();
                        if (Input.GetMouseButtonDown(0))
                        {
                            destructed = builder.DestroyConstruction();
                            inMode = !destructed ? inMode : PlayingMode.Simulation;
                        }
                        break;
                    }
                #endregion

                #region Simulation Mode
                case PlayingMode.Simulation:
                    {
                        builder.HideAllPreviews();

                        if (windDirection.x != 0 || windDirection.y != 0)
                        {
                            duneModel.Tick(grainsPerStep, (int)windDirection.x, (int)windDirection.y, heightVariation, heightVariation);
                        }

                        for (int i = 0; i < 100; i++)
                        {
                            grainsForAvalanche = duneModel.RunAvalancheBurst(Math.Max(maxCellsPerFrame, grainsForAvalanche));
                        }

                        break;
                    }
                #endregion
            }

            if (constructed)
            {
                dualMeshConstructor.ApplyHeightMapToMesh(terrainGO.GetComponent<MeshFilter>().mesh, terrain);
                constructed = false;
            }
        }

        CheckForPullDowns();
        
        dualMeshConstructor.ApplyHeightMapToMesh(sandGO.GetComponent<MeshFilter>().mesh, sand);

        //float after = duneModel.TotalSand();
        //Debug.Log($"Δ arena = {after - before:F5}");

    }
    
    #endregion

    public void OnDestroy()
    {
        sand.Dispose();
        terrain.Dispose();
        terrainShadow.Dispose();
        duneModel.shadow.Dispose();
    }
}
