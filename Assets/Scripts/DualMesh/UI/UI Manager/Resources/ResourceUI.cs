using TMPro;
using UnityEngine;
using ResourceSystem;
using System.Collections.Generic;
using UnityEngine.UI;

public class ResourceUI : MonoBehaviour
{
    [Header("Resources info prefab")]
    [SerializeField] private GameObject resourceSlotPrefab;

    [Header("Contenedor")]
    [SerializeField] private Transform resourcePanelParent;

    private class ResourceSlot
    {
        public GameObject slotGO;
        public TextMeshProUGUI amountText;
        public TextMeshProUGUI rateText;
    }

    private Dictionary<Resource, ResourceSlot> resourceSlots = new();

    void Start()
    {
        foreach (var res in ResourceManager.GetUnlockedResources())
        {
            CreateResourceSlot(res);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(this.GetComponent<RectTransform>());
    }

    void Update()
    {
        foreach (var kvp in resourceSlots)
        {
            var res = ResourceManager.GetAllResources()[kvp.Key];
            var slot = kvp.Value;

            // Mostrar solo si amount o rate son distintos de 0
            bool shouldShow = res.Amount != 0 || res.Rate != 0;

            slot.slotGO.SetActive(shouldShow);

            if (shouldShow)
            {
                slot.amountText.text = $"{res.Amount:0.00}";
                slot.rateText.text = $"{res.Rate:+0.00;-0.00;0.00}";
            }
        }
    }

    private void CreateResourceSlot(ResourceClass res)
    {
        GameObject slotGO = Instantiate(resourceSlotPrefab, resourcePanelParent);
        slotGO.name = res.Name.ToString() + "Slot";

        // Icon
        var slotImage = slotGO.transform.Find("Image").GetComponent<Image>();
        var sprite = ResourceSystem.ResoureIconManager.Instance.GetIcon(res.Name);
        if (sprite != null) slotImage.sprite = sprite;

        // Texts
        var amountText = slotGO.transform.Find("Texts/Amount").GetComponent<TextMeshProUGUI>();
        var rateText = slotGO.transform.Find("Texts/Rate").GetComponent<TextMeshProUGUI>();

        amountText.text = $"{res.Amount:0.00}";
        rateText.text = $"{res.Rate:+0.00;-0.00;0.00}";

        resourceSlots[res.Name] = new ResourceSlot 
        { 
            slotGO = slotGO,
            amountText = amountText, 
            rateText = rateText 
        };
    }
}
