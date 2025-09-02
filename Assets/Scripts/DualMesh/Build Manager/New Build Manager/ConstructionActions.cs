using UnityEngine;
using ResourceSystem;

public class PlaceOnGroundAction : IConstructionAction
{
    public void Execute(ConstructionInstance instance)
    {

    }
}

public class ConsumeResourcesAction : IConstructionAction
{
    public void Execute(ConstructionInstance instance)
    {
        foreach (PrefabCost cost in instance.data.cost)
        {
            foreach (ResourceAmount amount in cost.cost)
                ResourceManager.TryConsumeResource(amount.resource, amount.amount);
        }
    }
}

public class ConnectToNeighboursAction : IConstructionAction
{
    public void Execute(ConstructionInstance instance)
    {
        // Connect logic
    }
}