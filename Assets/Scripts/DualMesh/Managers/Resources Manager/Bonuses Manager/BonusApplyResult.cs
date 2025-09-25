using System.Collections.Generic;

namespace BonusSystem
{
    public enum BonusOriginType { Global, Local }

    public class BonusAppliedInfo
    {
        public BonusOriginType OriginType;
        public BonusTarget Target;
        public int? ProviderId;
        public string ProviderName;
        public int? Radius;
        public float Multiplier;
        public float AppliedAmount;
        public string Description;
    }

    public class BonusApplicationResult
    {
        public float FinalValue;
        public float GlobalMultiplier = 1f;
        public float LocalMultiplier = 1f;
        public int? Radius;
        public List<BonusAppliedInfo> AppliedBonuses = new List<BonusAppliedInfo>();
    }
}
