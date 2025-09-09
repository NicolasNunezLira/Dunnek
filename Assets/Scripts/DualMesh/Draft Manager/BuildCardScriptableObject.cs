using ConstructionSystem;
using UnityEngine;

namespace DraftSystem
{
    [CreateAssetMenu(fileName = "NewBuildCard", menuName = "Draft/BuildCard")]
    public class BuildCard : ScriptableObject
    {
        public string cardName;
        public Sprite icon;
        public string constructionType;
        public Rarity rarity;
        [TextArea] public string description;
        public int cost;
    }
}