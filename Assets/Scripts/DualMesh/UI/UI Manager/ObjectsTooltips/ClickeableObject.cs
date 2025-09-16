using UnityEngine;

public class ClickeableObject : MonoBehaviour
{
    [SerializeField] private string tooltipInfo = "Clickeable Object";


    
    private void OnMouseDown()
    {
        if (DualMesh.Instance.inMode != DualMesh.PlayingMode.Simulation) return;

        ResourcesLink link = GetComponent<ResourcesLink>();

        int? id = (link != null) ? (link.IsConsumer ? link.ConsumerId : null) : null;
        
        string info = (link != null) ? link.GetInfoString() : null;

        TooltipManager.Instance.ShowTooltip(transform, tooltipInfo, info, id);
    }

    /*
    private void OnMouseExit()
    {
        TooltipManager.Instance.HideTooltip();
    }
    */
}