using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public partial class DualMesh : MonoBehaviour
{
    #region --- Build ---
    public void SetBuildType(string codeName)
    {
        //Debug.Log($"Set Build Type called with codeName = {codeName}.");

        currentConstruction = codeName;

        if (inMode != PlayingMode.Build)
            inMode = PlayingMode.Build;

        builder.HideAllActionsPreviews();

        currentConstruction = codeName;
        builder.currentConstruction = codeName;

        if (!ConstructionSystem.ConstructionConfig.Instance.ConstructionConfigs.TryGetValue(codeName, out var config))
        {
            Debug.LogWarning($"Construcción no encontrada: {codeName}");
            return;
        }

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
    }
    #endregion
}