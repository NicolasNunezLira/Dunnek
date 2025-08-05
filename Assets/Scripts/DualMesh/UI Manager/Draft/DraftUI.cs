using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Utils;

namespace DraftSystem
{
    public class DraftUI : Singleton<DraftUI>
    {
        [SerializeField] private Transform cardContainer;
        [SerializeField] private BuildCardUI cardPrefab;
        [SerializeField] private Button confirmButton;

        private List<BuildCardUI> instantiatedCards = new();
        private BuildCard selectedCard;

        public void ShowDraft(List<BuildCard> draftOptions)
        {
            ClearPreviousCards();

            foreach (var cardData in draftOptions)
            {
                var cardUI = Instantiate(cardPrefab, cardContainer);
                cardUI.Setup(cardData);
                cardUI.GetComponent<Button>().onClick.AddListener(() => OnCardSelected(cardUI));
            }
        }

        public void OnCardSelected(BuildCardUI selected)
        {
            selectedCard = selected.GetData();

            foreach (var cardUI in instantiatedCards)
            {
                var outline = cardUI.GetComponent<Outline>();
                if (outline) outline.enabled = cardUI == selected;
            }
        }

        private void ConfirmSelection()
        {
            if (selectedCard != null)
            {
                ConstructionUnlockerManager.UnlockConstruction(selectedCard.constructionType);

                ClearPreviousCards();
                gameObject.SetActive(false);
            }
        }

        private void ClearPreviousCards()
        {
            foreach (var card in instantiatedCards)
            {
                Object.Destroy(card); // card.GameObject
            }

            instantiatedCards.Clear();
            selectedCard = null;
            confirmButton.onClick.RemoveAllListeners();
        }
    }
}