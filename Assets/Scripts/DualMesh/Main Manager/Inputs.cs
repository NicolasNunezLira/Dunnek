using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public partial class DualMesh : MonoBehaviour
{
    #region Handle Input
    public void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.C) && inMode != PlayingMode.Recycle)
        {
            PlayingMode newMode = (inMode == PlayingMode.Build) ? PlayingMode.Simulation : PlayingMode.Build;
            SetMode(newMode);
        }

        if (Input.GetKeyDown(KeyCode.X) && inMode != PlayingMode.Build)
        {
            PlayingMode newMode = (inMode == PlayingMode.Recycle) ? PlayingMode.Simulation : PlayingMode.Recycle;
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

        if (Input.GetKeyDown(KeyCode.B) && inMode != PlayingMode.Draft)
        {
            PlayingMode newMode = (inMode == PlayingMode.Draft) ? PlayingMode.Simulation : PlayingMode.Draft;
            SetMode(newMode);
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

        if (inMode == PlayingMode.Simulation)
        {
            builder.HideAllPreviews();
            builder.ClearWallPreview();
            builder.ClearPoints();
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
