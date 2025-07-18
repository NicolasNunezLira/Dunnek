using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Data;
using Unity.VisualScripting;
using UnityEngine;

namespace Building
{
    public partial class BuildSystem
    {
        #region Confirm

        public bool ConfirmBuild()
        {
            if (!canBuild) return false;

            activePreview.SetActive(false);


            switch (currentBuildMode)
            {
                case DualMesh.BuildMode.PlaceHouse:
                    GameObjectConstruction(housePrefab, previewX, previewZ, prefabRotation, ConstructionType.House);
                    return true;
                case DualMesh.BuildMode.Dig:
                    DigAction(previewX, previewZ, buildRadius, digDepth);
                    return true;
                case DualMesh.BuildMode.Flat:
                    FlatSand(previewX, previewZ, 3 * buildRadius);
                    return true;
                case DualMesh.BuildMode.AddSand:
                    AddSandCone(previewX, previewZ, 0.5f * buildRadius, 6f * buildRadius);
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
    }
}
