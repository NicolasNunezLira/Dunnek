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
            if (!canBuild && resourceManager.TryConsumeResource(
                        ResourceSystem.ResourceName.Work,
                        ActionConfig.Instance.actionsConfig[currentActionMode].cost.Work
                ))
                return false;

            activePreview.SetActive(false);

            switch (currentActionMode)
            {
                case DualMesh.ActionMode.Dig:
                    DigAction(previewX, previewZ, buildRadius, digDepth);
                        return true;
                case DualMesh.ActionMode.Flat:
                    FlatSand(previewX, previewZ, 3 * buildRadius);
                    return true;
                case DualMesh.ActionMode.AddSand:
                    AddSandCone(previewX, previewZ, 0.5f * buildRadius, 6f * buildRadius);
                    return true;
            }
            return false;
        }
    }
}
