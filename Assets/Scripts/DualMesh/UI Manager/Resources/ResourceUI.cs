using TMPro;
using UnityEngine;
using ResourceSystem;

public class ResourceUI : MonoBehaviour
{
    public TextMeshProUGUI workersText;
    public TextMeshProUGUI sandText;

    void Update()
    {
        workersText.text = $"Work: {ResourceManager.Instance.GetAmount(ResourceName.Work)} ({ResourceManager.Instance.GetRate(ResourceName.Work)})";
        sandText.text = $"Sand: {ResourceManager.Instance.GetAmount(ResourceName.Sand)} ({ResourceManager.Instance.GetRate(ResourceName.Sand)})"; 
    }
}
