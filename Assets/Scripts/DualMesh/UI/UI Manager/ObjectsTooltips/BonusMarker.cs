using UnityEngine;

public class BonusMarker : MonoBehaviour
{
    [SerializeField] private GameObject markerPrefab;
    private GameObject markerInstance;

    public void ShowMarker()
    {
        if (markerPrefab == null) return;
        if (markerInstance != null) return;

        Vector3 pos = transform.position + Vector3.up * 2f;
        markerInstance = GameObject.Instantiate(markerPrefab, pos, Quaternion.identity);
        
        markerInstance.AddComponent<LookAtCamera>();
    }

    public void HideMarker()
    {
        if (markerInstance != null)
        {
            Destroy(markerInstance);
            markerInstance = null;
        }
    }
}
