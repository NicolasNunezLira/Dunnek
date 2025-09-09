using UnityEngine;
using UnityEngine.UI;

public class ResourceTooltip : MonoBehaviour
{
    [SerializeField] private Button toggleButton;
    private int consumerID;
    private bool isActive;

    public void Setup(int id, bool currentState)
    {
        this.consumerID = id;
        this.isActive = currentState;
        UpdateButtonText();

        toggleButton.onClick.RemoveAllListeners();
        toggleButton.onClick.AddListener(ToggleProduction);
    }

    private void ToggleProduction()
    {
        isActive = !isActive;
        ResourceSystem.ResourceManager.SetConsumerActive(consumerID, isActive);
        UpdateButtonText();
    }

    private void UpdateButtonText()
    {
        toggleButton.GetComponentInChildren<Text>().text = isActive ? "Deactivate production" : "Activate production";
    } 
}