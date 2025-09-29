using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Utils;

namespace DraftSystem
{
    public class DraftUI : Singleton<DraftUI>
    {
        [SerializeField] private Transform cardContainer;
        [SerializeField] private CardUI cardPrefab;
        [SerializeField] private Button confirmButton;

        private List<CardUI> instantiatedCards = new();
        private CardInstance selectedCard;

        protected override void Awake()
        {
            base.Awake();

            if (cardContainer != null) cardContainer.gameObject.SetActive(false);
            if (cardPrefab != null) cardPrefab.gameObject.SetActive(false);

            if (confirmButton != null)
            {
                confirmButton.onClick.AddListener(ConfirmSelection);
                confirmButton.gameObject.SetActive(false);
            }
        }

        public void ShowDraft(List<CardInstance> draftOptions)
        {
            ClearPreviousCards();

            cardContainer.gameObject.SetActive(true);
            confirmButton.gameObject.SetActive(true);

            foreach (var instance in draftOptions)
            {
                var cardUI = Instantiate(cardPrefab, cardContainer);
                cardUI.Setup(instance);
                cardUI.gameObject.SetActive(true);
                instantiatedCards.Add(cardUI);
            }
        }

        public void OnCardSelected(CardUI selected)
        {
            selectedCard = selected.GetInstance();

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
                DraftManager.Instance.OnDraftChosen(selectedCard.cardData);

                ClearPreviousCards();
                cardContainer.gameObject.SetActive(false);
                confirmButton.gameObject.SetActive(false);

                DualMesh.Instance.SetMode(DualMesh.PlayingMode.Simulation);
            }
        }

        private void ClearPreviousCards()
        {
            foreach (var card in instantiatedCards)
            {
                Object.Destroy(card.gameObject);
            }

            instantiatedCards.Clear();
            selectedCard = null;
        }
    }
}