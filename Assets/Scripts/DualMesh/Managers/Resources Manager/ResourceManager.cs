using System.Collections.Generic;
using UnityEngine;
using ConstructionSystem;
using System.Linq;
using BonusSystem;
using Unity.VisualScripting;

namespace ResourceSystem
{
    /*
    public static class ResourceManager
    {
        #region Variables
        static private Dictionary<Resource, ResourceClass> resources = new();
        static private Dictionary<int, ResourceBuilding> buildings = new();
        public static IReadOnlyDictionary<int, ResourceBuilding> AllBuildings => buildings;
        #endregion

        #region Awake
        public static void Awake()
        {
            RegisterResource(Resource.Work, 1000f);
            RegisterResource(Resource.Sand, 1000f);
        }
        #endregion

        #region Resources Methods
        public static void RegisterResource(Resource name, float initialAmount)
        {
            if (!resources.ContainsKey(name))
            {
                resources[name] = new ResourceClass(name, initialAmount);
            }
        }

        public static void AddResource(Resource name, float amount)
        {
            if (resources.TryGetValue(name, out var res))
            {
                res.Add(amount);
            }
            else
            {
                Debug.LogWarning($"Trying to add to unregistered resource: {name}");
            }
        }

        public static bool TryConsumeResource(Resource name, float amount)
        {
            if (resources.TryGetValue(name, out var res))
            {
                return res.TryConsume(amount);
            }
            Debug.LogWarning($"Trying to consume from unregistered resource: {name}");
            return false;
        }

        public static float GetAmount(Resource name)
        {
            return resources.TryGetValue(name, out var res) ? res.Amount : 0f;
        }

        public static Dictionary<Resource, ResourceClass> GetAllResources()
        {
            return resources;
        }

        public static bool HasEnough(Resource name, float amount)
        {
            return resources.ContainsKey(name) && resources[name].Amount >= amount;
        }

        /// <summary>
        /// Aplica los cambios de producción/consumo efectivos de cada consumidor (considerando bonuses).
        /// </summary>
        public static void UpdateResources()
        {
            foreach (var res in resources.Values)
            {
                res.UpdateFromConsumers(buildings);
            }
            foreach (var res in resources.Values)
            {
                res.UpdateAmount();
            }

        }
        #endregion

        #region Consumers Methods
        public static bool TryAddConsumer(int id, string codeName)
        {
            if (buildings.ContainsKey(id))
            {
                Debug.LogWarning($"Consumer with ID {id} already exists.");
                return false;
            }

            var rates = ConstructionConfig.Instance.ConstructionConfigs[codeName].rate;
            if (rates.Values.All(v => v == 0)) return false;

            buildings[id] = new ResourceBuilding(id, codeName, false);

            // Ahora BonusManager se encarga de verificar si este consumidor
            // está dentro de algún bonus local y de registrarlo en consecuencia.
            BonusManager.RegisterConsumerInLocalBonuses(buildings[id]);
            return true;
        }

        public static void RemoveConsumer(int id, bool recycle = false)
        {
            if (buildings.ContainsKey(id))
            {
                ResourceBuilding building = buildings[id];

                // Avisar al BonusManager que este consumidor deja de existir
                BonusManager.UnregisterConsumerFromLocalBonuses(building);

                if (recycle)
                {
                    var config = ConstructionConfig.Instance.ConstructionConfigs[building.codeName];
                    AddResource(Resource.Sand, -Mathf.Floor(config.cost[Resource.Sand] / 2));
                    AddResource(Resource.Work, config.recycleWorkCost);
                }

                buildings.Remove(id);
            }
        }

        public static void UpdateConsumers()
        {
            foreach (var kvp in buildings.ToList())
            {
                var consumer = kvp.Value;

                if (consumer.isForceToStop)
                {
                    consumer.isOperative = false;
                }
                else
                {
                    // Se activa si tiene recursos suficientes
                    consumer.isOperative = consumer.rates.All(r =>
                        r.Value >= 0 || GetAmount(r.Key) >= -r.Value);
                }

                buildings[kvp.Key] = consumer;
            }
        }

        public static void SetConsumerActive(int id, bool isForceToStop)
        {
            if (!buildings.ContainsKey(id)) return;

            ResourceBuilding consumer = buildings[id];
            consumer.isForceToStop = isForceToStop;
            buildings[id] = consumer;
        }

        public static Dictionary<int, ResourceBuilding> GetAllConsumers()
        {
            return buildings;
        }

        public struct ResourceBuilding
        {
            public int id;
            public string codeName;
            public ConstructionConfig.ResourceCost rates => ConstructionConfig.Instance.ConstructionConfigs[codeName].rate;
            public ConstructionInstance instance;
            public bool isOperative;
            public bool isForceToStop;
            public Vector3 Position => instance.Position;
            public List<LocalBonus> Bonuses { get; private set; }

            public ResourceBuilding(int id, string codeName, bool isOperative)
            {
                this.id = id;
                this.codeName = codeName;
                this.isOperative = isOperative;
                isForceToStop = false;
                DualMesh.Instance.builder.constructions.TryGetValue(id, out this.instance);
                Bonuses = new();
            }

            public void AddLocalBonus(LocalBonus bonus)
            {
                Bonuses.Add(bonus);
            }
        }
        #endregion
    }
    */
    #region - ResourceManager
    public static class ResourceManager
    {
        #region - Variables
        private static readonly Dictionary<Resource, ResourceClass> resources = new();
        private static readonly Dictionary<int, ResourceBuilding> buildings = new();

