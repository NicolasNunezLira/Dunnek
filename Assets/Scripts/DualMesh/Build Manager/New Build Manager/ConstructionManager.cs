using UnityEngine;
using System;
using System.Collections.Generic;

public class ConstructionManager
{
    private Dictionary<string, ConstructionData> dataById;
    private Dictionary<string, IConstructionPlacer> placers;

    public ConstructionManager(List<ConstructionData> dataList)
    {
        dataById = new();
        placers = new();

        foreach (var data in dataList)
        {
            dataById[data.id] = data;
        }

        placers[Placer.Single] = new SinglePlacer();
        placers[Placer.Wall] = new WallPlacer();
    }

    public void Build(string id, Vector3 start, Vector3? end = null)
    {
        if (!dataById.ContainsKey(id)) return;

        var data = dataById[id];
        var placer = placers[data.placerType];
        placer.Place(data, start, end);
    }
}

/*
Runtime use

constructionManager.Build("house", position);

or 

constructionManager.Build("wall", pointA, pointB)
*/