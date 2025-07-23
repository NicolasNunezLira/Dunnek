using System.Collections.Generic;
using Data;
using UnityEngine;

namespace Building
{
    public partial class BuildSystem
    {
        private GameObject existingStartTower = null, existingEndTower = null;
        public Dictionary<GameObject, Dictionary<Renderer, Material[]>> previewChanges = new();

        #region Preview Wall
        public void PreviewWall()
        {
            ClearWallPreview();
            canPlaceWall = true;

            if (!wallStartPoint.HasValue || !tempWallEndPoint.HasValue) return;

            Vector3 p1 = wallStartPoint.Value;
            Vector3 p2 = tempWallEndPoint.Value;

            Vector3 dir = (p2 - p1).normalized;
            float distance = Vector3.Distance(p1, p2);
            int segments = Mathf.Max(1, Mathf.FloorToInt(distance / wallPrefabLength));
            float adjustedLength = distance / segments;
            Vector3 step = dir * adjustedLength;

            for (int i = 2; i <= segments; i++)
            {
                Vector3 pos = p1 + step * (i - 0.5f);
                (int x, int z) = GridIndex(pos);

                float y = Mathf.Max(duneModel.sand[x, z], duneModel.terrain[x, z]) - 0.1f;
                Vector3 adjusted = new Vector3(pos.x, y, pos.z);

                GameObject wallSegment = GameObject.Instantiate(wallPreviewGO, adjusted, Quaternion.LookRotation(dir) * Quaternion.Euler(0, 90, 0));
                wallSegment.name = $"WallPreview_{i}";
                wallSegment.transform.SetParent(wallPreviewParent.transform);

                bool canBuildSegment = constructionGrid[x, z].Count == 0 || constructionGrid.IsOnlyTowerAt(x, z);
                Color segmentColor = canBuildSegment ? green : red;

                if (!canBuildSegment) canPlaceWall = false;

                ChangePreviewColor(wallSegment, segmentColor, false);

                wallSegment.SetActive(true);
            }

            // Coloca torres
            PlaceTowerPreview(p1);
            PlaceTowerPreview(p2);

            isWallPreviewActive = true;
        }
        #endregion
        
        #region Place Tower Previews
        private void PlaceTowerPreview(Vector3 position)
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
                //ChangePreviewColor(existingTower, green, true);
                // Asigna según si ya existe la torre inicial
                if (existingStartTower == null)
                    existingStartTower = existingTower;
                else
                    existingEndTower = existingTower;

                return;
            }

            towerPreviewGO?.SetActive(true);
            GameObject previewTower = GameObject.Instantiate(towerPreviewGO, finalPos, Quaternion.identity);
            previewTower.name = "TowerPreview" + (wallStartPoint.HasValue ? "Start" : "End");
            previewTower.transform.SetParent(wallPreviewParent.transform);
            ChangePreviewColor(previewTower, green, false);
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
            Ray ray1 = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray1, out RaycastHit hit1, 100f, LayerMask.GetMask("Terrain")))
            {
                Vector3 clickedPoint = hit1.point;

                GameObject towerHit = TryGetTowerUnderCursor();
                if (towerHit != null)
                {
                    towerPreviewGO?.SetActive(false);
                    clickedPoint = towerHit.transform.position;
                }
                else
                {
                    towerPreviewGO?.SetActive(true);
                }

                if (!wallStartPoint.HasValue)
                {
                    wallStartPoint = clickedPoint;
                    thereIsATower = towerHit != null;
                    Debug.Log("Start point set.");
                    return false;
                }
                else if (!wallEndPoint.HasValue)
                {
                    wallEndPoint = clickedPoint;
                    Debug.Log("End point set.");
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

