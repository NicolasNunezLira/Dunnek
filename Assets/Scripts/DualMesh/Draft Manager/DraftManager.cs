using Utils;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Data;

namespace DraftSystem
{
    public class DraftManager : Singleton<DraftManager>
    {
        #region Variables
        public DraftState currentState = DraftState.Idle;
        public List<BuildCard> unlockedCards = new();

        public List<BuildCard> allCards;
        public int cartToDraft = 3;

        [Header("Rariry configuration")]
        public AnimationCurve rarityDistribution;
        #endregion

        #region Awake
        protected override void Awake()
        {
            base.Awake();

            ConstructionUnlockerManager.Awake();

        }
        #endregion

        #region Start Drafting
        public void StartDraft()
        {
            if (currentState != DraftState.Idle) return;

            List<BuildCardInstance> draftOptions = GetRandomCardInstances(cartToDraft);
            ShowDraftUI(draftOptions);

            currentState = DraftState.Drafting;
        }

        List<BuildCardInstance> GetRandomCardInstances(int count)
        {
            List<BuildCardInstance> result = new();
            HashSet<BuildCard> used = new();

            var filteredPool = allCards
                .Where(c => !unlockedCards.Contains(c))
                .ToList();

            int maxCount = Mathf.Min(count, filteredPool.Count);
            int tries = 0;
            int maxTries = 100;

            while (result.Count < maxCount && tries < maxTries)
            {
                float roll = Random.value;
                Rarity chosenRarity = GetRarityFromRoll(roll);

                var possible = filteredPool
                    .Where(c => c.rarity == chosenRarity && !used.Contains(c))
                    .ToList();

                if (possible.Count > 0)
                {
                    var selected = possible[Random.Range(0, possible.Count)];
                    used.Add(selected);
                    result.Add(new BuildCardInstance(selected, result.Count));
                }

                tries++;
            }

            return result;
        }

        Rarity GetRarityFromRoll(float roll)
        {
            float curveVal = rarityDistribution.Evaluate(roll);

            if (curveVal < 0.25f) return Rarity.Common;
            if (curveVal < 0.5f) return Rarity.Uncommon;
            if (curveVal < 0.75f) return Rarity.Rare;
            return Rarity.Legendary;
        }

        void ShowDraftUI(List<BuildCardInstance> draftOptions)
        {
            List<BuildCard> cardsToShow = draftOptions.Select(instance => instance.cardData).ToList();
            DraftUI.Instance.ShowDraft(cardsToShow);
        }
        #endregion

        #region On Draft
        public void OnDraftChosen(BuildCard card)
        {
            unlockedCards.Add(card);
            UnlockConstruction(card.constructionType);
        }

        public void UnlockConstruction(ConstructionType type)
        {
            ConstructionUnlockerManager.UnlockConstruction(type);
        }
        #endregion
    }
}