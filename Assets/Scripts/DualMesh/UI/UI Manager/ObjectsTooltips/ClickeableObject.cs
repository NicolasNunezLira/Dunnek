using BonusSystem;
using ResourceSystem;
using UnityEngine;

public class ClickeableObject : MonoBehaviour
{
    [SerializeField] private string tooltipInfo = "Clickeable Object";
    private BonusRadiusVisualizer radiusVisualizer;
    private bool isInitialized = false;

    private void OnMouseDown()
    {
        if (DualMesh.Instance.inMode != DualMesh.PlayingMode.Simulation) return;

        ResourcesLink link = GetComponent<ResourcesLink>();

        int? id = (link != null) ? (link.IsConsumer ? link.ConsumerId : null) : null;

        //string info = (link != null) ? link.GetInfoString() : null;
        string info = (link != null) ? link.GetInfoStringWithBonuses() : null;

        TooltipManager.Instance.ShowTooltip(transform, tooltipInfo, info, id);

        BonusVisualizerManager.Instance.ClearVisuals();

        if (link == null) return;

        if (link.IsConsumer && link.ConsumerId.HasValue)
        {
            var consumer = ResourceManager.AllConsumers[link.ConsumerId.Value];

            foreach (var local in BonusManager.GetLocalBonuses())
            {
                if (local.AffectsConsumer(consumer) &&
                    local is IProviderInfo provider && provider.ProviderObject != null)
                {
                    var marker = provider.ProviderObject.GetComponent<BonusMarker>();
                    if (marker != null)
                    {
                        marker.ShowMarker();
                        BonusVisualizerManager.Instance.RegisterMarker(marker);
                    }
                }
            }
        }

        if (link.IsBonusProvider && link.LocalBonuses.Count > 0)
        {
            radiusVisualizer = GetComponent<BonusRadiusVisualizer>();
            if (radiusVisualizer != null)
            {
                foreach (var l in link.LocalBonuses)
                {
                    radiusVisualizer.ShowRadius(l.radius);
                    BonusVisualizerManager.Instance.RegisterRadius(radiusVisualizer);
                }
            }
        }

        isInitialized = true;
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0) && isInitialized)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.collider.gameObject != this.gameObject)
                {
                    BonusVisualizerManager.Instance.ClearVisuals();
                    TooltipManager.Instance.HideTooltip();
                    isInitialized = false;
                }
            }
            else
            {
                BonusVisualizerManager.Instance.ClearVisuals();
                TooltipManager.Instance.HideTooltip();
                isInitialized = false;
            }
        }
    }

    
}