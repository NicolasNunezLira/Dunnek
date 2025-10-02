using UnityEngine;
using System.Collections.Generic;
using ResourceSystem;
public class ResourceNode : MonoBehaviour
{
    public ResourceSystem.Resource resourceType;
    public float amount = 100f;

    private List<CollectorBuilding> collectors = new();

    public void RegisterCollector(CollectorBuilding collector)
    {
        if (!collectors.Contains(collector))
            collectors.Add(collector);
    }

    public void UnregisterCollector(CollectorBuilding collector)
    {
        collectors.Remove(collector);
    }

    public float Extract(float request)
    {
        float taken = Mathf.Min(request, amount);
        amount -= taken;

        if (amount <= 0)
            NotifyDepleted();

        return taken;
    }

    private void NotifyDepleted()
    {
        foreach (var collector in collectors.ToArray())
            collector.OnResourceNodeDepleted(this);

        collectors.Clear();
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        foreach (var collector in collectors.ToArray())
            collector.OnResourceNodeDepleted(this);

        collectors.Clear();
    }

    public List<CollectorBuilding> GetCollectors() => collectors;
}
