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
            */
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