        public static IReadOnlyDictionary<int, ResourceBuilding> AllBuildings => buildings;
        #endregion

        #region - Awake / Register
        public static void Awake()
        {
            RegisterResource(Resource.Work, 1000f);
            RegisterResource(Resource.Sand, 1000f);
        }

        public static void RegisterResource(Resource name, float initialAmount)
        {
            if (!resources.ContainsKey(name))
                resources[name] = new ResourceClass(name, initialAmount);
        }
        #endregion

        #region - Resources Methods
        public static void AddResource(Resource name, float amount)
        {
            if (resources.TryGetValue(name, out var res)) res.Add(amount);
        }

        public static bool TryConsumeResource(Resource name, float amount)
        {
            return resources.TryGetValue(name, out var res) && res.TryConsume(amount);
        }

        public static float GetAmount(Resource name)
        {
            return resources.TryGetValue(name, out var res) ? res.Amount : 0f;
        }

        public static float GetRate(Resource name)
        {
            return resources.TryGetValue(name, out var res) ? res.Rate : 0f;
        }

        public static void UpdateResources()
        {
            foreach (var res in resources.Values)
                res.UpdateFromConsumers(buildings);
            foreach (var res in resources.Values)
                res.UpdateAmount();
        }

        public static Dictionary<Resource, ResourceClass> GetAllResources()
        {
            return resources;
        }
        #endregion

        #region - Buildings (Consumers)
        public static bool TryAddBuilding(int id, string codeName)
        {
            if (buildings.ContainsKey(id)) return false;

            var rates = ConstructionConfig.Instance.ConstructionConfigs[codeName].rate;
            if (rates.Values.All(v => v == 0)) return false;

            buildings[id] = new ResourceBuilding(id, codeName, false);

            BonusManager.RegisterConsumerInLocalBonuses(buildings[id]);
            return true;
        }

        public static void RemoveBuilding(int id, bool recycle = false)
        {
            if (!buildings.ContainsKey(id)) return;

            var building = buildings[id];

            BonusManager.UnregisterConsumerFromLocalBonuses(building);

            if (recycle)
            {
                var config = ConstructionConfig.Instance.ConstructionConfigs[building.codeName];
                AddResource(Resource.Sand, -Mathf.Floor(config.cost[Resource.Sand] / 2));
                AddResource(Resource.Work, config.recycleWorkCost);
            }

            buildings.Remove(id);
        }

        public static void SetBuildingActive(int id, bool isForceToStop)
        {
            if (!buildings.ContainsKey(id)) return;
            var building = buildings[id];
            building.isForceToStop = isForceToStop;
            buildings[id] = building;
        }

        public static void UpdateConsumers()
        {
            foreach (var kvp in buildings.ToList())
            {
                var consumer = kvp.Value;

                if (consumer.isForceToStop)
                {
                    consumer.isOperative = false;
                }
                else
                {
                    // Se activa si tiene recursos suficientes
                    consumer.isOperative = consumer.rates.All(r =>
                        r.Value >= 0 || GetAmount(r.Key) >= -r.Value);
                }

                buildings[kvp.Key] = consumer;
            }
        }

