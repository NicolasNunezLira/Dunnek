using System.Collections.Generic;
using UnityEngine;

namespace Building
{
    public partial class BuildSystem
    {
        private Color green = new Color(0f, 1f, 0f, 0.3f), red = new Color(1f, 0f, 0f, 0.3f);
        public Dictionary<Renderer, Material[]> originalTowerMaterials = new();
        private Vector3? tempWallEndPoint;
        public bool canPlaceWall = true, isWallPreviewActive, thereIsATower = false;
        
        #region Handle
        public void HandleBuildPreview()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit, 100f, LayerMask.GetMask("Terrain")))
            {
                point = hit.point;
                int x = Mathf.FloorToInt(point.x * duneModel.xResolution / duneModel.size);
                int z = Mathf.FloorToInt(point.z * duneModel.zResolution / duneModel.size);

                if (x < 0 || z < 0 || x + buildSize > duneModel.xResolution + 1 || z + buildSize > duneModel.zResolution + 1)
                    return;

                if (currentBuildMode == DualMesh.BuildMode.PlaceWallBetweenPoints)
                {
                    if (wallStartPoint.HasValue)
                    {
                        tempWallEndPoint = point;
                        PreviewWall();
                        towerPreviewGO?.SetActive(false);
                        return;
                    }
                    else
                    {
                        towerPreviewGO?.SetActive(DualMesh.instance.inMode == DualMesh.PlayingMode.Build);
                        tempWallEndPoint = null;
                    }
                }

                Renderer rend = activePreview.GetComponentInChildren<Renderer>();
                Bounds bounds = rend.bounds;

                float cellSize = duneModel.size / duneModel.xResolution;
                // Convertimos los límites del modelo 3D a coordenadas en la grilla del terreno
                int xMin = Mathf.Clamp(Mathf.FloorToInt(bounds.min.x / cellSize), 0, duneModel.xResolution - 1);
                int xMax = Mathf.Clamp(Mathf.CeilToInt(bounds.max.x / cellSize), 0, duneModel.xResolution - 1);
                int zMin = Mathf.Clamp(Mathf.FloorToInt(bounds.min.z / cellSize), 0, duneModel.zResolution - 1);
                int zMax = Mathf.Clamp(Mathf.CeilToInt(bounds.max.z / cellSize), 0, duneModel.zResolution - 1);

                canBuild = true;
                float maxY = float.MinValue;

                for (int xi = xMin; xi <= xMax; xi++)
                {
                    for (int zj = zMin; zj <= zMax; zj++)
                    {
                        //if (constructionGrid[xi, zj].Count > 0)
                        if (!(constructionGrid[x, z].Count == 0 || constructionGrid.IsOnlyTowerAt(x, z)))
                            canBuild = false;

                        float y = Mathf.Max(duneModel.sand[xi, zj], duneModel.terrain[xi, zj]);
                        if (y > maxY) maxY = y;
                    }
                }

                float avgX = (x + (buildSize - 1) / 2f) * duneModel.size / duneModel.xResolution;
                float avgZ = (z + (buildSize - 1) / 2f) * duneModel.size / duneModel.zResolution;

                activePreview.transform.position = new UnityEngine.Vector3(avgX, maxY + 0.5f, avgZ);

                Color color = canBuild ? green : red;
                foreach (var rend_ in activePreview.GetComponentsInChildren<Renderer>())
                {
                    if (rend_.material.HasProperty("_Color"))
                        rend_.material.color = color;
                }

                previewX = x;
                previewZ = z;
            }
        }
        #endregion

        #region Manage Previews
        public void UpdateBuildPreviewVisual()
        {
            HideAllPreviews();

            switch (currentBuildMode)
            {
                case DualMesh.BuildMode.PlaceHouse:
                    activePreview = housePreviewGO;
                    break;

                case DualMesh.BuildMode.PlaceWallBetweenPoints:
                    activePreview = towerPreviewGO;
                    break;
            }

            
            activePreview?.SetActive(true);
        }

        public void UpdateActionPreviewVisual()
        {
            HideAllPreviews();

            switch (currentActionMode)
            {
                case DualMesh.ActionMode.Dig:
                    activePreview = shovelPreviewGO;
                    break;

                case DualMesh.ActionMode.Flat:
                    activePreview = sweeperPreviewGO;
                    break;

                case DualMesh.ActionMode.AddSand:
                    activePreview = circlePreviewGO;
                    break;
            }
           
            activePreview.SetActive(true);
        }

        public void HideAllPreviews()
        {
            shovelPreviewGO?.SetActive(false);
            wallPreviewGO?.SetActive(false);
            towerPreviewGO?.SetActive(false);
            housePreviewGO?.SetActive(false);
            sweeperPreviewGO?.SetActive(false);
            circlePreviewGO?.SetActive(false);
        }

        public void HideAllActionsPreviews()
        {
            shovelPreviewGO?.SetActive(false);
            sweeperPreviewGO?.SetActive(false);
            circlePreviewGO?.SetActive(false);
        }

        public void HideAllBuildsPreviews()
        {
            wallPreviewGO?.SetActive(false);
            towerPreviewGO?.SetActive(false);
            ClearWallPreview();
            ClearPoints();
        }

        public void RotateWallPreview()
        {
            if (currentBuildMode == DualMesh.BuildMode.PlaceWallBetweenPoints) return;

            prefabRotation *= UnityEngine.Quaternion.Euler(0, 45f, 0);
            activePreview.transform.rotation = prefabRotation;
        }      
        #endregion
    }
}