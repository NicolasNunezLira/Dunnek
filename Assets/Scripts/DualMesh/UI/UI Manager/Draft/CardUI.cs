using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DraftSystem;
using ResourceSystem;

public class CardUI : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI cardName;
    [SerializeField] private TextMeshProUGUI description;
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
    }

    private void OnSelectCard()
    {
        DraftUI.Instance.OnCardSelected(this);
    }

    public CardInstance GetInstance() => cardInstance;
}
