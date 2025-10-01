using System.Collections.Generic;
using UnityEngine;

namespace DraftSystem
{
    [CreateAssetMenu(fileName = "NewCard", menuName = "Draft/Card")]
    public class Card : ScriptableObject
    {
        public string cardName;
        public Sprite icon;
        public Rarity rarity;
        [TextArea] public string description;
        [SerializeReference] public List<ICardEffect> effects;
    }
}