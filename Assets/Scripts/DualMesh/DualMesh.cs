using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public partial class DualMesh :MonoBehaviour
{
    #region Awake
    public static DualMesh Instance;
    void Awake()
    {
        Instance = this;
        ResourceSystem.ResourceManager.Awake();
    }
    #endregion

    #region Start

    void Start()
    {
        Initializer();
    }
    #endregion

    #region Update
    void Update()
    {
        //float before = duneModel.TotalSand();
        
        if (!isPaused)
        {
            HandleInput();

            switch (inMode)
            {
                #region Build Mode
                case PlayingMode.Build:
                    {
                        TimeManager.Instance.Pause();
                        ConstructionMode();
                        break;
                    }
                #endregion

                #region Destroy Mode
                case PlayingMode.Recycle:
                    {
                        TimeManager.Instance.Pause();
                        RecycleMode();
                        break;
                    }
                #endregion

                #region Action Mode
                case PlayingMode.Action:
                    {
                        TimeManager.Instance.Pause();
                        ActionsMode();
                        break;
                    }
                #endregion

                #region Simulation Mode
                case PlayingMode.Simulation:
                    {
                        TimeManager.Instance.Play();
                        SimulationMode();
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
        
        dualMeshConstructor.ApplyChanges(sandGO.GetComponent<MeshFilter>().mesh, sand, sandChanges);
        terrainShadowChanges.ClearChanges();

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
        sandChanges.Dispose();
        terrainShadowChanges.Dispose();
    }
}
