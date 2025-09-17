// BonusSystem/BonusApplyResult.cs
using System.Collections.Generic;
using UnityEngine;
using ResourceSystem;

namespace BonusSystem
{
    public enum BonusOriginType { Global, Local }

    public class BonusAppliedInfo
    {
        public BonusOriginType OriginType;
        public int? ProviderId;          // id de la construcción que creó el bonus (si lo conocemos)
        public string ProviderName;      // opcional: código o nombre
        public float Multiplier;         // p. ej. 1.1f para +10%
        public float AppliedAmount;      // qué cantidad aporta (informativo)
        public string Description;       // texto legible del bonus (ej. "+10% Work")
    }

    public class BonusApplicationResult
    {
        public float FinalValue;               // base * globalMult * localMult
        public float GlobalMultiplier = 1f;    // producto de globales aplicadas
        public float LocalMultiplier = 1f;     // producto de locales aplicadas
        public List<BonusAppliedInfo> AppliedBonuses = new List<BonusAppliedInfo>();
    }
}
