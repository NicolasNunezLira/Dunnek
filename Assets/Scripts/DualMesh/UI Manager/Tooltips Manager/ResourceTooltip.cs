using UnityEngine;
using UnityEngine.UI;

public class ResourceTooltip : MonoBehaviour
{
    [SerializeField] private Button toggleButton;
    private int consumerID;
    private bool isActive;

    public void Setup(int consumerId, bool currentState)
    {
        this.consumerID = consumerID;
        this.isActive = isActive;
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