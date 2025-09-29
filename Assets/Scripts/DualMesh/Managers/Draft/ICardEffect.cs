namespace DraftSystem
{
    public interface ICardEffect
    {
        void Apply();
    }

    [System.Serializable]
    public class UnlockConstructionEffect : ICardEffect
    {
        public string constructionType;

        public void Apply()
        {
            ConstructionUnlockerManager.UnlockConstruction(constructionType);
        }
    }

    [System.Serializable]
    public class GrantGlobalBonusEffect : ICardEffect
    {
        public ResourceSystem.Resource resource;
        public float multiplier;
        public BonusSystem.BonusTarget target;

        public void Apply()
        {
            var bonus = new BonusSystem.GlobalBonus(resource, multiplier, target, null);
            BonusSystem.BonusManager.AddBonus(bonus);
        }
    }
}