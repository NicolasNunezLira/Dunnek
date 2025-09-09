using UnityEngine;
using UnityEngine.UI;

public class UIButtonReference : MonoBehaviour
{
    public string buttonID;
    public Button button;
    public Outline outline;
    public Image iconImage;

    private void Awake()
    {
        if (button == null) button = GetComponent<Button>();
        if (outline == null) outline = GetComponent<Outline>();
        iconImage = GetComponentInChildren<Image>();
    }
}