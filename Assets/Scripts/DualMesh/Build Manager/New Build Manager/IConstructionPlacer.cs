using UnityEngine;

public interface IConstructionPlacer
{
    ConstructionInstance Place(
        int currentId,
        ConstructionData data,
        Vector3 start,
        Vector3? end = null,
        Quaternion? rotation = null,
        int currentCompositeConstructionID = -1);
}