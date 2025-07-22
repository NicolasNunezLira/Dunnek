using System.Collections.Generic;
using Data;
using UnityEngine;

namespace Building
{
    public partial class BuildSystem
    {
        private ConstructionData construction, actualConstruction;
        private Color green = new Color(0f, 1f, 0f, 0.3f), red = new Color(1f, 0f, 0f, 0.3f);
        public Dictionary<Renderer, Material[]> originalTowerMaterials = new();
        private Vector3? tempWallEndPoint;
        private bool canPlaceWall = true, isWallPreviewActive, thereIsATower = false;
        
        #region Handle
        public void HandleBuildPreview()
        {
            if (!thereIsATower) activePreview.SetActive(true);
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, LayerMask.GetMask("Terrain")))
            {
                point = hit.point;
                int x = Mathf.FloorToInt(point.x * duneModel.xResolution / duneModel.size);
                int z = Mathf.FloorToInt(point.z * duneModel.zResolution / duneModel.size);

                if (x < 0 || z < 0 || x + buildSize > duneModel.xResolution + 1 || z + buildSize > duneModel.zResolution + 1)
                    return;

                if (currentBuildMode == DualMesh.BuildMode.PlaceWallBetweenPoints && wallStartPoint.HasValue)
                    {
                        tempWallEndPoint = point;
                        PreviewWall();
                        towerPreviewGO?.SetActive(false);
                        return;
                    }
                    else
                    {
                        tempWallEndPoint = null;
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
            shovelPreviewGO.SetActive(false);
            wallPreviewGO.SetActive(false);
            housePreviewGO.SetActive(false);

            switch (currentBuildMode)
            {
                case DualMesh.BuildMode.Dig:
                    activePreview = shovelPreviewGO;
                    break;

                case DualMesh.BuildMode.PlaceHouse:
                    activePreview = housePreviewGO;
                    break;

                case DualMesh.BuildMode.Flat:
                    activePreview = sweeperPreviewGO;
                    break;

                case DualMesh.BuildMode.AddSand:
                    activePreview = circlePreviewGO;
                    break;

                case DualMesh.BuildMode.PlaceWallBetweenPoints:
                    activePreview = towerPreviewGO;
                    break;
            }

            if (activePreview != null)
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
            ClearWallPreview();
            ClearPoints();
        }

        public void RotateWallPreview()
        {
            if (currentBuildMode == DualMesh.BuildMode.Dig) return;

            prefabRotation *= UnityEngine.Quaternion.Euler(0, 45f, 0);
            activePreview.transform.rotation = prefabRotation;
        }      
        #endregion
    }
}