using UnityEngine;

namespace DraftSystem
{
    [CreateAssetMenu(fileName = "RarityColors", menuName = "Draft/Rarity Colors")]
    public class RarityColorConfig : ScriptableObject
    {
        [System.Serializable]
        public struct RarityColor
        {
            public Rarity rarity;
            public Color color;
        }

        public RarityColor[] rarityColors;

        public Color GetColor(Rarity rarity)
        {
            foreach (var rc in rarityColors)
            {
                if (rc.rarity == rarity) return rc.color;
            }
            return Color.white; // fallback
        }
    }
}
