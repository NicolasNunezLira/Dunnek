using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class BonusRadiusVisualizer : MonoBehaviour
{
    private LineRenderer lr;

    private void Awake()
    {
        lr = GetComponent<LineRenderer>();
        lr.loop = true;
        lr.useWorldSpace = false;
        lr.enabled = false;
    }

    public void ShowRadius(float radius, int segments = 64)
    {
        lr.enabled = true;
        lr.positionCount = segments;
        float angleStep = 2 * Mathf.PI / segments;

        for (int i = 0; i < segments; i++)
        {
            float angle = i * angleStep;
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;
            lr.SetPosition(i, new Vector3(x, 0, z));
        }
    }

    public void HideRadius()
    {
        if (lr == null) return;
        lr.enabled = false;
    }
}