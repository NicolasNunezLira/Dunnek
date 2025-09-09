using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public partial class DualMesh : MonoBehaviour
{
    #region Handle Input
    public void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            PlayingMode newMode = (inMode == PlayingMode.Build) ? PlayingMode.Simulation : PlayingMode.Build;
            SetMode(newMode);
        }

        /*
        if (Input.GetKeyDown(KeyCode.X) && inMode != PlayingMode.Build)
        {
            PlayingMode newMode = (inMode == PlayingMode.Recycle) ? PlayingMode.Simulation : PlayingMode.Recycle;
            SetMode(newMode);
        }
        */

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

        // Toggle: si ya está en este modo, vuelve a Simulation
        if (inMode == newMode)
            inMode = PlayingMode.Simulation;
        else
            inMode = newMode;

        // Limpieza si volvemos a Simulation
        if (inMode == PlayingMode.Simulation)
        {
            builder.HideAllPreviews();
            builder.ClearWallPreview();
            builder.ClearPoints();
        }

        // Actualizar la UI principal
        if (uiController != null)
        {
            uiController.UpdateMainButtonVisuals(inMode);

            switch (inMode)
            {
                case PlayingMode.Build:
                    // mostrar panel de construcciones
                    uiController.buildOptionsPanel.SetActive(true);
                    // mostrar la pestaña que estaba activa antes o la inicial
                    uiController.ShowCategory(uiController.currentCategory ?? "Housing");
                    break;

                case PlayingMode.Action:
                    // mostrar pestaña de acciones
                    uiController.buildOptionsPanel.SetActive(true);
                    uiController.ShowCategory("Actions");
                    break;

                case PlayingMode.Simulation:
                    // ocultar panel de construcciones/acciones
                    uiController.buildOptionsPanel.SetActive(false);
                    break;
            }
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
