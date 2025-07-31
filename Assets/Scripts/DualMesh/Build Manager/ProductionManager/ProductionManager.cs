using System.Collections.Generic;
using Data;
using Utils;

public class ProductionManager : Singleton<ProductionManager>
{
    public Dictionary<int, ProductiveConstruction> constructions = new();

    public float totalRequiredWorkers = 0;

    protected override void Awake()
    {
        base.Awake();
    }

    void Update()
    {
        if (totalRequiredWorkers <= ResourceSystem.ResourceManager.Instance.GetAmount(ResourceSystem.ResourceName.Workers))
        {
            Dictionary<int, bool> activate = new();
            foreach ((int i, ProductiveConstruction construction) in constructions)
            {
                if (!construction.isActive)
                {
                    activate[i] = ResourceSystem.ResourceManager.Instance.TryConsumeResource(ResourceSystem.ResourceName.Workers, construction.requirements.Workers);
                }
            }

            foreach ((int i, bool act) in activate)
            {
                var construction = constructions[i];
                construction.isActive = act;
                constructions[i] = construction;
            }
        }
    }

    public void UpdateResources()
    {
        foreach ((int i, ProductiveConstruction construction) in constructions)
        {
            if (!construction.isActive) continue;

            ResourceSystem.ResourceManager.Instance.AddResource(ResourceSystem.ResourceName.Sand, construction.flow.Sand);
        }
    }
}

public struct ProductiveConstruction
{
    public ConstructionType type;
    public bool isActive;
    public ConstructionConfig.ResourceCost requirements => ConstructionConfig.Instance.constructionConfig[type].requirements;
    public ConstructionConfig.ResourceCost flow => ConstructionConfig.Instance.constructionConfig[type].production.flow;

    public ProductiveConstruction(ConstructionType type, bool isActive)
    {
        this.type = type;
        this.isActive = isActive;
    }
}