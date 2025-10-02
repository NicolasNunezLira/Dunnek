using UnityEngine;
using System.Collections.Generic;

namespace ResourceSystem
{
    public class CollectorBuilding : MonoBehaviour
    {
        public ResourceSystem.Resource targetResource;
        public float collectRate = 1f;
        public float radius = 5f;

        private List<ResourceNode> nodes = new();

        void Start()
        {
            ScanForNodes();
        }

        void ScanForNodes()
        {
            var hits = Physics.OverlapSphere(transform.position, radius);
            foreach (var hit in hits)
            {
                var node = hit.GetComponent<ResourceNode>();
                if (node != null && node.resourceType == targetResource)
                {
                    nodes.Add(node);
                    node.RegisterCollector(this);
                }
            }
        }

        public float Collect()
        {
            float total = 0f;
            foreach (var node in nodes.ToArray())
            {
                total += node.Extract(collectRate);
            }
            return total;
        }

        public void OnResourceNodeDepleted(ResourceNode node)
        {
            nodes.Remove(node);
        }

        private void OnDestroy()
        {
            foreach (var node in nodes.ToArray())
                node.UnregisterCollector(this);

            nodes.Clear();
        }

        public List<ResourceNode> GetNodes() => nodes;
        public int GetNodeCount() => nodes.Count;
    }
}
