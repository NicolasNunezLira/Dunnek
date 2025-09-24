using System.Collections.Generic;
using UnityEngine;
using Utils;

public class BonusVisualizerManager : Singleton<BonusVisualizerManager>
{
    private readonly List<(GameObject obj, int originalLayer)> highlightedObjects = new();
    private readonly List<BonusRadiusVisualizer> activeRadii = new();

    public void RegisterHighlightedObject(GameObject obj)
    {
        bool alreadyRegistered = highlightedObjects.Exists(x => x.obj == obj);
        if (!alreadyRegistered)
            highlightedObjects.Add((obj, obj.layer));
    }
    
    public void RegisterRadius(BonusRadiusVisualizer radius) => activeRadii.Add(radius);

    public void ClearVisuals()
    {
        foreach (var (obj, originalLayer) in highlightedObjects)
        {
            if (obj != null)
                RecursivelyFunctions.SetLayerRecursively(obj, originalLayer);
        }
        foreach (var r in activeRadii) r?.HideRadius();
        highlightedObjects.Clear();
        activeRadii.Clear();
    }
}