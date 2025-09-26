using UnityEngine;
using Utils;
using UnityEngine.EventSystems;

public class ClickeableObject : MonoBehaviour
{
    [SerializeField] private string tooltipInfo = "Clickeable Object";
    private BonusRadiusVisualizer radiusVisualizer;
    private bool isInitialized = false, isClicked = false;

    private void OnMouseDown()
    {
        if (DualMesh.Instance.inMode != DualMesh.PlayingMode.Simulation) return;

        ResourcesLink link = GetComponent<ResourcesLink>();

        int? id = (link != null) ? (link.IsConsumer ? link.ConsumerId : null) : null;

        //string info = (link != null) ? link.GetInfoString() : null;
        string info = (link != null) ? link.GetInfoStringWithBonuses() : null;

        string title = (link != null) ? link.Building.Config.displayName : tooltipInfo;       

        TooltipManager.Instance.ShowTooltip(transform, title, info, id);

        BonusVisualizerManager.Instance.ClearVisuals();

        if (link == null) return;

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

        isInitialized = true;
        isClicked = true;
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0) && isInitialized)
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

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