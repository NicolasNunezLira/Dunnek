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

    public GameObject terrainGO, sandGO;

    public NativeGrid sand, terrain, terrainShadow;

    public ConstructionGrid constructionGrid;

    public ModelDM duneModel;
    private FindSlopeMooreDeterministic slopeFinder;

    private DualMeshConstructor dualMeshConstructor;
    private Dictionary<(int, int), Vector2Int> criticalSlopes;

    private int grainsForAvalanche = 0;

    private bool constructed = false, destructed = false, isHandlingPullDown = false;
    private BuildSystem builder;
    private GameObject activePreview;

    public Data.ConstructionType currentConstructionType;

    public enum PlayingMode { Simulation, Build, Recycle, Action, Draft };
    public PlayingMode inMode { get; set; } = PlayingMode.Simulation;

    [SerializeField]
    public enum BuildMode
    { PlaceHouse, PlaceWallBetweenPoints, PlaceCantera };

    [SerializeField]
    public enum ActionMode
    { Flat, AddSand, Dig };

    private BuildMode currentBuildMode = BuildMode.PlaceHouse;
    private ActionMode currentActionMode = ActionMode.Dig;

    private Dictionary<int, ConstructionData> constructions;
    private Dictionary<int, CompositeConstruction> compositeConstructions;
    private int currentConstructionID = 1, currentCompositeConstructionID = 1;

    private bool isPaused = false, isWallReadyForConstruction = false;

    private FrameVisualChanges sandChanges, terrainShadowChanges;
    #endregion
}
