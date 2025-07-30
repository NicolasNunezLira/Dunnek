using UnityEngine;
using System.Collections.Generic;
using Unity.Mathematics;
using System.Linq;
using Data;

namespace Building
{
    public partial class BuildSystem
    {
        #region Constructions of Game Object
        public GameObject GameObjectConstruction(
            ConstructionType type, int posX, int posZ, Quaternion rotation,
            Vector3? overridePosition = null, bool verify = true)
        {
            Dictionary<ConstructionType, int> constructionDict = new Dictionary<ConstructionType, int> { { type, 1 } };
            if (verify)
            {
                if (!HasEnoughResources(constructionDict))
                {
                    return null;
                }
            }

            float cellSize = duneModel.size / duneModel.xResolution;

            float y = Mathf.Max(
                duneModel.sand[posX, posZ],
                duneModel.terrain[posX, posZ]
            );

            Vector3 centerPos = overridePosition ?? new Vector3(
                (posX + 0.5f) * cellSize,
                y,
                (posZ + 0.5f) * cellSize
            );

            GameObject parentGO = GameObject.Find("Construcciones");
            if (parentGO == null)
            {
                parentGO = new GameObject("Construcciones");
                SetLayerRecursively(parentGO, LayerMask.NameToLayer("Constructions"));
            }

            // Instanciar el prefab con el objeto padre
            GameObject prefab = constructionsConfigs.constructionConfig[type].loadedPrefab;
            GameObject prefabInstance = GameObject.Instantiate(prefab, centerPos, rotation, parentGO.transform);
            SetLayerRecursively(prefabInstance, LayerMask.NameToLayer("Constructions"));
            prefabInstance.name = type.ToString() + currentConstructionID;

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
                        duneModel.terrainShadow[x, z] = targetHeight;
                        duneModel.terrainShadowChanges.AddChanges(x, z);
                        duneModel.sand[x, z] = floorHeight;
                        duneModel.sandChanges.AddChanges(x, z);
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
                type,
                support,
                GetSupportBorder(support, duneModel.xResolution, duneModel.zResolution),
                floorHeight,
                targetHeight - floorHeight);

            UpdateResources(constructionDict);
            return prefabInstance;
        }
        #endregion

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
            ConstructionType currentType,
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
                //constructionGrid[cell.x, cell.y] = currentConstructionID;
                constructionGrid.AddConstruction(cell.x, cell.y, currentConstructionID, currentType);
            }

            foreach (var cell in boundarySupport)
            {
                //constructionGrid[cell.x, cell.y] = currentConstructionID;
                constructionGrid.AddConstruction(cell.x, cell.y, currentConstructionID, currentType);
            }

            currentConstructionID++;
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

        #region Verificate resources for constructions
        private bool HasEnoughResources(Dictionary<ConstructionType, int> amounts)
        {
            float necessarySand = 0, necessaryWorkers = 0;
            foreach (var (type, amount) in amounts)
            {
                var config = constructionsConfigs.constructionConfig[type];
                var cost = config.cost;

                necessarySand += cost.Sand * amount;
                necessaryWorkers = Mathf.Max(necessaryWorkers, cost.Workers);
            }

            return resourceManager.GetAmount(ResourceSystem.ResourceName.Workers) >= necessaryWorkers &&
                    resourceManager.GetAmount(ResourceSystem.ResourceName.Sand) >= necessarySand;
        }
        #endregion

        #region Verificate resources for actions
        public bool HasEnoughtResourcesForAction(DualMesh.ActionMode action)
        {
            var cost = ActionConfig.Instance.actionsConfig[action].cost;

            return resourceManager.GetAmount(ResourceSystem.ResourceName.WorkForce) >= cost.WorkForce &&
                    resourceManager.GetAmount(ResourceSystem.ResourceName.Sand) >= cost.Sand;
        }
        #endregion  

        #region Consume resources
        private void UpdateResources(Dictionary<ConstructionType, int> amounts)
        {
            float necessaryWorkers = 0;
            foreach (var (type, amount) in amounts)
            {
                var cost = constructionsConfigs.constructionConfig[type].cost;

                resourceManager.TryConsumeResource(ResourceSystem.ResourceName.Sand, cost.Sand * amount);

                necessaryWorkers = Mathf.Max(necessaryWorkers, cost.Workers);

                var production = constructionsConfigs.constructionConfig[type].production;

                resourceManager.AddResource(ResourceSystem.ResourceName.Workers, production.Workers[0]);
                resourceManager.AddResource(ResourceSystem.ResourceName.Sand, production.Sand[0]);
            }

            //resourceManager.TryConsumeResource("Workers", necessaryWorkers);
        }
        #endregion
    }    
}