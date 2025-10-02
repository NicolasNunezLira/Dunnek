using System.Collections.Generic;
using BonusSystem;
using ResourceSystem;

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

        public UnlockConstructionEffect(string constructionType)
        {
            this.constructionType = constructionType;
        }

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
        public List<string> affectedBuildTypes;

        public GrantGlobalBonusEffect(
            ResourceSystem.Resource resource,
            float multiplier,
            BonusSystem.BonusTarget target,
            List<string> affectedBuildTypes
        )
        {
            this.resource = resource;
            this.multiplier = multiplier;
            this.target = target;
            this.affectedBuildTypes = affectedBuildTypes;
        }

        public void Apply()
        {
            var bonus = new BonusSystem.GlobalBonus(resource, multiplier, target, affectedBuildTypes, null);
            BonusSystem.BonusManager.AddBonus(bonus);
        }
    }

    [System.Serializable]
    public class GrantResourceBonusEffect : ICardEffect
    {
        public Resource resource;
        public float amount;

        public GrantResourceBonusEffect(Resource resource, float amount)
        {
            this.resource = resource;
            this.amount = amount;
        }

        public void Apply()
        {
            var bonus = new ResourceBonus(resource, amount);
            BonusSystem.BonusManager.AddBonus(bonus);
        }
    }
}