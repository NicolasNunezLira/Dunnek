using TMPro;
using UnityEngine;
using ResourceSystem;

public class ResourceUI : MonoBehaviour
{
    public TextMeshProUGUI workersText;
    public TextMeshProUGUI sandText;

    void Update()
    {
        var work = ResourceManager.GetAllResources()[Resource.Work];
        workersText.text = $"Work: {work.Amount:0.00} ({work.Rate:0.00})";

        var sand = ResourceManager.GetAllResources()[Resource.Sand];
        sandText.text = $"Sand: {sand.Amount:0.00} ({sand.Rate:0.00})";
    }
}   