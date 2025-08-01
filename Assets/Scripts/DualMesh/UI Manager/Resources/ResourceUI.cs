using TMPro;
using UnityEngine;
using ResourceSystem;

public class ResourceUI : MonoBehaviour
{
    public TextMeshProUGUI workersText;
    public TextMeshProUGUI sandText;

    void Update()
    {
        workersText.text = $"Work: {ResourceManager.GetAmount(Resource.Work)} ({ResourceManager.GetRate(Resource.Work)})";
        sandText.text = $"Sand: {ResourceManager.GetAmount(Resource.Sand)} ({ResourceManager.GetRate(Resource.Sand)})"; 
    }
}