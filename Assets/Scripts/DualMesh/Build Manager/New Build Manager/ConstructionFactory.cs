using System;
using System.Collections.Generic;
using UnityEngine;

public class ConstructionFactory
{
    private Dictionary<string, Type> _actionsMap;

    public ConstructionFactory()
    {
        _actionsMap = new Dictionary<string, Type>()
        {
            { "PlaceOnGround", typeof(PlaceOnGroundAction) },
            { "ConsumeResources", typeof(ConsumeResourcesAction) },
            { "ConnectToNeighbors", typeof(ConnectToNeighborsAction) }
        };
    }

    public ConstructionInstance CreateConstruction
    (
        ConstructionData data,
        Vector3 position,
        Quaternion rotation
    )
    {
        GameObject prefab = data.prefabs;
    }
}