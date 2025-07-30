using System.Collections.Generic;
using Utils;
using UnityEngine;

namespace ResourceSystem {
    public class ResourceManager : Singleton<ResourceManager>
    {
        private Dictionary<ResourceName, Resource> resources = new Dictionary<ResourceName, Resource>();

        protected override void Awake()
        {
            base.Awake();

            RegisterResource(ResourceName.Workers, 1f);
            RegisterResource(ResourceName.AvailableWorkers, 1f);
            RegisterResource(ResourceName.WorkForce, 1f);
            RegisterResource(ResourceName.Sand, 0f);
        }

        public void RegisterResource(ResourceName name, float initialAmount)
        {
            if (!resources.ContainsKey(name))
            {
                resources[name] = new Resource(name, initialAmount);
            }
        }

        public void AddResource(ResourceName name, float amount)
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

        public bool TryConsumeResource(ResourceName name, float amount)
        {
            if (resources.TryGetValue(name, out var res))
            {
                return res.TryConsume(amount);
            }
            Debug.LogWarning($"Trying to consume from unregistered resource: {name}");
            return false;
        }

        public float GetAmount(ResourceName name)
        {
            return resources.TryGetValue(name, out var res) ? res.Amount : 0f;
        }

        public Dictionary<ResourceName, Resource> GetAllResources()
        {
            return resources;
        }

        public void UpdateWorkForce()
        {
            resources[ResourceName.WorkForce].Add(resources[ResourceName.AvailableWorkers].Amount);
        }
    }
}