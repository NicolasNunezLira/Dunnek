using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public partial class DualMesh : MonoBehaviour
{
    public string currentBuild;

    #region --- Build ---
    public void SetBuildType(string codeName)
    {
        // Cambiar modo a Build
        if (inMode != PlayingMode.Build)
            inMode = PlayingMode.Build;

        // Ocultar previews de acciones
        builder.HideAllActionsPreviews();

        // Guardar id de construcción
        currentConstruction = codeName;

        // Buscar configuración
        if (!ConstructionSystem.ConstructionConfig.Instance.ConstructionConfigs.TryGetValue(codeName, out var config))
        {
            Debug.LogWarning($"Construcción no encontrada: {codeName}");
            return;
        }

        // Determinar BuildMode: murallas tienen lógica especial
        currentBuildMode = (config.category == ConstructionSystem.ConstructionCategory.Wall)
            ? BuildMode.PlaceWallBetweenPoints
            : BuildMode.PlaceBuild;

        builder.currentBuildMode = currentBuildMode;

        // Limpiar previews si es construcción genérica
        if (currentBuildMode == BuildMode.PlaceBuild)
        {
            builder.ClearWallPreview();
            builder.ClearPoints();
        }

        builder.UpdateBuildPreviewVisual();

        // Actualizar UI
        uiController.UpdateMainButtonVisuals(PlayingMode.Build);
        uiController.ShowCategory(config.category.ToString());
        uiController.UpdateSelectedVisual(codeName);
    }
    #endregion

    #region --- Action ---
    public void SetActionType(ActionMode mode)
    {
        if (inMode != PlayingMode.Action)
            inMode = PlayingMode.Action;

        builder.HideAllBuildsPreviews();

        currentActionMode = mode;
        builder.currentActionMode = mode;

        builder.UpdateActionPreviewVisual();

        // Actualizar UI
        uiController.UpdateMainButtonVisuals(PlayingMode.Action);
        uiController.ShowCategory("Actions");
        uiController.UpdateActionsButtonVisual(mode.ToString().ToLower());
    }
    #endregion
}