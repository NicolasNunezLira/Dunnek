using UnityEngine;

namespace Building
{
    public partial class BuildSystem
    {
        #region Handle
        public void HandleBuildPreview()
        {
            activePreview.SetActive(true);
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, LayerMask.GetMask("Terrain")))
            {
                point = hit.point;
                int x = Mathf.FloorToInt(point.x * duneModel.xResolution / duneModel.size);
                int z = Mathf.FloorToInt(point.z * duneModel.zResolution / duneModel.size);

                if (x < 0 || z < 0 || x + buildSize > duneModel.xResolution + 1 || z + buildSize > duneModel.zResolution + 1)
                    return;

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
                        if (constructionGrid[xi, zj] > 0) // ← asegúrate que `true` signifique libre
                            canBuild = false;

                        float y = Mathf.Max(duneModel.sand[xi, zj], duneModel.terrainShadow[xi, zj]);
                        if (y > maxY) maxY = y;
                    }
                }

                float avgX = (x + (buildSize - 1) / 2f) * duneModel.size / duneModel.xResolution;
                float avgZ = (z + (buildSize - 1) / 2f) * duneModel.size / duneModel.zResolution;

                activePreview.transform.position = new UnityEngine.Vector3(avgX, maxY + 0.5f, avgZ);


                Color color = canBuild ? new Color(0f, 1f, 0f, 0.3f) : new Color(1f, 0f, 0f, 0.3f);
                foreach (var rend_ in activePreview.GetComponentsInChildren<Renderer>())
                {
                    if (rend_.material.HasProperty("_Color"))
                        rend_.material.color = color;
                }

                previewX = x;
                previewZ = z;
            }
        }

        public bool SetPointsForWall()
        {
            Ray ray1 = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray1, out RaycastHit hit1, 100f, LayerMask.GetMask("Terrain")))
            {
                if (!wallStartPoint.HasValue)
                {
                    wallStartPoint = hit1.point;
                    Debug.Log("Start point set.");
                    return false;
                }
                else if (!wallEndPoint.HasValue)
                {
                    wallEndPoint = hit1.point;
                    Debug.Log("End point set.");
                    return true;
                }
            }
            return false;
        }
        #endregion

        #region Confirm

        public bool ConfirmBuild()
        {
            if (!canBuild) return false;

            activePreview.SetActive(false);


            switch (currentBuildMode)
            {
                case DualMesh.BuildMode.PlaceHouse:
                    GameObjectConstruction(housePrefab, previewX, previewZ, prefabRotation, "House");
                    return true;
                case DualMesh.BuildMode.Raise:
                    GameObjectConstruction(wallPrefab, previewX, previewZ, prefabRotation, "Wall");
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

        #region Manage Previews


        public void UpdateBuildPreviewVisual()
        {
            shovelPreviewGO.SetActive(false);
            wallPreviewGO.SetActive(false);
            housePreviewGO.SetActive(false);

            switch (currentBuildMode)
            {
                case DualMesh.BuildMode.Raise:
                    activePreview = wallPreviewGO;
                    break;

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
                    //ClearWallPreview();
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
        }

        public void RotateWallPreview()
        {
            if (currentBuildMode == DualMesh.BuildMode.Dig) return;

            prefabRotation *= UnityEngine.Quaternion.Euler(0, 45f, 0);
            activePreview.transform.rotation = prefabRotation;
        }

        public void PreviewWall()
        {
            if (!wallStartPoint.HasValue || !wallEndPoint.HasValue) return;

            Vector3 p1 = wallStartPoint.Value;
            Vector3 p2 = wallEndPoint.Value;
            float cellSize = duneModel.size / duneModel.xResolution;

            Vector3 dir = (p2 - p1).normalized;
            float length = Vector3.Distance(p1, p2);
            int segments = Mathf.FloorToInt(length / wallPrefabLength); // Define wallPrefabLength en unidades del mundo

            for (int i = 0; i <= segments; i++)
            {
                Vector3 pos = p1 + dir * i * wallPrefabLength;
                int x = Mathf.FloorToInt(pos.x / cellSize);
                int z = Mathf.FloorToInt(pos.z / cellSize);

                if (x < 0 || x >= duneModel.xResolution || z < 0 || z >= duneModel.zResolution) continue;

                float y = Mathf.Max(duneModel.sand[x, z], duneModel.terrainShadow[x, z]);

                Vector3 adjusted = new Vector3(pos.x, y, pos.z);

                GameObject wallSegment = GameObject.Instantiate(wallPreviewGO, adjusted, Quaternion.LookRotation(dir));
                wallSegment.name = $"WallPreview_{i}";
                wallSegment.transform.SetParent(wallPreviewParent.transform);
            }

            // Coloca torres
            PlaceTowerPreview(p1, dir);
            PlaceTowerPreview(p2, -dir);

            isWallPreviewActive = true;
        }

        private void PlaceTowerPreview(Vector3 position, Vector3 forward)
        {
            float cellSize = duneModel.size / duneModel.xResolution;

            int x = Mathf.FloorToInt(position.x / cellSize);
            int z = Mathf.FloorToInt(position.z / cellSize);

            if (x < 0 || x >= duneModel.xResolution || z < 0 || z >= duneModel.zResolution) return;

            float y = Mathf.Max(duneModel.sand[x, z], duneModel.terrainShadow[x, z]);

            Vector3 finalPos = new Vector3(position.x, y + 0.5f, position.z); // 0.5f para que sobresalga un poco

            GameObject previewTower = GameObject.Instantiate(towerPreviewGO, finalPos, Quaternion.LookRotation(forward));
            previewTower.name = "TowerPreview";
            previewTower.transform.SetParent(wallPreviewParent.transform);
        }
        
        void ClearWallPreview()
        {
            if (wallPreviewParent != null)
                GameObject.Destroy(wallPreviewParent);
            wallPreviewParent = new GameObject("WallPreviewParent");
        }



        #endregion
    }
}