using UnityEngine;

public class ClickeableObject : MonoBehaviour
{
    [SerializeField] private string tooltipInfo = "Clickeable Object";

    private void OnMouseDown()
    {
        if (DualMesh.Instance.inMode != DualMesh.PlayingMode.Simulation) return;

        TooltipManager.Instance.ShowTooltip(transform, tooltipInfo);
    }

    private void OnMouseExit()
    {
        TooltipManager.Instance.HideTooltip();
    }
}