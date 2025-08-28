using UnityEngine;
using UnityEngine.UI;
using StormSystem;
using TMPro;

public class WindUI : MonoBehaviour
{
    [SerializeField] private RectTransform arrow;
    [SerializeField] private TextMeshProUGUI windLabel;

    private void Start()
    {
        StormManager.Instance.OnWindChanged += UpdateWindUI;

        UpdateWindUI(StormManager.Instance.currentDirection);
    }

    void Update()
    {
        if (windLabel != null)
        {
            int turnsRemaining = StormManager.Instance.eventTurnsRemaining;
            windLabel.text = "Remaining turns " + turnsRemaining;
        }
    } 

    private void OnDestroy()
    {
        if (StormManager.Instance != null)
        {
            StormManager.Instance.OnWindChanged -= UpdateWindUI;
        }
    }

    private void UpdateWindUI(Vector2 windDir)
    {
        float angle = Mathf.Atan2(windDir.x, windDir.y) * Mathf.Rad2Deg;
        arrow.localEulerAngles = new Vector3(0, 0, -angle);
        /*
        if (windLabel != null)
        {
            int turnsRemaining = StormManager.Instance.eventTurnsRemaining;
            windLabel.text = "Remaining turns " + turnsRemaining;
        }
        */
    }
}