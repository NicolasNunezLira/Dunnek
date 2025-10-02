using UnityEngine;
using System.Collections.Generic;
using Unity.Mathematics;
using System.Linq;
using ConstructionSystem;
using ResourceSystem;
using BonusSystem;
using Utils;

namespace Building
{
    public partial class BuildSystem
    {
        #region - Constructions of Game Object
        /// <summary>
        /// Crea el gameobject del preview correspondiente si se cumplen los requisitos de construcción.
        /// </summary>
        /// <param name="codeName"></param>
        /// <param name="part"></param>
        /// <param name="posX"></param>
        /// <param name="posZ"></param>
        /// <param name="rotation"></param>
        /// <param name="overridePosition"></param>
        /// <param name="verify"></param>
        /// <returns></returns>
        public GameObject GameObjectConstruction(
            string codeName,
            string part,
            int posX,
            int posZ,
            Quaternion rotation,
            Vector3? overridePosition = null,
            bool verify = true)
        {
            if (string.IsNullOrEmpty(codeName.Trim())) return null;

            Dictionary<string, int> constructionDict = new Dictionary<string, int> { { codeName, 1 } };
            if (verify)
            {
                if (!HasEnoughResourcesForBuild(constructionDict))
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
                RecursivelyFunctions.SetLayerRecursively(parentGO, LayerMask.NameToLayer("Constructions"));
            }

            var config = ConstructionConfig.Instance.ConstructionConfigs[codeName];
            /*if (config.loadedPrefabs.Count == 0)
            {
                Debug.LogError($"No prefabs loaded for construction type: {codeName}");
                return null;
            }
            else if (config.loadedPrefabs.Count > 1)
            {
                Debug.LogWarning($"Multiple prefabs found for construction type: {codeName}. Using the first one.");
            }*/

            if (!config.loadedPrefabs.ContainsKey(part))
            {
                Debug.LogError($"Part '{part}' not found in loaded prefabs for construction type: {codeName}");
                return null;
            }

            GameObject prefab = config.loadedPrefabs[part];
            GameObject prefabInstance = GameObject.Instantiate(prefab, centerPos, rotation, parentGO.transform);
            RecursivelyFunctions.SetLayerRecursively(prefabInstance, LayerMask.NameToLayer("Constructions"));
            prefabInstance.name = codeName + "_" + part + "-" + currentConstructionID;

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
                codeName,
                support,
                GetSupportBorder(support, duneModel.xResolution, duneModel.zResolution),
                floorHeight,
                targetHeight - floorHeight);

            UpdateResources(constructionDict);
            return prefabInstance;
        }
        #endregion
        
        #region - Save constructions
        /// <summary>
        /// Añade un objeto a la lista de construcciones, inicializando los componentes para el tooltip del objeto y marcando los nodos usados.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="position"></param>
        /// <param name="rotation"></param>
        /// <param name="codeName"></param>
        /// <param name="support"></param>
        /// <param name="boundarySupport"></param>
        /// <param name="floorHeight"></param>
        /// <param name="buildHeight"></param>
        public void AddConstructionToList(
            GameObject obj,
            Vector3 position,
            Quaternion rotation,
            string codeName,
            List<int2> support,
            List<int2> boundarySupport,
            float floorHeight,
            float buildHeight
        )
        {
            var instance = new ConstructionInstance(
                obj,
                position,
                rotation,
                ConstructionConfig.Instance.ConstructionConfigs[codeName],
                support,
                boundarySupport,
                floorHeight,
                buildHeight
            );


            constructions.Add(currentConstructionID, instance);

            foreach (var cell in support)
            {
                constructionGrid.AddConstruction(cell.x, cell.y, currentConstructionID, codeName);
            }

            foreach (var cell in boundarySupport)
            {
                constructionGrid.AddConstruction(cell.x, cell.y, currentConstructionID, codeName);
            }

            // Link para los tooltips
            ResourcesLink link;
            var configs = ConstructionConfig.Instance.ConstructionConfigs;
            switch (configs[codeName].category)
            {
                case ConstructionCategory.Housing:
                case ConstructionCategory.Consumer:
                    bool wasAdded = ResourceManager.TryAddBuilding(
                        currentConstructionID,
                        codeName);
                    if (wasAdded)
                    {
                        link = obj.GetComponent<ResourcesLink>();
                        if (link != null) link.Init(
                            instance,
                            codeName,
                            currentConstructionID);
                    }
                    break;
                case ConstructionCategory.BonusProvider:
                    link = obj.GetComponent<ResourcesLink>();
                    if (link != null) link.Init(instance, codeName);

                    var config = configs[codeName];
                    if (config.bonusList != null)
                    {
                        foreach (var bonusDef in config.bonusList)
                        {
                            foreach (var eff in bonusDef.effects)
                            {
                                if (!System.Enum.TryParse(eff.resource, out Resource resource))
                                {
                                    Debug.LogWarning($"Recurso desconocido en bonus: {eff.resource}");
                                    continue;
                                }

                                BonusTarget target = bonusDef.target == "Production" ? BonusTarget.Production : BonusTarget.Consumption;

                                if (bonusDef.bonusType == "Global")
                                {
                                    var globalBonus = new GlobalBonus(resource, 1f + eff.pct, target, bonusDef.affectedBuildType, instance);
                                    BonusSystem.BonusManager.AddBonus(globalBonus);
                                }
                                else if (bonusDef.bonusType == "Local")
                                {
                                    Vector2Int pos2D = new Vector2Int(
                                        Mathf.RoundToInt(position.x),
                                        Mathf.RoundToInt(position.z)
                                    );

                                    int radius = Mathf.RoundToInt(bonusDef.radius);

                                    var localBonus = new LocalBonus(resource, 1f + eff.pct, target, bonusDef.affectedBuildType, instance, pos2D, radius);
                                    BonusSystem.BonusManager.AddBonus(localBonus);

                                    // TODO: Revisar este cambio en caso de error
                                    //localBonus.RecalculateAffectedBuildings(ResourceManager.GetAllConsumers().Values);
                                    localBonus.RecalculateAffectedBuildings();
                                }
                            }
                        }
                    }
                    break;
            }

            currentConstructionID++;
        }
        #endregion

