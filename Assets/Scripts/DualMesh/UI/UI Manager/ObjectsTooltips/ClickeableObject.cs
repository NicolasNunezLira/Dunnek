using UnityEngine;
using Utils;
using UnityEngine.EventSystems;
using ResourceSystem;
using System.Text;

public class ClickeableObject : MonoBehaviour
{
    [SerializeField] private string tooltipInfo = "Clickeable Object";
    private BonusRadiusVisualizer radiusVisualizer;
    private bool isInitialized = false, isClicked = false;

    private void OnMouseDown()
    {
        if (DualMesh.Instance.inMode != DualMesh.PlayingMode.Simulation) return;

        string title = tooltipInfo;
        string info = "";

        BonusVisualizerManager.Instance.ClearVisuals();

        // --- Caso 1: Buildings normales (ResourcesLink) ---
        ResourcesLink link = GetComponent<ResourcesLink>();
        int? id = (link != null) ? (link.IsConsumer ? link.ConsumerId : null) : null;

        if (link != null)
        {
            title = link.Building.Config.displayName;
            info = link.GetInfoStringWithBonuses();

            TooltipManager.Instance.ShowTooltip(transform, title, info, id);

            // Visualizar bonuses
            if (link.Building.IsAffectingByAnyBonus() && !isClicked)
            {
                foreach (var (_, bonusesList) in link.Building.AffectingBonuses)
                {
                    foreach (var bonus in bonusesList)
                    {
                        GameObject obj = bonus.Building.Obj;
                        if (obj == null) continue;

                        BonusVisualizerManager.Instance.RegisterHighlightedObject(obj);
                        RecursivelyFunctions.SetLayerRecursively(obj, 12);
                    }
                }
            }

            // Visualizar radios de bonus
            if (link.IsBonusProvider && link.LocalBonuses.Count > 0)
            {
                radiusVisualizer = GetComponentInChildren<BonusRadiusVisualizer>();
                if (radiusVisualizer != null)
                {
                    foreach (var l in link.LocalBonuses)
                    {
                        radiusVisualizer.ShowRadius(l.radius);
                        BonusVisualizerManager.Instance.RegisterRadius(radiusVisualizer);
                    }
                }
            }
        }

        // --- Caso 2: ResourceNode ---
        var node = GetComponent<ResourceNode>();
        if (node != null)
        {
            title = $"Resource Node";
            info = $"{node.resourceType} disponible: {node.amount:F1}";
            TooltipManager.Instance.ShowTooltip(transform, title, info, null);

            // Highlight collectors que recolectan este nodo
            foreach (var building in node.GetCollectors())
            {
                if (building == null) continue;
                BonusVisualizerManager.Instance.RegisterHighlightedObject(building.gameObject);
                RecursivelyFunctions.SetLayerRecursively(building.gameObject, 12);
            }
        }

        // --- Caso 3: CollectorBuilding ---
        var collector = GetComponent<CollectorBuilding>();
        if (collector != null)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Recolector de {collector.targetResource}");
            sb.AppendLine($"Tasa por turno: {collector.collectRate * collector.GetNodeCount()}");
            title = "Collector";
            info = sb.ToString();
            TooltipManager.Instance.ShowTooltip(transform, title, info, null);

            // Highlight nodos que recolecta
            foreach (var n in collector.GetNodes())
            {
                if (n == null) continue;
                BonusVisualizerManager.Instance.RegisterHighlightedObject(n.gameObject);
                RecursivelyFunctions.SetLayerRecursively(n.gameObject, 12);
            }
        }

        isInitialized = true;
        isClicked = true;
    }
    
    private void Update()
    {
        if (Input.GetMouseButtonDown(0) && isInitialized)
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.collider.gameObject != this.gameObject)
                {
                    BonusVisualizerManager.Instance.ClearVisuals();
                    TooltipManager.Instance.HideTooltip();
                    isInitialized = false;
                    isClicked = false;
                }
            }
            else
            {
                BonusVisualizerManager.Instance.ClearVisuals();
                TooltipManager.Instance.HideTooltip();
                isInitialized = false;
                isClicked = false;
            }
        }
    }
}
