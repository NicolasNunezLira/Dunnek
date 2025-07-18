using UnityEngine;
using System.Collections.Generic;
using Unity.Mathematics;
using System.Numerics;
using Vector3 = UnityEngine.Vector3;
using Quaternion = UnityEngine.Quaternion;

namespace Building
{
    public partial class BuildSystem
    {
        public void BuildWallBetween(Vector3 p1, Vector3 p2)
        {
            // Coloca las torres en los extremos
            (int x, int z) = GridIndex(p1);
            //int idTower1 = currentConstructionID;
            GameObjectConstruction(towerPrefab, x, z,
                Quaternion.LookRotation(Vector3.zero), "Tower", new Vector3(p1.x, Mathf.Max(duneModel.sand[x, z], duneModel.terrainShadow[x, z]), p1.z));
            (x, z) = GridIndex(p2);
            int idTower2 = currentConstructionID;
            GameObjectConstruction(towerPrefab, x, z,
                Quaternion.LookRotation(Vector3.zero), "Tower", new Vector3(p2.x, Mathf.Max(duneModel.sand[x, z], duneModel.terrainShadow[x, z]), p2.z));
            

            Vector3 dir = (p2 - p1).normalized;
            float distance = Vector3.Distance(p1, p2);

            GameObject parent = GameObject.Find("Construcciones") ?? new GameObject("Construcciones");

            // Calcular número de muros para cubrir completamente la distancia entre torres
            int segments = Mathf.Max(1, Mathf.FloorToInt(distance / wallPrefabLength));
            float adjustedLength = distance / segments;
            Vector3 step = dir * adjustedLength;

            List<int2> allSupport = new();

            int count = 0;
            for (int i = 0; i < segments; i++)
            {
                Vector3 pos = p1 + step * (i - 0.5f);  // Centrado en cada tramo
                (x, z) = GridIndex(pos);
                if (constructionGrid[x, z] == idTower2)
                {
                    if (count > 0) return;
                    count++;
                }
                float y = Mathf.Max(duneModel.sand[x, z], duneModel.terrain[x, z]) - 0.1f;
                Vector3 adjusted = new Vector3(pos.x, y, pos.z);

                Quaternion rotation = Quaternion.LookRotation(new Vector3(dir.x, 0, dir.z)) * Quaternion.Euler(0, 90, 0);
                GameObject wall = GameObjectConstruction(wallPrefab, x, z, rotation, "Wall", adjusted);

                if (wall != null)
                {
                    Vector3 localScale = wall.transform.localScale;
                    // Ajusta el largo del muro (asumiendo eje X es largo del prefab)
                    localScale.x = adjustedLength / wallPrefabLength;
                    wall.transform.localScale = localScale;
                }

                allSupport.Add(new int2(x, z));
            }
        }

        private (int x, int z) GridIndex(Vector3 pos)
        {
            float cellSize = duneModel.size / duneModel.xResolution;
            int x = Mathf.Clamp(Mathf.FloorToInt(pos.x / cellSize), 0, duneModel.xResolution - 1);
            int z = Mathf.Clamp(Mathf.FloorToInt(pos.z / cellSize), 0, duneModel.zResolution - 1);
            return (x, z);
        }
    }
}