        #region - Support functions
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

        #region - Verificate resources for constructions
        public bool HasEnoughResourcesForBuild(Dictionary<string, int> amounts)
        {
            Dictionary<Resource, float> necessaryResources = new Dictionary<Resource, float>();
            foreach (var (codeName, amount) in amounts)
            {
                if (string.IsNullOrEmpty(codeName)) continue;

                var config = ConstructionConfig.Instance.ConstructionConfigs[codeName];

                foreach ((Resource resource, float cost) in config.cost)
                {
                    if (!necessaryResources.ContainsKey(resource))
                    {
                        necessaryResources[resource] = 0;
                    }
                    necessaryResources[resource] += cost * amount;
                }
            }

            bool hasEnough = true;
            foreach ((Resource resource, float cost) in necessaryResources)
            {
                if (ResourceManager.GetAmount(resource) < cost)
                {
                    hasEnough = false;
                    break;
                }
            }

            return hasEnough;
        }

        #endregion

        #region - Verificate resources for actions
        public bool HasEnoughtResourcesForAction(DualMesh.ActionMode action)
        {
            Dictionary<Resource, float> necessaryResources = new Dictionary<Resource, float>();
            var costs = ActionConfig.Instance.actionsConfig[action].cost;

            foreach ((Resource resource, float cost) in costs)
            {
                if (!necessaryResources.ContainsKey(resource))
                {
                    necessaryResources[resource] = 0;
                }
                necessaryResources[resource] += cost;
            }

            bool hasEnough = true;
            foreach ((Resource resource, float cost) in necessaryResources)
            {
                if (ResourceManager.GetAmount(resource) < cost)
                {
                    hasEnough = false;
                    break;
                }
            }

            return hasEnough;
        }
        #endregion  

        #region - Consume resources
        private void UpdateResources(Dictionary<string, int> constructionAmounts)
        {
            foreach (var (codeName, count) in constructionAmounts)
            {
                if (!ConstructionConfig.Instance.ConstructionConfigs.TryGetValue(codeName, out var config))
                {
                    Debug.LogWarning($"No se encontró config para {codeName}");
                    continue;
                }

                for (int i = 0; i < count; i++)
                {
                    // 1. Consumir los recursos del costo de la construcción
                    foreach ((Resource resource, float cost) in config.cost)
                    {
                        bool success = ResourceManager.TryConsumeResource(resource, cost);
                        if (!success)
                        {
                            Debug.LogWarning($"No hay suficiente {resource} para construir {codeName}");
                        }
                    }

                    // 2. Registrar el consumer si corresponde (ya lo haces en AddConstructionToList)
                    // ResourceManager.TryAddConsumer(...) se llama desde AddConstructionToList
                }

                // 3. Actualizar todos los recursos para reflejar tasas de producción/consumo actuales
                ResourceManager.UpdateResources();
            }
        }


        #endregion
    }    
}