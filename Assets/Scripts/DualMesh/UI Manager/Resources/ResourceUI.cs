using TMPro;
using UnityEngine;
using ResourceSystem;

public class ResourceUI : MonoBehaviour
{
    public TextMeshProUGUI workersText;
    public TextMeshProUGUI sandText;

    void Update()
    {
        workersText.text = $"Workers: {ResourceManager.Instance.GetAmount("Workers")}";
        sandText.text = $"Sand: {ResourceManager.Instance.GetAmount("ConstructionSand")}"; 
    }
}
