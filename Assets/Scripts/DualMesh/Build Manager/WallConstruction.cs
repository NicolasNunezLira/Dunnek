using UnityEngine;
using System.Collections.Generic;
using Unity.Mathematics;
using Data;
using Vector3 = UnityEngine.Vector3;
using Quaternion = UnityEngine.Quaternion;

namespace Building
{
    public partial class BuildSystem
    {
        public void BuildWallBetween(Vector3 p1, Vector3 p2)
        {
            CompositeConstruction Wall = new CompositeConstruction(currentCompositeConstructionID, CompositeConstruction.CompositeType.Wall);
            int x, z, idTower2;
            // Coloca las torres en los extremos
            (p1, _, _, _) = TryBuildATower(p1, Wall);
            (p2, x, z, idTower2) = TryBuildATower(p2, Wall);

            Vector3 dir = (p2 - p1).normalized;
            float distance = Vector3.Distance(p1, p2);

            GameObject parent = GameObject.Find("Construcciones") ?? new GameObject("Construcciones");

            // Calcular número de muros para cubrir completamente la distancia entre torres
            int segments = Mathf.Max(1, Mathf.FloorToInt(distance / wallPrefabLength));
            float adjustedLength = distance / segments;
            Vector3 step = dir * adjustedLength;

            List<int2> allSupport = new();

            for (int i = 2; i < segments; i++)
            {
                Vector3 pos = p1 + step * (i - 0.5f);  // Centrado en cada tramo
                (x, z) = GridIndex(pos);
                float y = Mathf.Max(duneModel.sand[x, z], duneModel.terrain[x, z]) - 0.1f;
                Vector3 adjusted = new Vector3(pos.x, y, pos.z);

                Quaternion rotation = Quaternion.LookRotation(new Vector3(dir.x, 0, dir.z)) * Quaternion.Euler(0, 90, 0);
                GameObject wall = GameObjectConstruction(wallPrefab, x, z, rotation, Data.ConstructionType.SegmentWall, adjusted);

                if (wall != null)
                {
                    Vector3 localScale = wall.transform.localScale;
                    // Ajusta el largo del muro (asumiendo eje X es largo del prefab)
                    localScale.x = adjustedLength / wallPrefabLength;
                    wall.transform.localScale = localScale;
                }

                AddPartToWall(Wall);

                allSupport.Add(new int2(x, z));
            }
            currentCompositeConstructionID++;
            activePreview = towerPreviewGO;
            activePreview.SetActive(false);

            //RestoreAllPreviews();
        }

        private (int x, int z) GridIndex(Vector3 pos)
        {
            float cellSize = duneModel.size / duneModel.xResolution;
            int x = Mathf.Clamp(Mathf.FloorToInt(pos.x / cellSize), 0, duneModel.xResolution - 1);
            int z = Mathf.Clamp(Mathf.FloorToInt(pos.z / cellSize), 0, duneModel.zResolution - 1);
            return (x, z);
        }

        private void AddPartToWall(CompositeConstruction wall)
        {
            wall.AddPart(constructions[currentConstructionID - 1]);
            constructions[currentConstructionID - 1].groupID = currentCompositeConstructionID;
        }

        private (Vector3, int, int, int) TryBuildATower(Vector3 p, CompositeConstruction Wall)
        {
            int id;
            (int x, int z) = GridIndex(p);
            if (!constructionGrid.TryGetTypesAt(x, z, ConstructionType.Tower, out List<int> ids))
            {
                id = currentConstructionID;
                GameObjectConstruction(towerPrefab, x, z,
                    Quaternion.LookRotation(Vector3.zero),
                    Data.ConstructionType.Tower, new Vector3(p.x, Mathf.Max(duneModel.sand[x, z], duneModel.terrainShadow[x, z]), p.z));
                AddPartToWall(Wall);
            }
            else
            {
                id = ids[0];
                p = constructions[id].position;
            }
            return (p, x, z, id);
        }
    }
}
