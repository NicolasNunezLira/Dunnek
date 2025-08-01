using System.Collections.Generic;
using Data;
using Utils;
using ResourceSystem;

public class ProductionManager : Singleton<ProductionManager>
{
    public Dictionary<int, ProductiveConstruction> constructions = new();

    public float totalRequiredWorkers = 0;

    protected override void Awake()
    {
        base.Awake();
    }

    public void UpdateProductiveConstructions()
    {
        Dictionary<int, bool> updatedStates = new();

        foreach ((int id, ProductiveConstruction construction) in constructions)
        {
            bool shouldBeActive = construction.isActive;

            if (!construction.isActive)
            {
                // Si está inactiva, intenta activarse consumiendo recursos
                if (ResourceManager.TryConsumeResource(Resource.Work, construction.rates[Resource.Work]))
                {
                    shouldBeActive = true;
                }
            }
            else
            {
                // Si está activa, revisa que aún cumple los requisitos
                if (!ResourceManager.HasEnough(Resource.Work, construction.rates[Resource.Work]))
                {
                    shouldBeActive = false;
                    //resourceManager.AddResource(Resource.Workers, construction.requirements.Workers);
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