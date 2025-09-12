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
                case DualMesh.BuildMode.PlaceBuild:
                    if (!string.IsNullOrEmpty(DualMesh.Instance.currentBuild))
                    {
                        // Usar currentBuild como id de construcción
                        GameObjectConstruction(
                            DualMesh.Instance.currentBuild,
                            "building",        // parte principal por convención
                            previewX,
                            previewZ,
                            prefabRotation
                        );
                        return true;
                    }
                    break;

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

        #region Confirm action

        public bool ConfirmAction()
        {
            if (!HasEnoughtResourcesForAction(currentActionMode)) return false;

            activePreview.SetActive(false);

            ApplyActionCost(currentActionMode);

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

                /*
                case DualMesh.ActionMode.Recycle: // Recycle como acción
                RecycleAt(previewX, previewZ);
                return true;
                */
            }

            return false;
        }
        #endregion
    }
}
