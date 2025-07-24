using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public partial class DualMesh : MonoBehaviour
{
    public void SetBuildType(BuildMode mode)
    {
        builder.HideAllActionsPreviews();
        if (mode == BuildMode.PlaceHouse)
        {
            builder.ClearWallPreview();
            builder.ClearPoints();
        }
        builder.currentBuildMode = mode;
        currentBuildMode = mode;
        
        builder.UpdateBuildPreviewVisual();
        uiController.UpdateBuildsButtonVisual(mode);
    }

    public void SetActionType(ActionMode mode)
    {
        builder.HideAllBuildsPreviews();
        builder.currentActionMode = mode;
        currentActionMode = mode;
        builder.UpdateActionPreviewVisual();
        uiController.UpdateActionsButtonVisual(mode);
    }
}