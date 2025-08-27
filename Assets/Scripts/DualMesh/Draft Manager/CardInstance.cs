using Data;
using UnityEngine;

namespace DraftSystem
{
    public class BuildCardInstance
    {
        public BuildCard cardData { get; private set; }

        public bool wasChosen = false;
        public int indexInDraft;

        public BuildCardInstance(BuildCard card, int index)
        {
            cardData = card;
            indexInDraft = index;
        }

        public string Name => cardData.cardName;
        public Sprite Icon => cardData.icon;
        public string Description => cardData.description;
        public int Cost => cardData.cost;
        public Rarity Rarity => cardData.rarity;
        public ConstructionType Type => cardData.constructionType;
    }
}