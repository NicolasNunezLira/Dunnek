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
        foreach (var kvp in instance.data.cost)
        {
            ResourceManager.instance.Consume(kvp.Key, kvp.Value);
        }
    }
}

public class ConnectToNeighboursAction : IConstructionAction
{
    public void Execute(Construction instance)
    {
        // Connect logic
    }
}