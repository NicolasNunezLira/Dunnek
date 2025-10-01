using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public partial class DualMesh : MonoBehaviour
{
    #region Handle Input
    public void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && inMode != PlayingMode.Simulation)
        {
            SetMode(PlayingMode.Simulation);
        }
        #endregion
    }

    #region Methods for inputs
    /// <summary>
    /// Fija el modo del juego ( simulación, constriccion, acciones, drafteo), y actualiza la ui en función de esto.
    /// </summary>
    /// <param name="newMode"></param>
    public void SetMode(PlayingMode newMode)
    {
        builder.HideAllPreviews();

        if (inMode == newMode)
            inMode = PlayingMode.Simulation;
        else
            inMode = newMode;

        if (inMode == PlayingMode.Simulation)
        {
            builder.HideAllPreviews();
            builder.ClearWallPreview();
            builder.ClearPoints();
        }

        UIController uiController = UIController.Instance;
        if (uiController != null)
        {
            uiController.UpdateMainButtonVisuals(inMode);

            switch (inMode)
            {
                case PlayingMode.Build:
                    uiController.buildOptionsPanel.SetActive(true);
                    uiController.ShowCategory(uiController.currentCategory ?? "Housing");
                    break;

                case PlayingMode.Action:
                    uiController.buildOptionsPanel.SetActive(true);
                    uiController.ShowCategory("Actions");
                    break;

                case PlayingMode.Simulation:
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
