using UnityEngine;
using System.Collections.Generic;
using Unity.Mathematics;
using Building;
using System;

#region SinglePlacer
public class SinglePlacer : IConstructionPlacer
{
    private readonly Dictionary<string, GameObject> prefabLookup;

    public SinglePlacer(Dictionary<string, GameObject> prefabLookup)
    {
        this.prefabLookup = prefabLookup;
    }

    public ConstructionInstance Place(
        int currentId,
        ConstructionData data,
        Vector3 start,
        Vector3? end = null,
        Quaternion? rotation = null,
        int currentCompositeConstructionID = -1)
    {
        if (data.prefabs == null || data.prefabs.Count == 0)
        {
            Debug.LogError($"[SinglePlacer] No prefabs defined for construction {data.id}");
            return null;
        }

        string prefabName = data.prefabs[0];
        if (!prefabLookup.TryGetValue(prefabName, out GameObject prefab))
        {
            Debug.LogError($"[SinglePlacer] Prefab {prefabName} not fount for construction {data.id}");
            return null;
        }

        GameObject instance = GameObject.Instantiate(prefab, start, rotation ?? Quaternion.identity);
        instance.name = $"{data.id}_{currentId}";

        return new SimpleConstructionInstance(currentId, data, instance, start, rotation ?? Quaternion.identity);
    }
}
#endregion

#region WallPlacer
public class WallPlacer : IConstructionPlacer
{
    private readonly Dictionary<string, GameObject> prefabLookup;

    private enum Axis { X, Z }

    public WallPlacer(Dictionary<string, GameObject> prefabLookup)
    {
        this.prefabLookup = prefabLookup;
    }

    public ConstructionInstance Place(
        int currentId,
        ConstructionData data,
        Vector3 start,
        Vector3? end = null,
        Quaternion? rotation = null,
        int currentCompositeId = -1)
    {
        if (!end.HasValue)
        {
            Debug.LogError("[WallPlacer] End point required.");
            return null;
        }

        if (data.prefabs == null || data.prefabs.Count < 2)
        {
            Debug.LogError($"[WallPlacer] Expected at least two prefabs (tower + segment) for {data.id}");
            return null;
        }

        // 0 -> tower, 1 -> segment
        if (!prefabLookup.TryGetValue(data.prefabs[0], out GameObject towerPrefab) ||
            !prefabLookup.TryGetValue(data.prefabs[1], out GameObject segmentPrefab))
        {
            Debug.LogError($"[WallPlacer] Missing prefabs '{data.prefabs[0]}' or '{data.prefabs[1]}'");
            return null;
        }

        Vector3 p1 = start;
        Vector3 p2 = end.Value;
        Vector3 dir = (p2 - p1);
        Vector3 dirXZ = new Vector3(dir.x, 0f, dir.z).normalized;
        float distance = Vector3.Distance(p1, p2);

        // --- medir el prefab de segmento en local: tamaños en X y Z (sin rotaciones aplicadas)
        // Crearemos un "sample" para medir y luego lo reutilizamos como el primer segmento.
        GameObject sample = GameObject.Instantiate(segmentPrefab, p1, Quaternion.identity);
        Vector3 originalScale = sample.transform.localScale;

        Vector3 sizeLocal = CalculateLocalBoundsXZ(sample.transform); // longitudes en espacio local del root
        float lenX = Mathf.Max(0.0001f, sizeLocal.x);
        float lenZ = Mathf.Max(0.0001f, sizeLocal.z);
        // elegir eje horizontal más largo del prefab (X o Z)
        Axis longAxis = (lenX >= lenZ) ? Axis.X : Axis.Z;
        float baseSegmentLength = (longAxis == Axis.X) ? lenX : lenZ;

        // número de segmentos para cubrir la distancia
        int segments = Mathf.Max(1, Mathf.FloorToInt(distance / baseSegmentLength));
        float adjustedLength = distance / segments;

        // rotación que alinea el eje largo elegido con la dirección p1->p2
        Quaternion align =
            Quaternion.FromToRotation(
                (longAxis == Axis.X) ? Vector3.right : Vector3.forward,
                dirXZ);

        // factor de escala SOLO sobre el eje largo
        float scaleFactor = adjustedLength / baseSegmentLength;

        // compuesta a devolver
        var composite = new CompositeConstructionInstance(currentCompositeId, data);

        // colocar TORRE INICIAL
        GameObject t0 = GameObject.Instantiate(towerPrefab, p1, Quaternion.identity);
        composite.AddPart(new SimpleConstructionInstance(currentId, data, t0, p1));
        currentId++;

        // colocar segmentos (centros espaciados uniformemente)
        for (int i = 0; i < segments; i++)
        {
            Vector3 center = p1 + dirXZ * ((i + 0.5f) * adjustedLength);

            GameObject seg = (i == 0) ? sample : GameObject.Instantiate(segmentPrefab, center, Quaternion.identity);

            // aplicar rotación de alineación del eje + up
            seg.transform.rotation = align;

            // restablecer escala base y escalar sólo el eje largo
            Vector3 s = (i == 0) ? originalScale : seg.transform.localScale;
            if (longAxis == Axis.X) s = new Vector3(s.x * scaleFactor, s.y, s.z);
            else /* Axis.Z */            s = new Vector3(s.x, s.y, s.z * scaleFactor);
            seg.transform.localScale = s;

            // posicionar
            seg.transform.position = center;

            composite.AddPart(new SimpleConstructionInstance(currentId, data, seg, center));
            currentId++;
        }

        // colocar TORRE FINAL
        GameObject t1 = GameObject.Instantiate(towerPrefab, p2, Quaternion.identity);
        composite.AddPart(new SimpleConstructionInstance(currentId, data, t1, p2));
        currentId++;

        return composite;
    }

