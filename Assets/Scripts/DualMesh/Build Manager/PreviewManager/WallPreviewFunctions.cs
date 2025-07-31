using System.Collections.Generic;
using Data;
using UnityEngine;

namespace Building
{
    public partial class BuildSystem
    {
        private GameObject existingStartTower = null, existingEndTower = null;
        private Color wallPreviewColor;
        public Dictionary<GameObject, Dictionary<Renderer, Material[]>> previewChanges = new();

        #region Preview Wall
        public void PreviewWall()
        {
            //towerPreviewGO?.SetActive(false);
            PreviewManager.Instance.buildPreviews[Data.ConstructionType.Tower].SetActive(false);
            ClearWallPreview();

            if (!wallStartPoint.HasValue || !tempWallEndPoint.HasValue) return;

            Vector3 p1 = wallStartPoint.Value;
            Vector3 p2 = tempWallEndPoint.Value;

            Vector3 dir = (p2 - p1).normalized;
            float distance = Vector3.Distance(p1, p2);
            int segments = Mathf.Max(1, Mathf.FloorToInt(distance / wallPrefabLength));
            float adjustedLength = distance / segments;
            Vector3 step = dir * adjustedLength;

            canPlaceWall = HasEnoughResources(new Dictionary<ConstructionType, int>
            {
                {ConstructionType.Tower, 2},
                {ConstructionType.SegmentWall, segments - 2}
            });
            wallPreviewColor = canPlaceWall ? Color.green : Color.red;

            for (int i = 2; i <= segments; i++)
            {
                Vector3 pos = p1 + step * (i - 0.5f);
                (int x, int z) = GridIndex(pos);

                float y = Mathf.Max(duneModel.sand[x, z], duneModel.terrain[x, z]) - 0.1f;
                Vector3 adjusted = new Vector3(pos.x, y, pos.z);

                GameObject wallSegment = GameObject.Instantiate(
                    PreviewManager.Instance.buildPreviews[Data.ConstructionType.SegmentWall],
                    adjusted, Quaternion.LookRotation(dir) * Quaternion.Euler(0, 90, 0));
                wallSegment.name = $"WallPreview_{i}";
                wallSegment.transform.SetParent(wallPreviewParent.transform);

                bool canBuildSegment = constructionGrid[x, z].Count == 0 || constructionGrid.IsOnlyTowerAt(x, z);
                Color segmentColor = (canBuildSegment && canPlaceWall) ? green : red;

                canPlaceWall = canPlaceWall && canBuildSegment;

                ChangePreviewColor(wallSegment, segmentColor, false);

                wallSegment.SetActive(true);
            }

            // Coloca torres
            PlaceTowerPreview(p1, wallPreviewColor);
            PlaceTowerPreview(p2, wallPreviewColor);

            isWallPreviewActive = true;
        }
        #endregion
        
        #region Place Tower Previews
        private void PlaceTowerPreview(Vector3 position, Color color)
        {
            float cellSize = duneModel.size / duneModel.xResolution;
            int x = Mathf.FloorToInt(position.x / cellSize);
            int z = Mathf.FloorToInt(position.z / cellSize);

            if (x < 0 || x >= duneModel.xResolution || z < 0 || z >= duneModel.zResolution) return;

            float y = Mathf.Max(duneModel.sand[x, z], duneModel.terrain[x, z]);
            Vector3 finalPos = new Vector3(position.x, y - 0.1f, position.z);

            GameObject existingTower = GetExistingTowerAt(x, z);
            if (existingTower != null)
            {
                if (existingStartTower == null)
                    existingStartTower = existingTower;
                else
                    existingEndTower = existingTower;

                return;
            }

            PreviewManager.Instance.buildPreviews[ConstructionType.Tower]?.SetActive(false);
            GameObject previewTower = GameObject.Instantiate(
                PreviewManager.Instance.buildPreviews[Data.ConstructionType.Tower], finalPos, Quaternion.identity);
            previewTower.SetActive(true);
            previewTower.name = "TowerPreview" + (wallStartPoint.HasValue ? "Start" : "End");
            previewTower.transform.SetParent(wallPreviewParent.transform);
            ChangePreviewColor(previewTower, color, false);
        }
        #endregion

