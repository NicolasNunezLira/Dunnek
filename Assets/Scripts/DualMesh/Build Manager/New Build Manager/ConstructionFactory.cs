using System;
using System.Collections.Generic;
using UnityEngine;

public class ConstructionFactory
{
    int currentId = 1;
    Dictionary<string, GameObject> prefabLookup;
    private Dictionary<string, Type> _actionsMap;
    private Dictionary<Placer, IConstructionPlacer> _placersMap;

    public ConstructionFactory(Dictionary<string, GameObject> prefabLookup)
    {
        this.prefabLookup = prefabLookup;

        _actionsMap = new Dictionary<string, Type>()
        {
            { "PlaceOnGround", typeof(PlaceOnGroundAction) },
            { "ConsumeResources", typeof(ConsumeResourcesAction) },
            { "ConnectToNeighbors", typeof(ConnectToNeighboursAction) }
        };

        _placersMap = new Dictionary<Placer, IConstructionPlacer>()
        {
            { Placer.Single, new SinglePlacer(this.prefabLookup)},
            { Placer.Wall, new WallPlacer(this.prefabLookup)}
        };
    }

    /// <summary>
    ///  Creates a construction instance from data, position and rotation
    /// </summary>
    public ConstructionInstance Create
    (
        ConstructionData data,
        Vector3 position,
        Quaternion rotation
    )
    {
        if (data.prefabs == null || data.prefabs.Count == 0)
        {
            Debug.LogError($"No prefabs defined for construction {data.id}");
            return null;
        }

        string prefabName = data.prefabs[0];
        if (!prefabLookup.TryGetValue(prefabName, out GameObject prefab))
        {
            Debug.LogError($"Prefab {prefabName} not found for construction {data.id}");
            return null;
        }

        GameObject instance = GameObject.Instantiate(prefab, position, rotation);
        instance.name = $"{data.name}_{currentId}";
        currentId++;

        return new ConstructionInstance(currentId, instance, data, position, rotation);
    }
}