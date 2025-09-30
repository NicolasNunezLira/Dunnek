using System.Linq;
using ResourceSystem;
using UnityEngine;

namespace DraftSystem
{
    public class CardInstance
    {
        public Card cardData { get; private set; }

        public bool wasChosen = false;
        public int indexInDraft;

        public bool showCost { get; private set; }

        public CardInstance(Card card, int index)
        {
            cardData = card;
            indexInDraft = index;
        }

        public void ShowCost()
        {
            showCost = true;
        }

        public void HideCost()
        {
            showCost = false;
        }

        public string Name => cardData.cardName;
        public Sprite Icon => cardData.icon;
        public string Description => cardData.description;
        public Rarity Rarity => cardData.rarity;

        public void ApplyEffects()
        {
            foreach (var effect in cardData.effects)
            {
                effect.Apply();
            }
            wasChosen = true;
        }
    }
}