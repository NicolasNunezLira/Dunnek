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

        protected override void Awake()
        {
            base.Awake();

            cardContainer?.gameObject.SetActive(false);
            cardPrefab?.gameObject.SetActive(false);

            if (confirmButton != null)
            {
                confirmButton.onClick.AddListener(ConfirmSelection);
                confirmButton.gameObject.SetActive(false);
            }
        }

        public void ShowDraft(List<BuildCard> draftOptions)
        {
            ClearPreviousCards();

            cardContainer.gameObject.SetActive(true);
            confirmButton.gameObject.SetActive(true);

            foreach (var cardData in draftOptions)
            {
                var cardUI = Instantiate(cardPrefab, cardContainer);
                cardUI.Setup(cardData);
                cardUI.gameObject.SetActive(true);
                //cardUI.GetComponent<Button>().onClick.AddListener(() => OnCardSelected(cardUI));
                instantiatedCards.Add(cardUI);
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
                //gameObject.SetActive(false);
                cardContainer.gameObject.SetActive(false);
                confirmButton.gameObject.SetActive(false);

                DraftManager.Instance.currentState = DraftState.Idle;
                DualMesh.Instance.SetMode(DualMesh.PlayingMode.Simulation);
            }
        }

        private void ClearPreviousCards()
        {
            foreach (var card in instantiatedCards)
            {
                Object.Destroy(card.gameObject); // card.GameObject
            }

            instantiatedCards.Clear();
            selectedCard = null;
            //confirmButton.onClick.RemoveAllListeners();
        }
    }
}