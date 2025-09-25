using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIButtonReference : MonoBehaviour
{
    public string buttonID;
    public Button button;
    public Outline outline;
    public Image iconImage;
    public TextMeshProUGUI label;
    public Transform costPanel;
    public GameObject resourceSlotPrefab;

    private void Awake()
    {
        if (button == null) button = GetComponent<Button>();
        if (outline == null) outline = GetComponent<Outline>();
        if (costPanel == null) costPanel = GetComponentInChildren<Transform>();
        iconImage = GetComponentInChildren<Image>();
        label = GetComponentInChildren<TextMeshProUGUI>();
    }
}