using TMPro;
using UnityEngine;
using ResourceSystem;

public class TimerUI : MonoBehaviour
{
    public TextMeshProUGUI timeText;

    void Update()
    {
        timeText.text = $"Turn: {TimeManager.Instance.turn}";
    }
}
