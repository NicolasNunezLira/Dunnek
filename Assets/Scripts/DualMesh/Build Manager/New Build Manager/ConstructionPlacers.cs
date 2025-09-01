using UnityEngine;
using System.Collections.Generic;
using Unity.Mathematics;

#region SinglePlacer
public class SinglePlacer : IConstructionPlacer
{
    public void Place(ConstructionData data, Vector3 start, Vector3? end = null)
    {
        GameObject.Instantiate(data.prefabs[0], start, Quaternion.identity);
    }
}
#endregion

#region WallPlacer
public class WallPlacer : IConstructionPlacer
{
    private BuildSystem buildSystem;

    public WallPlacer(BuildSystem system)
    {
        this.buildSystem = system;
    }

    public void Place(ConstructionData data, Vector3 start, Vector3? end = null)
    {
        if (!end.HasValue) return;

        Vector3 p1 = start;
        Vector3 p2 = end.Value;

        CompositeConstruction wall = new CompositeConstruction(
            buildSystem.currentCompositeConstructionID,
            CompositeConstruction.CompositeType.Wall
        );

        int x, z, idTower2;
        // Coloca torres
        (p1, _, _, _) = buildSystem.TryBuildATower(p1, wall);
        (p2, x, z, idTower2) = buildSystem.TryBuildATower(p2, wall);

        Vector3 dir = (p2 - p1).normalized;
        float distance = Vector3.Distance(p1, p2);

        int segments = Mathf.Max(1, Mathf.FloorToInt(distance / buildSystem.wallPrefabLength));
        float adjustedLength = distance / segments;
        Vector3 step = dir * adjustedLength;

        List<int2> allSupport = new();

        for (int i = 2; i < segments; i++)
        {
            Vector3 pos = p1 + step * (i - 0.5f);
            (x, z) = buildSystem.GridIndex(pos);
            float y = Mathf.Max(buildSystem.duneModel.sand[x, z], buildSystem.duneModel.terrain[x, z]) - 0.1f;
            Vector3 adjusted = new Vector3(pos.x, y, pos.z);

            Quaternion rotation = Quaternion.LookRotation(new Vector3(dir.x, 0, dir.z)) * Quaternion.Euler(0, 90, 0);
            GameObject wallSegment = buildSystem.GameObjectConstruction(
                ConstructionType.SegmentWall, x, z, rotation, adjusted
            );

            if (wallSegment != null)
            {
                Vector3 localScale = wallSegment.transform.localScale;
                localScale.x = adjustedLength / buildSystem.wallPrefabLength;
                wallSegment.transform.localScale = localScale;
            }

            buildSystem.AddPartToWall(wall);
            allSupport.Add(new int2(x, z));
        }

        buildSystem.currentCompositeConstructionID++;
        buildSystem.activePreview = PreviewManager.Instance.buildPreviews[ConstructionType.Tower];
        buildSystem.activePreview.SetActive(false);
    }
}
#endregion
