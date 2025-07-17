using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public partial class DualMesh : MonoBehaviour
{
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

                if (inMode == PlayingMode.Simulation)
                {
                    builder.RestoreHoverMaterials();
                }
            }
            #endregion

            switch (inMode)
            {
                #region Build Mode
                case PlayingMode.Build:
                    {
                        ConstructionMode();
                        break;
                    }
                #endregion

                #region Destroy Mode
                case PlayingMode.Destroy:
                    {
                        DestructionMode();
                        break;
                    }
                #endregion

                #region Simulation Mode
                case PlayingMode.Simulation:
                    {
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

        //dualMeshConstructor.ApplyHeightMapToMesh(sandGO.GetComponent<MeshFilter>().mesh, sand);
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
