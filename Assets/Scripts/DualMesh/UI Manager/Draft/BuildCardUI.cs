using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DraftSystem; // Asegúrate de que esto esté apuntando al namespace correcto

public class BuildCardUI : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI cardName;
    [SerializeField] private TextMeshProUGUI description;
    [SerializeField] private TextMeshProUGUI cost;
    [SerializeField] private Button selectButton;

    void Awake()
    {
        if (selectButton != null)
        {
            selectButton.onClick.AddListener(OnSelectCard);
        }
    }

    private void OnSelectCard()
    {
        DraftUI.Instance.OnCardSelected(this);
    }

    private BuildCard buildCard;

    public void Setup(BuildCard data)
    {
        buildCard = data;

        if (icon != null) icon.sprite = data.icon;
        if (cardName != null) cardName.text = data.cardName;
        if (description != null) description.text = data.description;
        if (cost != null) cost.text = data.cost.ToString();
    }

    public BuildCard GetData() => buildCard;
}
