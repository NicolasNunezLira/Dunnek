using Data;

namespace Building
{
    public partial class BuildSystem
    {
        #region Confirm build

        public bool ConfirmBuild()
        {
            if (!canBuild) return false;

            activePreview.SetActive(false);

            switch (currentBuildMode)
            {
                case DualMesh.BuildMode.PlaceHouse:
                    GameObjectConstruction(ConstructionType.House, previewX, previewZ, prefabRotation);
                    return true;
                case DualMesh.BuildMode.PlaceCantera:
                    GameObjectConstruction(ConstructionType.Cantera, previewX, previewZ, prefabRotation);
                    return true;
                case DualMesh.BuildMode.PlaceWallBetweenPoints:
                    if (wallStartPoint.HasValue && wallEndPoint.HasValue)
                    {
                        BuildWallBetween(wallStartPoint.Value, wallEndPoint.Value);
                        wallStartPoint = null;
                        wallEndPoint = null;
                        isWallPreviewActive = false;
                        return true;
                    }
                    break;
            }
            return false;
        }
        #endregion

        public bool ConfirmAction()
        {
            if (!canBuild) return false;

            activePreview.SetActive(false);

            switch (currentActionMode)
            {
                case DualMesh.ActionMode.Dig:
                    //if (resourceManager.GetAmount(ResourceSystem.ResourceName.WorkForce) < 1) return false;
                    if (resourceManager.TryConsumeResource(
                        ResourceSystem.ResourceName.WorkForce,
                        ActionConfig.Instance.actionsConfig[DualMesh.ActionMode.Dig].cost.WorkForce))
                    {
                        DigAction(previewX, previewZ, buildRadius, digDepth);
                        return true;
                    }
                    return false;
                case DualMesh.ActionMode.Flat:
                    if (resourceManager.GetAmount(ResourceSystem.ResourceName.WorkForce) < 4) return false;
                    FlatSand(previewX, previewZ, 3 * buildRadius);
                    return true;
                case DualMesh.ActionMode.AddSand:
                    if (resourceManager.GetAmount(ResourceSystem.ResourceName.WorkForce) < 2) return false;
                    AddSandCone(previewX, previewZ, 0.5f * buildRadius, 6f * buildRadius);
                    return true;
            }
            return false;
        }
    }
}
