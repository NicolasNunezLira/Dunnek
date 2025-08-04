using UnityEngine;
using Data;
using ResourceSystem;

namespace Building
{
    public partial class BuildSystem
    {
        public void ApplyActionCost(DualMesh.ActionMode action)
        {
            var config = ActionConfig.Instance.actionsConfig[action];

            foreach ((Resource resource, float cost) in config.cost)
            {
                ResourceManager.TryConsumeResource(resource, cost);
            }

            foreach ((Resource resource, float amount) in config.production)
            {
                ResourceManager.AddResource(resource, amount);
            }
        }
    }
}