    /// <summary>
    /// Calcula tamaño total en X y Z del prefab en el espacio local del root,
    /// agregando todos los Renderers hijos (funciona con child rotations).
    /// </summary>
    private static Vector3 CalculateLocalBoundsXZ(Transform root)
    {
        bool hasAny = false;
        Vector3 min = new Vector3(float.PositiveInfinity, 0f, float.PositiveInfinity);
        Vector3 max = new Vector3(float.NegativeInfinity, 0f, float.NegativeInfinity);

        // MeshRenderer + MeshFilter
        var meshFilters = root.GetComponentsInChildren<MeshFilter>(true);
        foreach (var mf in meshFilters)
        {
            if (mf.sharedMesh == null) continue;
            var b = mf.sharedMesh.bounds; // local al MeshFilter
            ExpandByBounds(root, mf.transform, b, ref min, ref max);
            hasAny = true;
        }

        // SkinnedMeshRenderer (usa localBounds)
        var skinned = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        foreach (var sk in skinned)
        {
            var b = sk.localBounds; // local al SkinnedMeshRenderer
            ExpandByBounds(root, sk.transform, b, ref min, ref max);
            hasAny = true;
        }

        if (!hasAny) return new Vector3(1f, 0f, 1f);

        return new Vector3(max.x - min.x, 0f, max.z - min.z);

        static void ExpandByBounds(Transform root, Transform child, Bounds localBounds, ref Vector3 min, ref Vector3 max)
        {
            // 8 vértices del bounds local del child → al espacio local del root
            var c = localBounds.center;
            var e = localBounds.extents;

            // combinaciones de ± extents
            for (int sx = -1; sx <= 1; sx += 2)
            for (int sy = -1; sy <= 1; sy += 2)
            for (int sz = -1; sz <= 1; sz += 2)
            {
                Vector3 pLocalChild = new Vector3(
                    c.x + sx * e.x,
                    c.y + sy * e.y,
                    c.z + sz * e.z);

                Vector3 pWorld = child.TransformPoint(pLocalChild);
                Vector3 pRoot = root.InverseTransformPoint(pWorld);

                if (pRoot.x < min.x) min.x = pRoot.x;
                if (pRoot.z < min.z) min.z = pRoot.z;
                if (pRoot.x > max.x) max.x = pRoot.x;
                if (pRoot.z > max.z) max.z = pRoot.z;
            }
        }
    }
}

#endregion
