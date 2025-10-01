using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Utils;
using ResourceSystem;
using TMPro;

namespace DraftSystem
{
    public class DraftUI : Singleton<DraftUI>
    {
        #region - Variables
        [Header("Bonus button:")]
        [SerializeField] private Button bonusButton;
        [SerializeField] private ResourceDraftCost bonusCost;

        [Header("Unlock Button:")]
        [SerializeField] private Button unlockButton;
        [SerializeField] private ResourceDraftCost unlockCost;

        [Header("Cards panel:")]
        [SerializeField] private Transform cardContainer;
        [SerializeField] private CardUI cardPrefab;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button closeButton;

        private Dictionary<Button, Color> originalColors = new();
        private Dictionary<Button, Coroutine> flashRoutines = new();
        private float flashDuration = 1f;


        private List<CardUI> instantiatedCards = new();
        private CardInstance selectedCard;
        #endregion

        #region - Awake
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

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(CloseSelection);
                closeButton.gameObject.SetActive(false);
            }

            if (unlockButton != null)
            {
                unlockButton.onClick.AddListener(() => SetDraftMode(DraftMode.UnlockOnly, unlockCost));
                originalColors[unlockButton] = unlockButton.GetComponent<UnityEngine.UI.Image>().color;
                var resourceSlot = unlockButton.transform.Find("ResourceSlotTemplate");

                var sprite = resourceSlot.GetComponentInChildren<UnityEngine.UI.Image>();
                sprite.sprite = ResourceSystem.ResoureIconManager.Instance.GetIcon(unlockCost.resource);

                var text = resourceSlot.GetComponentInChildren<TextMeshProUGUI>();
                text.text = unlockCost.cost.ToString();
            }

            if (bonusButton != null)
            {
                bonusButton.onClick.AddListener(() => SetDraftMode(DraftMode.BonusOnly, bonusCost));
                originalColors[bonusButton] = bonusButton.GetComponent<UnityEngine.UI.Image>().color;

                var resourceSlot = bonusButton.transform.Find("ResourceSlotTemplate");

                var sprite = resourceSlot.GetComponentInChildren<UnityEngine.UI.Image>();
                sprite.sprite = ResourceSystem.ResoureIconManager.Instance.GetIcon(bonusCost.resource);

                var text = resourceSlot.GetComponentInChildren<TextMeshProUGUI>();
                text.text = bonusCost.cost.ToString();
            }
        }
        #endregion

        #region - Show Draft and selection
        public void ShowDraft(List<CardInstance> draftOptions)
        {
            ClearPreviousCards();

            cardContainer.gameObject.SetActive(true);
            confirmButton.gameObject.SetActive(true);
            closeButton.gameObject.SetActive(true);

            foreach (var instance in draftOptions)
            {
                var cardUI = Instantiate(cardPrefab, cardContainer);
                cardUI.Setup(instance);
                var outline = cardUI.GetComponent<Outline>();
                if (outline) outline.enabled = false;
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
                closeButton.gameObject.SetActive(false);

                DualMesh.Instance.SetMode(DualMesh.PlayingMode.Simulation);
            }
        }

        private void CloseSelection()
        {
            cardContainer.gameObject.SetActive(false);
            confirmButton.gameObject.SetActive(false);
            closeButton.gameObject.SetActive(false);

            DualMesh.Instance.SetMode(DualMesh.PlayingMode.Simulation);

            DraftManager.Instance.currentState = DraftState.Idle;
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

        private void SetDraftMode(DraftMode mode, ResourceDraftCost draftCost)
        {
            if (ResourceManager.TryConsumeResource(draftCost.resource, draftCost.cost))
            {
                DraftManager.Instance.SetMode(mode);
                DualMesh.Instance.SetMode(DualMesh.PlayingMode.Draft);
                return;
            }
            else
            {
                switch (mode)
                {
                    case DraftMode.BonusOnly:
                        FlashRed(bonusButton);
                        break;
                    case DraftMode.UnlockOnly:
                        FlashRed(unlockButton);
                        break;
                }
            }
        }
        #endregion

        #region - Flash red routine
        public void FlashRed(Button button)
        {
            if (flashRoutines.TryGetValue(button, out Coroutine running))
                StopCoroutine(running);

            flashRoutines[button] = StartCoroutine(FlashRoutine(button));
        }


        private IEnumerator FlashRoutine(Button button)
        {
            Image buttonImage = button.GetComponent<Image>();
            buttonImage.color = Color.red;

            float t = 0f;
            while (t < flashDuration)
            {
                t += Time.deltaTime;
                buttonImage.color = Color.Lerp(Color.red, originalColors[button], t / flashDuration);
                yield return null;
            }

            buttonImage.color = originalColors[button];
            flashRoutines.Remove(button);
        }
        #endregion
    }

    [System.Serializable]
    public struct ResourceDraftCost
    {
        public Resource resource;
        public float cost;
    }
}