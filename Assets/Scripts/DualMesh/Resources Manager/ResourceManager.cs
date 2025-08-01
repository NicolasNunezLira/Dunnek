using System.Collections.Generic;
using System.Data.Common;
using Data;
using UnityEngine;

namespace ResourceSystem {
    public static class ResourceManager
    {
        static private Dictionary<Resource, ResourceClass> resources = new Dictionary<Resource, ResourceClass>();
        static private Dictionary<int, Consumer> consumers = new Dictionary<int, Consumer>();

        public static void Awake()
        {
            RegisterResource(Resource.Work, 100f);
            RegisterResource(Resource.Sand, 100f);
        }

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

        public static void AddRate(Resource name, float amount)
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

        public static void TryAddConsumer(int id, ConstructionType type, bool isOperative)
        {
            var rates = ConstructionConfig.Instance.constructionConfig[type].rate;
            if (rates[Resource.Sand] == 0 && rates[Resource.Work] == 0) return;

            consumers[id] = new Consumer(id, type, isOperative);
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
    }
}