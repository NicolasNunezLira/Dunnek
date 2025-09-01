public interface IConstructionPlacer
{
    void Place(
        ConstructionData data,
        Vector3 start,
        Vector3? end = null
    );
}