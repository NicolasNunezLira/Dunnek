using System.Collections.Generic;
using UnityEngine;
using ResourceSystem;

namespace DraftSystem
{
    [CreateAssetMenu(fileName = "NewCard", menuName = "Draft/Card")]
    public class Card : ScriptableObject
    {
        public string cardName;
        public Sprite icon;
        public Rarity rarity;
        [TextArea] public string description;
        public Resource resource;
        public float cost;
        [SerializeReference] public List<ICardEffect> effects;
    }
}