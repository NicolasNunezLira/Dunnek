namespace Building
{
    public partial class BuildSystem
    {
        #region Confirm build
        public bool ConfirmBuild()
        {
            if (!canBuild) return false;

            activePreview.SetActive(false);

            bool wasBuilt = false;

            switch (currentBuildMode)
            {
                case DualMesh.BuildMode.PlaceBuild:
                    if (!string.IsNullOrEmpty(DualMesh.Instance.currentConstruction))
                    {
                        // Usar currentConstruction como id de construcción
                        GameObjectConstruction(
                            DualMesh.Instance.currentConstruction,
                            "building",        // parte principal por convención
                            previewX,
                            previewZ,
                            prefabRotation
                        );
                        wasBuilt = true;
                    }
                    break;

                case DualMesh.BuildMode.PlaceWallBetweenPoints:
                    if (wallStartPoint.HasValue && wallEndPoint.HasValue)
                    {
                        BuildWallBetween(wallStartPoint.Value, wallEndPoint.Value);
                        wallStartPoint = null;
                        wallEndPoint = null;
                        isWallPreviewActive = false;
                        wasBuilt = true;
                    }
                    break;
            }

            if (wasBuilt)
            {
                currentConstruction = null;
                // Actualizar la ui
                return true;
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
