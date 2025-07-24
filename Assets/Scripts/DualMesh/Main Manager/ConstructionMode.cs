using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public partial class DualMesh : MonoBehaviour
{
    public void ConstructionMode()
    {
        if (!builder.wallStartPoint.HasValue) builder.UpdateBuildPreviewVisual();

        SetBuildType(currentBuildMode);

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                currentBuildMode = (BuildMode)(((int)currentBuildMode - 1 + System.Enum.GetValues(typeof(BuildMode)).Length) % System.Enum.GetValues(typeof(BuildMode)).Length);
            }
            else
            {
                currentBuildMode = (BuildMode)(((int)currentBuildMode + 1) % System.Enum.GetValues(typeof(BuildMode)).Length);
            }

            SetBuildType(currentBuildMode);
        }

        if (EventSystem.current.IsPointerOverGameObject())
        {
            builder.HideAllPreviews();
            return;
        }

        builder.HandleBuildPreview();

        if (currentBuildMode == BuildMode.PlaceWallBetweenPoints && builder.wallStartPoint.HasValue)
        {
            builder.PreviewWall();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            builder.RotateWallPreview();
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (currentBuildMode != BuildMode.PlaceWallBetweenPoints)
            {
                constructed = builder.ConfirmBuild();
                inMode = !constructed ? inMode : PlayingMode.Simulation;
                uiController.UpdateButtonVisuals(inMode);
            }
            else
            {
                if (Input.GetMouseButtonDown(0) && builder.canPlaceWall)
                {
                    isWallReadyForConstruction = builder.SetPointsForWall();
                    if (isWallReadyForConstruction)
                    {
                        builder.ClearWallPreview();
                        constructed = builder.ConfirmBuild();
                        inMode = !constructed ? inMode : PlayingMode.Simulation;
                        uiController.UpdateButtonVisuals(inMode);
                    }
                }
            }
        }
    }
}