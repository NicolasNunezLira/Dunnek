using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public partial class DualMesh : MonoBehaviour
{
    public void ConstructionMode()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            currentBuildMode = (BuildMode)(((int)currentBuildMode + 1) % System.Enum.GetValues(typeof(BuildMode)).Length);
            builder.currentBuildMode = currentBuildMode;
            builder.UpdateBuildPreviewVisual();
            builder.HideAllPreviews();
        }

        builder.HandleBuildPreview();

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
            }
            else
            {
                if (Input.GetMouseButtonDown(0))
                {
                    isWallReadyForConstruction = builder.SetPointsForWall();
                    if (isWallReadyForConstruction)
                    {
                        builder.ClearWallPreview();
                        constructed = builder.ConfirmBuild();
                        inMode = !constructed ? inMode : PlayingMode.Simulation;
                    }
                }
                else
                {
                    builder.PreviewWall();
                }
            }
        ;
        }
    }
}