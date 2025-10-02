using UnityEngine;

namespace DraftSystem
{
    public class CardInstance
    {
        public Card cardData { get; private set; }

        public bool wasChosen = false;
        public int indexInDraft;

        public CardInstance(Card card, int index)
        {
            cardData = card;
            indexInDraft = index;
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