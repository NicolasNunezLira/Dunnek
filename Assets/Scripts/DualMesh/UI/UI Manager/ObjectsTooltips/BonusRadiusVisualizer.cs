using System.Buffers.Text;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class BonusRadiusVisualizer : MonoBehaviour
{
    private LineRenderer lr, lrOverlay;
    private float baseHeight, factor = 1;

    private void Awake()
    {
        Transform lrTransform = transform.parent.Find("LocalBonusRadius");
        lr = lrTransform.GetComponentInChildren<LineRenderer>();
        lr.loop = true;
        lr.useWorldSpace = false;
        lr.enabled = false;

        Transform overlayTransform = transform.parent.Find("LocalBonusRadiusOutline");
        lrOverlay = overlayTransform.GetComponentInChildren<LineRenderer>();
        lrOverlay.loop = true;
        lrOverlay.useWorldSpace = false;
        lrOverlay.enabled = false;

        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            float minY = float.MaxValue;
            foreach (var r in renderers)
            {
                minY = Mathf.Min(minY, r.bounds.min.y);
            }
            baseHeight = transform.InverseTransformPoint(new Vector3(0, minY, 0)).y;
        }
        else
        {
            baseHeight = 0f;
        }
    }

    public void ShowRadius(float radius, int segments = 64)
    {
        factor = lr.useWorldSpace ? 1 : DualMesh.Instance.tileSize;

        lr.enabled = true;
        lr.positionCount = segments;

        lrOverlay.enabled = true;
        lrOverlay.positionCount = segments;

        float angleStep = 2 * Mathf.PI / segments;

        for (int i = 0; i < segments; i++)
        {
            float angle = i * angleStep;
            float x = Mathf.Cos(angle) * radius * factor;
            float z = Mathf.Sin(angle) * radius * factor;
            lr.SetPosition(i, new Vector3(x, baseHeight, z));
            lrOverlay.SetPosition(i, new Vector3(x, baseHeight, z));
        }
    }

    public void HideRadius()
    {
        if (lr != null) lr.enabled = false;
        if (lrOverlay != null) lrOverlay.enabled = false;
    }
}