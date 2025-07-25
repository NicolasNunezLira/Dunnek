using System.Collections.Generic;
using Utils;
using UnityEngine;

namespace ResourceSystem {
    public class ResourceManager : Singleton<ResourceManager>
    {
        private Dictionary<string, Resource> resources = new Dictionary<string, Resource>();

        protected override void Awake()
        {
            base.Awake();

            RegisterResource("Workers", 0f);
            RegisterResource("Construction sand", 100f);
        }

        public void RegisterResource(string name, float initialAmount)
        {
            if (!resources.ContainsKey(name))
            {
                resources[name] = new Resource(name, initialAmount);
            }
        }

        public void AddResource(string name, float amount)
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

        public bool TryConsumeResource(string name, float amount)
        {
            if (resources.TryGetValue(name, out var res))
            {
                return res.TryConsume(amount);
            }
            Debug.LogWarning($"Trying to consume from unregistered resource: {name}");
            return false;
        }

        public float GetAmount(string name)
        {
            return resources.TryGetValue(name, out var res) ? res.Amount : 0f;
        }

        public Dictionary<string, Resource> GetAllResources()
        {
            return resources;
        }
    }
}