using System.Collections.Generic;
using UnityEngine;

#region Abstract Class for Construction Instances
public abstract class ConstructionInstance
{
    public int id;
    public ConstructionData data;
    public Vector3? position;

    protected ConstructionInstance(
        int id,
        ConstructionData data,
        Vector3? position
    )
    {
        this.id = id;
        this.data = data;
        this.position = position;
    }

    public abstract void Destroy();
}
#endregion

#region Single Construction Instance
public class SimpleConstructionInstance : ConstructionInstance
{
    public GameObject instance;
    public Quaternion rotation = Quaternion.identity;

    public SimpleConstructionInstance(
        int id,
        ConstructionData data,
        GameObject instance,
        Vector3? position,
        Quaternion rotation = default
    ) : base(id, data, position)
    {
        this.rotation = rotation;
        this.instance = instance;
    }

    public override void Destroy()
    {
        if (instance != null)
            GameObject.Destroy(instance);
    }
}
#endregion

#region Composite Construction Instance
public class CompositeConstructionInstance : ConstructionInstance
{
    public List<SimpleConstructionInstance> parts;

    public CompositeConstructionInstance(
        int id,
        ConstructionData data
    ) : base(id, data, position: null)
    {
    }

    public void AddPart(SimpleConstructionInstance part)
    {
        parts.Add(part);
    }

    public override void Destroy()
    {
        foreach (SimpleConstructionInstance part in parts)
        {
            part.Destroy();
        }
    }
}
#endregion