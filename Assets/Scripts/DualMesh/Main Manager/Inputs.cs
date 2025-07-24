using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public partial class DualMesh : MonoBehaviour
{
    #region Handle Input
    public void HandleInput()
    {
        // Enter/Exit Build Mode
        if (Input.GetKeyDown(KeyCode.C) && inMode != PlayingMode.Destroy)
        {
            PlayingMode newMode = (inMode == PlayingMode.Build) ? PlayingMode.Simulation : PlayingMode.Build;
            SetMode(newMode);
        }

        if (Input.GetKeyDown(KeyCode.X) && inMode != PlayingMode.Build)
        {
            PlayingMode newMode = (inMode == PlayingMode.Destroy) ? PlayingMode.Simulation : PlayingMode.Destroy;
            SetMode(newMode);
        }

        if (Input.GetKeyDown(KeyCode.V))
        {
            PlayingMode newMode = (inMode == PlayingMode.Action) ? PlayingMode.Simulation : PlayingMode.Action;
            SetMode(newMode);
        }

        if (Input.GetKeyDown(KeyCode.Escape) && inMode != PlayingMode.Simulation)
            {
                SetMode(PlayingMode.Simulation);
            }
        #endregion
    }

    #region Methods for inputs
    public void SetMode(PlayingMode newMode)
    {
        builder.HideAllPreviews();
        if (inMode == newMode)
        {
            inMode = PlayingMode.Simulation;
        }
        else
        {
            inMode = newMode;
        }

        if (uiController != null)
        {
            uiController.UpdateButtonVisuals(inMode);
        }

        UpdateMeshColliders();
    }

    void UpdateMeshColliders()
    {
        sandGO.GetComponent<MeshCollider>().sharedMesh = sandGO.GetComponent<MeshFilter>().mesh;
        terrainGO.GetComponent<MeshCollider>().sharedMesh = terrainGO.GetComponent<MeshFilter>().mesh;
    }
    #endregion
}
