using TMPro;
using UnityEngine;
using ResourceSystem;

public class ResourceUI : MonoBehaviour
{
    public TextMeshProUGUI workersText;
    public TextMeshProUGUI workForceText;
    public TextMeshProUGUI sandText;

    void Update()
    {
        workersText.text = $"Workers: {ResourceManager.Instance.GetAmount("Workers")}";
        workForceText.text = $"Work Force: {ResourceManager.Instance.GetAmount("Work Force")}";
        sandText.text = $"Sand: {ResourceManager.Instance.GetAmount("Sand")}"; 
    }
}
