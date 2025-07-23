using UnityEngine;
using DunefieldModel_DualMesh;
using System.Collections.Generic;
using Building;
using Data;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public partial class DualMesh : MonoBehaviour
{
    #region Variables

    public int simXResolution => openEnded ? xResolution + 40 : xResolution;
    public int simZResolution => openEnded ? zResolution + 40 : zResolution;

    private GameObject terrainGO, sandGO;

    //public float[,] terrainElev, sandElev, terrainShadow;
    // Native arrays for simulation
    public NativeGrid sand, terrain, terrainShadow;

    public ConstructionGrid constructionGrid;

    private ModelDM duneModel;
    private FindSlopeMooreDeterministic slopeFinder;

    private DualMeshConstructor dualMeshConstructor;
    private Dictionary<(int, int), Vector2Int> criticalSlopes;

    private int grainsForAvalanche = 0;

    private bool constructed = false, destructed = false, isHandlingPullDown = false;
    private BuildSystem builder;
    private GameObject housePreviewGO, wallPreviewGO, towerPreviewGO, activePreview, shovelPreviewGO, sweeperPreviewGO, circlePreviewGO;


    public enum PlayingMode { Build, Destroy, Simulation };
    private PlayingMode inMode = PlayingMode.Simulation;
    public enum BuildMode
    { PlaceHouse, PlaceWallBetweenPoints, Flat, AddSand, Dig};

    private BuildMode currentBuildMode = BuildMode.PlaceHouse;

    private Dictionary<int, ConstructionData> constructions;
    private Dictionary<int, CompositeConstruction> compositeConstructions;
    private int currentConstructionID = 1, currentCompositeConstructionID = 1;

    private bool isPaused = false, isWallReadyForConstruction = false;

    private FrameVisualChanges sandChanges, terrainShadowChanges;
    
    #endregion
}
