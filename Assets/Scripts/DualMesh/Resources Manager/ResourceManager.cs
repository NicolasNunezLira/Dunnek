using System.Collections.Generic;
using Data;
using UnityEngine;

namespace ResourceSystem {
    public static class ResourceManager
    {
        #region Variables
        static private Dictionary<Resource, ResourceClass> resources = new Dictionary<Resource, ResourceClass>();
        static private Dictionary<int, Consumer> consumers = new Dictionary<int, Consumer>();
        #endregion

        #region Awake
        public static void Awake()
        {
            RegisterResource(Resource.Work, 1f);
            RegisterResource(Resource.Sand, 40f);
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

        public static void TryUpdateResourcesByBuild(ConstructionType type)
        {
            if (!ConstructionConfig.Instance.constructionConfig.TryGetValue(type, out var config))
            {
                return;
            }

            foreach ((Resource resource, float cost) in config.cost)
            {
                TryConsumeResource(resource, cost);
            }

            foreach ((Resource resource, float rate) in config.rate)
            {
                TryAddRate(resource, rate);
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
        public static void TryAddConsumer(int id, ConstructionType type)
        {
            if (consumers.ContainsKey(id))
            {
                Debug.LogWarning($"Consumer with ID {id} already exists.");
                return;
            }

            var rates = ConstructionConfig.Instance.constructionConfig[type].rate;
            if (rates[Resource.Sand] == 0 && rates[Resource.Work] == 0) return;

            bool isOperative = true;
            foreach ((Resource resource, float rate) in rates)
            {
                if (rate >= 0) continue;

                if (-rate < GetAmount(resource))
                {
                    isOperative = false;
                    break;
                }
            }
            consumers[id] = new Consumer(id, type, isOperative);
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
                    var config = ConstructionConfig.Instance.constructionConfig[consumer.type];
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

            return true;
        }

        private static bool TryDeactivateConsumer(int id)
        {
            Consumer consumer = consumers[id];

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

            return newState;
        }

        public static Dictionary<int, Consumer> GetAllConsumers()
        {
            return consumers;
        }

        public struct Consumer
        {
            public int id;
            public ConstructionType type;
            public ConstructionConfig.ResourceCost rates => ConstructionConfig.Instance.constructionConfig[type].rate;
            public bool isOperative;

            public Consumer(int id, ConstructionType type, bool isOperative)
            {
                this.id = id;
                this.type = type;
                this.isOperative = isOperative;
            }
        }
        #endregion
    }
}