        #region Clear Wall Preview
        public void ClearWallPreview()
        {
            if (wallPreviewParent != null)
                GameObject.Destroy(wallPreviewParent);
            wallPreviewParent = new GameObject("WallPreviewParent");
        }
        #endregion

        #region Set points for wall
        public bool SetPointsForWall()
        {
            if (!HasEnoughResources(new Dictionary<ConstructionType, int> { { ConstructionType.Tower, 1 } })) return false;
            Ray ray1 = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray1, out RaycastHit hit1, 100f, LayerMask.GetMask("Terrain")))
            {
                Vector3 clickedPoint = hit1.point;

                GameObject towerHit = TryGetTowerUnderCursor();
                if (towerHit != null)
                {
                    PreviewManager.Instance.buildPreviews[ConstructionType.Tower]?.SetActive(false);
                    clickedPoint = towerHit.transform.position;
                }
                else
                {
                    PreviewManager.Instance.buildPreviews[ConstructionType.Tower]?.SetActive(true);
                }

                if (!wallStartPoint.HasValue)
                {
                    wallStartPoint = clickedPoint;
                    thereIsATower = towerHit != null;
                    return false;
                }
                else if (!wallEndPoint.HasValue)
                {
                    wallEndPoint = clickedPoint;
                    return true;
                }
            }
            return false;
        }
        #endregion

        #region Find towers
        private GameObject TryGetTowerUnderCursor()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, LayerMask.GetMask("Constructions")))
            {
                GameObject go = hit.collider.gameObject;
                if (go.name.Contains("Tower"))
                {
                    Transform current = go.transform;
                    while (current.parent != null && current.parent.name != "Construcciones")
                        current = current.parent;

                    return current.gameObject;
                }
            }
            return null;
        } 

        private GameObject GetExistingTowerAt(int x, int z)
        {
            var construcciones = GameObject.Find("Construcciones");
            if (construcciones == null) return null;

            foreach (Transform child in construcciones.transform)
            {
                if (child.name.Contains("Tower"))
                {
                    Vector3 pos = child.position;
                    int cx = Mathf.FloorToInt(pos.x * duneModel.xResolution / duneModel.size);
                    int cz = Mathf.FloorToInt(pos.z * duneModel.zResolution / duneModel.size);

                    if (cx == x && cz == z)
                        return child.gameObject;
                }
            }

            return null;
        }
        #endregion

        #region Change colors for previews
        private void ChangePreviewColor(GameObject obj, Color color, bool add=false)
        {
            Dictionary<Renderer, Material[]> original = new();

            foreach (var rend in obj.GetComponentsInChildren<Renderer>())
            {
                if (add)
                {
                    if (!original.ContainsKey(rend))
                    {
                        original[rend] = rend.materials;
                    }
                }

                Material newMat = new Material(rend.material); // copiar material original
                Color c = color;
                c.a = 0.1f;
                newMat.color = c;

                newMat.SetFloat("_Mode", 3); // Transparent
                newMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                newMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                newMat.SetInt("_ZWrite", 0);
                newMat.DisableKeyword("_ALPHATEST_ON");
                newMat.EnableKeyword("_ALPHABLEND_ON");
                newMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                newMat.renderQueue = 3000;

                rend.material = newMat;
            }

            if (add) previewChanges.TryAdd(obj, original);
        }

        private void RestorePreview(GameObject obj)
        {
            if (!previewChanges.TryGetValue(obj, out var original)) return;

            foreach (var rend in obj.GetComponentsInChildren<Renderer>())
            {
                if (original.TryGetValue(rend, out var originalMats))
                {
                    rend.materials = originalMats;
                }
            }

            previewChanges.Remove(obj);
        }

        public void RestoreAllPreviews()
        {
            if (previewChanges.Keys.Count == 0) return;

            foreach (var key in previewChanges.Keys)
            {
                RestorePreview(key);
            }
        }
        #endregion

        #region Clear variables for preview
        public void ClearPoints()
        {
            wallStartPoint = null;
            wallEndPoint = null;
            existingStartTower = null;
            existingEndTower = null;
        }
        #endregion
    }
}

