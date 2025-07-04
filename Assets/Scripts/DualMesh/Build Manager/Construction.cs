using UnityEngine;
using System.Collections.Generic;
using Unity.Mathematics;
using System.Linq;
using Data;
//using System.Numerics;

namespace Building
{
    public partial class BuildSystem
    {

        public void GameObjectConstruction(GameObject prefab, UnityEngine.Quaternion rotation, string name)
        {
            float cellSize = duneModel.size / duneModel.xResolution;

            float y = Mathf.Max(
                duneModel.sandElev[previewX, previewZ],
                duneModel.terrainElev[previewX, previewZ]
            );

            UnityEngine.Vector3 centerPos = new UnityEngine.Vector3(
                (previewX + 0.5f) * cellSize,
                y,
                (previewZ + 0.5f) * cellSize
            );

            GameObject parentGO = GameObject.Find("Construcciones");
            if (parentGO == null)
            {
                parentGO = new GameObject("Construcciones");
                SetLayerRecursively(parentGO, LayerMask.NameToLayer("Constructions"));
            }

            // Instanciar el prefab con el objeto padre
            GameObject prefabInstance = GameObject.Instantiate(prefab, centerPos, rotation, parentGO.transform);
            SetLayerRecursively(prefabInstance, LayerMask.NameToLayer("Constructions"));
            prefabInstance.name = name + currentConstructionID;//+ System.DateTime.Now.ToString("HHmmss");

            activePreview.SetActive(false);
            prefabInstance.SetActive(true);

            Renderer rend = prefabInstance.GetComponentInChildren<Renderer>();
            Bounds bounds = rend.bounds;
            float targetHeight = bounds.max.y - 0.05f;
            float floorHeight = bounds.min.y;

            // Obtener los bounds en espacio local
            Bounds localBounds = rend.localBounds;
            Transform objTransform = rend.transform;

            int xMin = Mathf.Clamp(Mathf.FloorToInt(bounds.min.x / cellSize - 1), 0, duneModel.xResolution);
            int xMax = Mathf.Clamp(Mathf.CeilToInt(bounds.max.x / cellSize + 1), 0, duneModel.xResolution);
            int zMin = Mathf.Clamp(Mathf.FloorToInt(bounds.min.z / cellSize - 1), 0, duneModel.zResolution);
            int zMax = Mathf.Clamp(Mathf.CeilToInt(bounds.max.z / cellSize + 1), 0, duneModel.zResolution);

            List<int2> support = new List<int2>();
            for (int x = xMin; x <= xMax; x++)
            {
                for (int z = zMin; z <= zMax; z++)
                {
                    float worldX = x * cellSize;
                    float worldZ = z * cellSize;
                    Vector3 worldPoint = new Vector3(worldX, bounds.center.y, worldZ);

                    // Convertimos al espacio local del objeto
                    Vector3 localPoint = objTransform.InverseTransformPoint(worldPoint);

                    // Verificamos si está dentro del local bounds
                    if (localBounds.Contains(localPoint))
                    {
                        duneModel.terrainElev[x, z] = targetHeight;
                        duneModel.sandElev[x, z] = floorHeight;
                        //isConstruible[x, z] = false;
                        support.Add(new int2(x, z));
                        duneModel.ActivateCell(x, z);
                        duneModel.UpdateShadow(x, z, duneModel.dx, duneModel.dz);
                    }
                }
            }
            AddConstructionToList(
                prefabInstance,
                centerPos,
                prefabRotation,
                currentBuildMode,
                support,
                GetSupportBorder(support, duneModel.xResolution, duneModel.zResolution),
                floorHeight,
                targetHeight - floorHeight);
            return;
        }

        // Helper method to set layer recursively
        private void SetLayerRecursively(GameObject obj, int newLayer)
        {
            if (obj == null) return;
            obj.layer = newLayer;
            foreach (Transform child in obj.transform)
            {
                if (child == null) continue;
                SetLayerRecursively(child.gameObject, newLayer);
            }
        }

        #region Save constructions

        public void AddConstructionToList(
            GameObject obj,
            UnityEngine.Vector3 position,
            UnityEngine.Quaternion rotation,
            DualMesh.BuildMode currentType,
            List<int2> support,
            List<int2> boundarySupport,
            float floorHeight,
            float buildHeight
        )
        {
            var data = new ConstructionData
            {
                obj = obj,
                position = position,
                rotation = rotation,
                type = currentType,
                support = support,
                boundarySupport = boundarySupport,
                floorHeight = floorHeight,
                buildHeight = buildHeight,
                duration = durationBuild,
                timeBuilt = Time.time
            };

            constructions.Add(currentConstructionID, data);

            foreach (var cell in support)
            {
                constructionGrid[cell.x, cell.y] = currentConstructionID;
            }

            foreach (var cell in boundarySupport)
            {
                constructionGrid[cell.x, cell.y] = currentConstructionID;
            }

            currentConstructionID++;
        }

        public GameObject GetPrefabForType(DualMesh.BuildMode mode)
        {
            switch (mode)
            {
                case DualMesh.BuildMode.Raise: return wallPrefab;
                case DualMesh.BuildMode.Dig: return shovelPreviewGO;
                case DualMesh.BuildMode.PlaceHouse: return housePrefab;
                default: return null;
            }
        }
        #endregion

        #region Support functions

        List<int2> GetSupportBorder(List<int2> support, int xMax, int zMax)
        {
            HashSet<int2> supportSet = new HashSet<int2>();
            foreach (var s in support)
                supportSet.Add(new int2((int)s.x, (int)s.y));

            HashSet<int2> borderSet = new HashSet<int2>();

            // Vectores vecinos en 8 direcciones
            int2[] directions = new int2[]
            {
                new int2(-1,  0), new int2(1,  0),
                new int2(0, -1), new int2(0,  1),
                new int2(-1, -1), new int2(-1, 1),
                new int2(1, -1), new int2(1, 1)
            };

            foreach (var s in supportSet)
            {
                foreach (var dir in directions)
                {
                    int2 neighbor = s + dir;

                    // Asegúrate de que está en los límites
                    if (neighbor.x < 0 || neighbor.x >= xMax || neighbor.y < 0 || neighbor.y >= zMax)
                        continue;

                    // Si no está en el soporte, es parte del borde
                    if (!supportSet.Contains(neighbor))
                        borderSet.Add(neighbor);
                }
            }

            return borderSet.ToList();
        }
        #endregion

    }
}