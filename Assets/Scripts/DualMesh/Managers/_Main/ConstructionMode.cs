using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public partial class DualMesh : MonoBehaviour
{
    public void ConstructionMode()
    {
        // Actualizar preview si no hay un inicio de muro
        if (builder.wallStartPoint.HasValue)
            builder.UpdateBuildPreviewVisual();

        // Aplicar construcción actual
        //SetBuildType(currentConstruction);

        // Cambiar modo con Tab (alternar entre PlaceBuild y PlaceWallBetweenPoints)
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                currentBuildMode = (BuildMode)(((int)currentBuildMode + 1) % System.Enum.GetValues(typeof(BuildMode)).Length);
            }
            else
            {
                currentBuildMode = (BuildMode)(((int)currentBuildMode - 1 + System.Enum.GetValues(typeof(BuildMode)).Length) % System.Enum.GetValues(typeof(BuildMode)).Length);
            }

            // Actualizar modo según BuildMode
            UpdateBuildModeFromCurrentBuild();
        }

        // Evitar interacción si el mouse está sobre UI
        if (EventSystem.current.IsPointerOverGameObject())
        {
            builder.HideAllPreviews();
            return;
        }

        // Preview de construcción
        builder.HandleBuildPreview();

        // Preview de muralla si corresponde
        if (currentBuildMode == BuildMode.PlaceWallBetweenPoints && builder.wallStartPoint.HasValue)
        {
            builder.PreviewWall();
        }

        // Rotar muro con R
        if (Input.GetKeyDown(KeyCode.R))
        {
            builder.RotateWallPreview();
        }

        // Confirmar construcción con click izquierdo
        if (Input.GetMouseButtonDown(0))
        {
            if (currentBuildMode != BuildMode.PlaceWallBetweenPoints)
            {
                constructed = builder.ConfirmBuild();
                inMode = !constructed ? inMode : PlayingMode.Simulation;
                UIController.Instance.UpdateMainButtonVisuals(inMode);
            }
            else
            {
                // Muro: confirmar solo si se puede colocar
                if (builder.canPlaceWall)
                {
                    isWallReadyForConstruction = builder.SetPointsForWall();
                    if (isWallReadyForConstruction)
                    {
                        builder.ClearWallPreview();
                        constructed = builder.ConfirmBuild();
                        inMode = !constructed ? inMode : PlayingMode.Simulation;
                        UIController.Instance.UpdateMainButtonVisuals(inMode);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Actualiza el BuildMode según la construcción actual (muro o genérica)
    /// </summary>
    private void UpdateBuildModeFromCurrentBuild()
    {
        if (string.IsNullOrEmpty(currentConstruction))
            return;

        if (!ConstructionSystem.ConstructionConfig.Instance.ConstructionConfigs.TryGetValue(currentConstruction, out var config))
            return;

        currentBuildMode = (config.category == ConstructionSystem.ConstructionCategory.Wall)
            ? BuildMode.PlaceWallBetweenPoints
            : BuildMode.PlaceBuild;

        builder.currentBuildMode = currentBuildMode;
        builder.UpdateBuildPreviewVisual();

        // UI: actualizar pestaña y botón seleccionado
        UIController.Instance.ShowCategory(config.category.ToString());
        UIController.Instance.UpdateSelectedVisual(currentConstruction);
    }
}