        public static void RemoveConsumer(int id, bool recycle = false)
        {
            if (buildings.ContainsKey(id))
            {
                ResourceBuilding building = buildings[id];

                // Avisar al BonusManager que este consumidor deja de existir
                BonusManager.UnregisterConsumerFromLocalBonuses(building);

                if (recycle)
                {
                    var config = ConstructionConfig.Instance.ConstructionConfigs[building.codeName];
                    AddResource(Resource.Sand, -Mathf.Floor(config.cost[Resource.Sand] / 2));
                    AddResource(Resource.Work, config.recycleWorkCost);
                }

                buildings.Remove(id);
            }
        }


        public static Dictionary<int, ResourceBuilding> GetAllConsumers()
        {
            return buildings;
        }

        public struct ResourceBuilding
        {
            public int id;
            public string codeName;
            public ConstructionInstance instance;
            public bool isOperative;
            public bool isForceToStop;
            public ConstructionConfig.ResourceCost rates => ConstructionConfig.Instance.ConstructionConfigs[codeName].rate;
            public Vector3 Position => instance.Position;
            public List<LocalBonus> Bonuses { get; private set; }

            public ResourceBuilding(int id, string codeName, bool isOperative)
            {
                this.id = id;
                this.codeName = codeName;
                this.isOperative = isOperative;
                isForceToStop = false;
                DualMesh.Instance.builder.constructions.TryGetValue(id, out this.instance);
                Bonuses = new();
            }

            public void AddLocalBonus(LocalBonus bonus)
            {
                Bonuses.Add(bonus);
            }
        }
        #endregion
    }
    #endregion
}


