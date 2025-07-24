using System.Collections.Generic;
using UnityEngine;

namespace ResourceSystem {
    public class ResourceManager : MonoBehaviour
    {
        private Dictionary<string, Resource> resources = new Dictionary<string, Resource>();

        public void RegisterResource(string name, int initialAmount)
        {
            if (!resources.ContainsKey(name))
            {
                resources[name] = new Resource(name, initialAmount);
            }
        }

        public void AddResource(string name, int amount)
        {
            if (resources.TryGetValue(name, out var res))
            {
                res.Add(amount);
            }
            else
            {
                Debug.LogWarning($"Tryinh to add to unregistered resource: {name}");
            }
        }

        public bool TryConsumeResource(string name, int amount)
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