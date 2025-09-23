using System.Collections.Generic;
using UnityEngine;
using Utils;

public class BonusVisualizerManager : Singleton<BonusVisualizerManager>
{
    private readonly List<BonusMarker> activeMarkers = new();
    private readonly List<BonusRadiusVisualizer> activeRadii = new();

    public void RegisterMarker(BonusMarker marker) => activeMarkers.Add(marker);
    public void RegisterRadius(BonusRadiusVisualizer radius) => activeRadii.Add(radius);

    public void ClearVisuals()
    {
        foreach (var m in activeMarkers) m?.HideMarker();
        foreach (var r in activeRadii) r?.HideRadius();
        activeMarkers.Clear();
        activeRadii.Clear();
    }
}