/*
using System.Collections.Generic;
using UnityEngine;
using ConstructionSystem;
using System.Linq;
using BonusSystem;

namespace ResourceSystem {
    public static class ResourceManager
    {
        #region Variables
        static private Dictionary<Resource, ResourceClass> resources = new Dictionary<Resource, ResourceClass>();
        static private Dictionary<int, Consumer> consumers = new Dictionary<int, Consumer>();
        static private Dictionary<int, bool> forcedToStopConsumers = new Dictionary<int, bool>();
        #endregion

        #region Awake
        public static void Awake()
        {
            RegisterResource(Resource.Work, 1000f);
            RegisterResource(Resource.Sand, 1000f);
        }
        #endregion

        #region Resources Methods
        public static void RegisterResource(Resource name, float initialAmount)
        {
            if (!resources.ContainsKey(name))
            {
                resources[name] = new ResourceClass(name, initialAmount);
            }
        }

        public static void AddResource(Resource name, float amount)
        {
            if (resources.TryGetValue(name, out var res))
            {
                res.Add(amount);
            }
            else
            {
                Debug.LogWarning($"Trying to add to unregistered resource: {name}");
            }
        }

        public static void TryAddRate(Resource name, float amount)
        {
            if (resources.TryGetValue(name, out var res))
            {
                res.AddRate(amount);
            }
            else
            {
                Debug.LogWarning($"Trying to add rate to unregistered resource: {name}");
            }
        }

        public static bool TryConsumeResource(Resource name, float amount)
        {
            if (resources.TryGetValue(name, out var res))
            {
                return res.TryConsume(amount);
            }
            Debug.LogWarning($"Trying to consume from unregistered resource: {name}");
            return false;
        }

        public static void TryUpdateResourcesByBuild(string codeName)
        {
            if (!ConstructionConfig.Instance.ConstructionConfigs.TryGetValue(codeName, out var config))
            {
                return;
            }

            foreach ((Resource resource, float cost) in config.cost)
            {
                TryConsumeResource(resource, cost);
            }

            /*
            foreach ((Resource resource, float rate) in config.rate)
            {
                TryAddRate(resource, rate); // Aqui
            }
            
        }

        public static float GetAmount(Resource name)
        {
            return resources.TryGetValue(name, out var res) ? res.Amount : 0f;
        }

        public static float GetRate(Resource name)
        {
            return resources.TryGetValue(name, out var res) ? res.Rate : 0f;
        }

        public static Dictionary<Resource, ResourceClass> GetAllResources()
        {
            return resources;
        }

        public static bool HasEnough(Resource name, float amount)
        {
            return resources.ContainsKey(name) && resources[name].Amount >= amount;
        }

        public static void UpdateResources()
        {
            foreach (ResourceClass resource in resources.Values)
            {
                resource.UpdateAmount();
            }
        }
        #endregion

        #region Consumers Methods
        public static bool TryAddConsumer(int id, string codeName)
        {
            if (consumers.ContainsKey(id))
            {
                Debug.LogWarning($"Consumer with ID {id} already exists.");
                return false;
            }

            var rates = ConstructionConfig.Instance.ConstructionConfigs[codeName].rate;
            if (rates.Values.All(v => v == 0)) return false;

            consumers[id] = new Consumer(id, codeName, false);

            BonusManager.RegisterConsumerInLocalBonuses(consumers[id]);
            return true;
        }

        public static void RemoveConsumer(int id, bool recycle = false)
        {
            if (consumers.ContainsKey(id))
            {
                Consumer consumer = consumers[id];
                foreach ((Resource resource, float rate) in consumer.rates)
                {
                    TryAddRate(resource, -rate);
                }

                if (recycle)
                {
                    var config = ConstructionConfig.Instance.ConstructionConfigs[consumer.codeName];
                    AddResource(Resource.Sand, -Mathf.Floor(config.cost[Resource.Sand] / 2));
                    AddResource(Resource.Work, config.recycleWorkCost); 
                }

                consumers.Remove(id);
            }
        }

        public static void UpdateConsumers()
        {
            Dictionary<int, bool> updatedStates = new Dictionary<int, bool>();

            foreach (Consumer consumer in consumers.Values)
            {
                if (consumer.isForceToStop)
                {
                    if (consumer.isOperative) TryDeactivateConsumer(consumer.id);
                    updatedStates[consumer.id] = false;
                    continue;
                }
                if (!consumer.isOperative) updatedStates[consumer.id] = TryActivateConsumer(consumer.id);
                else updatedStates[consumer.id] = TryDeactivateConsumer(consumer.id);
            }

            foreach ((int id, bool newState) in updatedStates)
            {
                var consumer = consumers[id];
                consumer.isOperative = newState;
                consumers[id] = consumer;
            }
        }

        private static bool TryActivateConsumer(int id)
        {
            Consumer consumer = consumers[id];
            if (consumer.isOperative) return true;

            foreach ((Resource resource, float rate) in consumer.rates)
            {
                if (rate >= 0) continue;

                if (GetAmount(resource) < -rate)
                {
                    return false;
                }
            }

            foreach ((Resource resource, float rate) in consumer.rates)
            {
                TryConsumeResource(resource, rate);
                TryAddRate(resource, rate);
            }

            consumer.isOperative = true; 
            return true;
        }

        private static bool TryDeactivateConsumer(int id)
        {
            Consumer consumer = consumers[id];
            if (!consumer.isOperative) return false;

            bool newState = true;

            foreach ((Resource resource, float rate) in consumer.rates)
            {
                if (rate >= 0) continue;

                if (GetAmount(resource) < -rate)
                {
                    newState = false;
                    break;
                }
            }

            if (!consumer.isForceToStop)
            {
                if (newState)
                {
                    foreach ((Resource resource, float rate) in consumer.rates)
                    {
                        if (rate > 0)
                        {
                            AddResource(resource, rate);
                        }
                        else
                        {
                            TryConsumeResource(resource, rate);
                        }
                    }
                }
                else
                {
                    foreach ((Resource resource, float rate) in consumer.rates)
                    {
                        TryAddRate(resource, -rate);
                    }
                }
            }
            else
            {
                foreach ((Resource resource, float rate) in consumer.rates)
                {
                    TryAddRate(resource, -rate);
                }
            }

            consumer.isOperative = false; 
            return newState;
        }


        public static void SetConsumerActive(int id, bool isForceToStop)
        {
            if (!consumers.ContainsKey(id)) return;

            Consumer consumer = consumers[id];

            consumer.isForceToStop = isForceToStop;

            consumers[id] = consumer;
        }

        public static Dictionary<int, Consumer> GetAllConsumers()
        {
            return consumers;
        }

public struct Consumer
{
    public int id;
    public string codeName;
    public ConstructionConfig.ResourceCost rates => ConstructionConfig.Instance.ConstructionConfigs[codeName].rate;
    public ConstructionInstance instance;
    public bool isOperative;
    public bool isForceToStop;
    public Vector3 Position => instance.Position;

    public Consumer(int id, string codeName, bool isOperative)
    {
        this.id = id;
        this.codeName = codeName;
        this.isOperative = isOperative;
        isForceToStop = false;
        DualMesh.Instance.builder.constructions.TryGetValue(id, out this.instance);
    }
}
        #endregion
    }
}
*/