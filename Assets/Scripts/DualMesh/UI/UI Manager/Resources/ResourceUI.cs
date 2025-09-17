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
        workersText.text = $"Work: {work.Amount} ({work.Rate})";

        var sand = ResourceManager.GetAllResources()[Resource.Sand];
        sandText.text = $"Sand: {sand.Amount} ({sand.Rate})";
    }
}   