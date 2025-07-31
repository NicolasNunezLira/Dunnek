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
        var resourceManager = ResourceSystem.ResourceManager.Instance;
        Dictionary<int, bool> updatedStates = new();

        foreach ((int id, ProductiveConstruction construction) in constructions)
        {
            bool shouldBeActive = construction.isActive;

            if (!construction.isActive)
            {
                // Si está inactiva, intenta activarse consumiendo recursos
                if (resourceManager.TryConsumeResource(ResourceSystem.ResourceName.Work, construction.rates.Work))
                {
                    shouldBeActive = true;
                }
            }
            else
            {
                // Si está activa, revisa que aún cumple los requisitos
                if (!resourceManager.HasEnough(ResourceSystem.ResourceName.Work, construction.rates.Work))
                {
                    shouldBeActive = false;
                    //resourceManager.AddResource(ResourceSystem.ResourceName.Workers, construction.requirements.Workers);
                }
            }

            updatedStates[id] = shouldBeActive;
        }

        // Aplicar cambios
        foreach ((int id, bool newState) in updatedStates)
        {
            var construction = constructions[id];
            construction.isActive = newState;
            constructions[id] = construction;
        }
    }
}

public struct ProductiveConstruction
{
    public ConstructionType type;
    public bool isActive;
    public ConstructionConfig.ResourceCost rates => ConstructionConfig.Instance.constructionConfig[type].rate;

    public ProductiveConstruction(ConstructionType type, bool isActive)
    {
        this.type = type;
        this.isActive = isActive;
    }
}