using UnityEngine;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Collections;

namespace Building
{
    public partial class BuildSystem
    {
        /*
        public void BuildWallBetween(Vector3 p1, Vector3 p2)
        {
            float cellSize = duneModel.size / duneModel.xResolution;
            float length = Vector3.Distance(p1, p2);
            Vector3 dir = (p2 - p1).normalized;

            int segments = Mathf.FloorToInt(length / wallPrefabLength);
            GameObject parent = GameObject.Find("Construcciones") ?? new GameObject("Construcciones");

            for (int i = 0; i <= segments; i++)
            {
                // Posición exacta donde quieres colocar el prefab (flotante)
                Vector3 pos = p1 + dir * (i * wallPrefabLength);

                // Solo para obtener altura del terreno
                int x = Mathf.Clamp(Mathf.FloorToInt(pos.x / cellSize), 0, duneModel.xResolution - 1);
                int z = Mathf.Clamp(Mathf.FloorToInt(pos.z / cellSize), 0, duneModel.zResolution - 1);

                float y = Mathf.Max(duneModel.sand[x, z], duneModel.terrainShadow[x, z]);

                // Posición final flotante con altura obtenida de la celda (x,z)
                Vector3 adjusted = new Vector3(pos.x, y, pos.z);

                Quaternion rot = Quaternion.LookRotation(dir) * Quaternion.Euler(0, 90f, 0);
                GameObjectConstruction(wallPrefab, x, z, rot, "Wall", adjusted);
            }

            PlaceTower(p1, dir, parent.transform);
            PlaceTower(p2, -dir, parent.transform);
        }
        */

        public void BuildWallBetween(Vector3 p1, Vector3 p2)
        {
            float cellSize = duneModel.size / duneModel.xResolution;
            Vector3 dir = (p2 - p1).normalized;
            float distance = Vector3.Distance(p1, p2);

            // Coloca las torres primero, en las posiciones exactas
            GameObject parent = GameObject.Find("Construcciones") ?? new GameObject("Construcciones");
            (int x, int z) = GridIndex(p1);
            GameObjectConstruction(towerPrefab, x, z,
                Quaternion.LookRotation(dir), "Tower", new Vector3(p1.x, Mathf.Max(duneModel.sand[x, z], duneModel.terrainShadow[x, z]), p1.z));
            (x, z) = GridIndex(p2);
            GameObjectConstruction(towerPrefab, x, z,
                Quaternion.LookRotation(-dir), "Tower", new Vector3(p2.x, Mathf.Max(duneModel.sand[x, z], duneModel.terrainShadow[x, z]), p2.z));
            //PlaceTower(p1, dir, parent.transform);
            //PlaceTower(p2, -dir, parent.transform);

            // Ajustamos distancia entre torres restando la mitad del tamaño de ambas torres (opcional)
            float availableLength = distance;
            int segments = Mathf.Max(1, Mathf.FloorToInt(availableLength / wallPrefabLength));
            float step = availableLength / segments;

            List<int2> allSupport = new();

            for (int i = 0; i < segments; i++)
            {
                Vector3 pos = p1 + dir * ((i + 0.5f) * step); // centrado entre pasos

                // Obtener índice para altura
                (x, z) = GridIndex(pos);

                float y = Mathf.Max(duneModel.sand[x, z], duneModel.terrainShadow[x, z]);
                Vector3 adjusted = new Vector3(pos.x, y, pos.z);

                Quaternion rotation = Quaternion.LookRotation(dir) * Quaternion.Euler(0, 90f, 0);
                GameObjectConstruction(wallPrefab, x, z, rotation, "Wall", adjusted);

                allSupport.Add(new int2(x, z));
            }

            AddConstructionToList(
                obj: parent,
                position: (p1 + p2) / 2f,
                rotation: Quaternion.identity,
                currentType: DualMesh.BuildMode.PlaceWallBetweenPoints,
                support: allSupport,
                boundarySupport: GetSupportBorder(allSupport, duneModel.xResolution, duneModel.zResolution),
                floorHeight: 0f,
                buildHeight: 1f
            );
        }

        private (int x, int z) GridIndex(Vector3 pos)
        {
            float cellSize = duneModel.size / duneModel.xResolution;
            int x = Mathf.Clamp(Mathf.FloorToInt(pos.x / cellSize), 0, duneModel.xResolution - 1);
            int z = Mathf.Clamp(Mathf.FloorToInt(pos.z / cellSize), 0, duneModel.zResolution - 1);
            return (x, z);
        }



        void PlaceTower(Vector3 pos, Vector3 dir, Transform parent)
        {
            int x = Mathf.FloorToInt(pos.x * duneModel.xResolution / duneModel.size);
            int z = Mathf.FloorToInt(pos.z * duneModel.zResolution / duneModel.size);
            float y = Mathf.Max(duneModel.sand[x, z], duneModel.terrainShadow[x, z]);
            Vector3 finalPos = new Vector3(pos.x, y, pos.z);
            GameObject.Instantiate(towerPrefab, finalPos, Quaternion.LookRotation(dir), parent);
        }
    }
}
