using UnityEngine;
using System;
using System.Collections.Generic;

public class ConstructionManager
{
    public List<GameObject> prefabReferences;
    private ConstructionFactory factory;
    private Dictionary<string, GameObject> prefabLookup;

    private List<ConstructionInstance> constructios;

    void Awake()
    {
        prefabLookup = new Dictionary<string, GameObject>();
        foreach (var prefab in prefabReferences)
        {
            prefabLookup[prefab.name] = prefab;
        }

        factory = new ConstructionFactory(prefabLookup);
    }
}

/*
Runtime use

constructionManager.Build("house", position);

or 

constructionManager.Build("wall", pointA, pointB)
*/