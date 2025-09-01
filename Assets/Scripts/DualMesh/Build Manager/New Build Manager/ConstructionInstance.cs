using UnityEngine;

public class ConstructionInstance
{
    public GameObject obj;
    public ConstructionData data;
    public Vector3 position;
    public Quaternion rotation;

    public ConstructionInstance(
        GameObject obj,
        ConstructionData data,
        Vector3 position,
        Quaternion rotation
    )
    {
        this.obj = obj;
        this.data = data;
        this.position = position;
        this.rotation = rotation;
    }
}