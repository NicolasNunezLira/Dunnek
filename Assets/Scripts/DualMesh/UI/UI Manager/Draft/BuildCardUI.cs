using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DraftSystem; // Asegúrate de que esto esté apuntando al namespace correcto

public class CardUI : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI cardName;
    [SerializeField] private TextMeshProUGUI description;
    [SerializeField] private TextMeshProUGUI cost;
    [SerializeField] private Button selectButton;

    private CardInstance cardInstance;

    void Awake()
    {
        if (selectButton != null)
        {
            selectButton.onClick.AddListener(OnSelectCard);
        }
    }

    public void Setup(CardInstance instance)
    {
        cardInstance = instance;

        var data = cardInstance.cardData;
        if (icon != null) icon.sprite = data.icon;
        if (cardName != null) cardName.text = data.cardName;
        if (description != null) description.text = data.description;
        if (cost != null) cost.text = data.cost.ToString();
    }

    private void OnSelectCard()
    {
        DraftUI.Instance.OnCardSelected(this);
    }

    public CardInstance GetInstance() => cardInstance;
}
