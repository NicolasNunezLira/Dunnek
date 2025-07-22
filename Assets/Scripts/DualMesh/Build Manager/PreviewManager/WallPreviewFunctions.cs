using System.Collections.Generic;
using Data;
using UnityEngine;

namespace Building
{
    public partial class BuildSystem
    {
        public void PreviewWall()
        {
            ClearWallPreview();
            canPlaceWall = true;

            if (!wallStartPoint.HasValue || !tempWallEndPoint.HasValue) return;

            Vector3 p1 = wallStartPoint.Value;
            Vector3 p2 = tempWallEndPoint.Value;
            //float cellSize = duneModel.size / duneModel.xResolution;

            Vector3 dir = (p2 - p1).normalized;
            float distance = Vector3.Distance(p1, p2);
            int segments = Mathf.Max(1, Mathf.FloorToInt(distance / wallPrefabLength));
            float adjustedLength = distance / segments;
            Vector3 step = dir * adjustedLength;

            for (int i = 0; i <= segments; i++)
            {
                Vector3 pos = p1 + step * (i - 0.5f);
                (int x, int z) = GridIndex(pos);

                float y = Mathf.Max(duneModel.sand[x, z], duneModel.terrain[x, z]) - 0.1f;
                Vector3 adjusted = new Vector3(pos.x, y, pos.z);

                GameObject wallSegment = GameObject.Instantiate(wallPreviewGO, adjusted, Quaternion.LookRotation(dir) * Quaternion.Euler(0, 90, 0));
                wallSegment.name = $"WallPreview_{i}";
                wallSegment.transform.SetParent(wallPreviewParent.transform);

                bool canBuildSegment = constructionGrid[x, z].Count == 0;
                Color segmentColor = canBuildSegment ? green : red;

                if (!canBuildSegment) canPlaceWall = false;

                ChangePreviewColor(wallSegment, segmentColor);

                wallSegment.SetActive(true);
            }

            // Coloca torres
            PlaceTowerPreview(p1, Vector3.zero);
            PlaceTowerPreview(p2, Vector3.zero);

            isWallPreviewActive = true;
        }

        private void ChangePreviewColor(GameObject preview, Color color)
        {
            foreach (var rend in preview.GetComponentsInChildren<Renderer>())
            {
                if (rend.material.HasProperty("_Color"))
                    rend.material.color = color;
            }
        }

        private void PlaceTowerPreview(Vector3 position, Vector3 forward)
        {
            float cellSize = duneModel.size / duneModel.xResolution;

            int x = Mathf.FloorToInt(position.x / cellSize);
            int z = Mathf.FloorToInt(position.z / cellSize);

            if (x < 0 || x >= duneModel.xResolution || z < 0 || z >= duneModel.zResolution) return;

            float y = Mathf.Max(
                duneModel.sand[x, z],
                duneModel.terrain[x, z]);

            Vector3 finalPos = new Vector3(position.x, y - 0.1f, position.z); // 0.5f para que sobresalga un poco

            GameObject previewTower = GameObject.Instantiate(towerPreviewGO, finalPos, Quaternion.LookRotation(forward));
            previewTower.name = "TowerPreview" + (wallStartPoint.HasValue ? "1" : "2");
            previewTower.transform.SetParent(wallPreviewParent.transform);

            ChangePreviewColor(previewTower, (constructionGrid[x, z].Count > 0) ? red : green);
        }

        public void ClearWallPreview()
        {
            if (wallPreviewParent != null)
                GameObject.Destroy(wallPreviewParent);
            wallPreviewParent = new GameObject("WallPreviewParent");
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
                else if (!wallEndPoint.HasValue && canPlaceWall)
                {
                    wallEndPoint = hit1.point;
                    Debug.Log("End point set.");
                    return true;
                }
            }
            return false;
        }

        public bool DetectTowerUnderCursor(Color color)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            int layerMask = LayerMask.GetMask("Constructions");

            var construccionesParent = GameObject.Find("Construcciones")?.transform;
            if (construccionesParent == null) { RestoreHoverMaterials(); toDestroy = null; return false; }


            if (Physics.Raycast(ray, out hit, 100f, layerMask))
            {
                GameObject hitGO = hit.collider.gameObject;
                if (hitGO.transform.IsChildOf(construccionesParent))
                {
                    if (!hitGO.name.Contains("Tower")) { RestoreHoverMaterials(); toDestroy = null; return false; }

                    Transform current = hitGO.transform;
                    while (current.parent != null && current.parent.name != "Construcciones")
                    {
                        current = current.parent;
                    }

                    GameObject target = current.gameObject;

                    if (toDestroy != target)
                    {
                        RestoreHoverMaterials();
                        ChangeColor(target, color);
                        toDestroy = target;
                        return true;
                    }
                }
                else
                {
                    RestoreHoverMaterials();
                    toDestroy = null;
                    return false;
                }
            }
            else
            {
                RestoreHoverMaterials();
                toDestroy = null;
                return false;
            }
            return false;
        }
    }
}

