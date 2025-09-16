using System.Collections.Generic;
using ResourceSystem;

namespace BonusSystem
{
    public static class BonusManager
    {
        private static List<Bonus> activeBonuses = new List<Bonus>();

        public static void AddBonus(Bonus bonus)
        {
            activeBonuses.Add(bonus);
        }

        public static void RemoveBonus(Bonus bonus)
        {
            activeBonuses.Remove(bonus);
        }

        public static void ClearAllBonuses()
        {
            activeBonuses.Clear();
        }

        public static float ApplyBonuses(
            float baseValue,
            Resource resource,
            ResourceManager.Consumer consumer
        )
        {
            float result = baseValue;
            foreach (Bonus bonus in activeBonuses)
            {
                if (bonus.Resource == resource)
                {
                    result = bonus.Apply(result, consumer);
                }
            }
            return result;
        }

        public static void RegisterConsumerInLocalBonuses(ResourceManager.Consumer consumer)
        {
            foreach (var bonus in activeBonuses)
            {
                if (bonus is LocalBonus localBonus)
                {
                    localBonus.AddConsumerIfInRange(consumer);
                }
            }
        }
    }
}