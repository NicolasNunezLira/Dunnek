using TMPro;
using UnityEngine;

public class TimerUI : MonoBehaviour
{
    public TextMeshProUGUI timeText;

    void Update()
    {
        timeText.text = $"Turn: {TimeManager.Instance.turn}";
    }
}
