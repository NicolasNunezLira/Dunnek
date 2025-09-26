using System.Collections.Generic;
using UnityEngine;
using ConstructionSystem;
using System.Linq;
using BonusSystem;

namespace ResourceSystem
{
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
            RegisterResource(Resource.Work, 1000f, true);
            RegisterResource(Resource.Food, 100f, true);
            RegisterResource(Resource.Morlaks, 100f, true);
            RegisterResource(Resource.Wisdom, 0f, true);
            RegisterResource(Resource.Power, 0f, true);
            RegisterResource(Resource.Energy, 0f, true);
            RegisterResource(Resource.Sand, 1000f, true);
            RegisterResource(Resource.Water, 10f, true);
            RegisterResource(Resource.Clay, 500f, true);
            RegisterResource(Resource.Stone, 0f, false);
            RegisterResource(Resource.Glass, 0f, false);
            RegisterResource(Resource.Limestone, 0f, false);
            RegisterResource(Resource.Copper, 0f, false);
            RegisterResource(Resource.Gold, 0f, false);
        }

        public static void RegisterResource(Resource name, float initialAmount, bool IsUnlocked)
        {
            if (!resources.ContainsKey(name))
                resources[name] = new ResourceClass(name, initialAmount, IsUnlocked);
        }
        #endregion

        #region - Lock - Unlock
        public static void UnlockResource(Resource name)
        {
            if (resources.TryGetValue(name, out var res))
            {
                res.Unlock();
            }
        }

        public static void LockResource(Resource name)
        {
            if (resources.TryGetValue(name, out var res))
            {
                res.Lock();
            }
        }

        public static IEnumerable<ResourceClass> GetUnlockedResources()
        {
            return resources.Values.Where(r => r.IsUnlocked);
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

            public void RemoveLocalBonus(LocalBonus bonus)
            {
                if (Bonuses.Contains(bonus))
                    Bonuses.Remove(bonus);
            }

        }
        #endregion
    }
    #endregion
}