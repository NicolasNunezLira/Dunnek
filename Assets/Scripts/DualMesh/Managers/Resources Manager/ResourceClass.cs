using System.Collections.Generic;
using System.Linq;
using BonusSystem;
using UnityEngine;

namespace ResourceSystem
{
    [System.Serializable]
    public enum Resource { Work, Sand }

    #region - ResourceClass
    [System.Serializable]
    public class ResourceClass
    {
        public Resource Name { get; private set; }
        public float Amount { get; private set; }

        // consumerId -> finalRate (ya con bonuses)
        private readonly Dictionary<int, float> producers = new();
        private readonly Dictionary<int, ProducerInfo> producersDetailed = new();

        public float Rate => producers.Values.Sum();

        public ResourceClass(Resource name, float initialAmount = 0f)
        {
            Name = name;
            Amount = initialAmount;
        }

        #region - ProducerInfo
        public class ProducerInfo
        {
            public int ConsumerId;
            public string ConsumerCodeName;
            public float BaseRate;
            public float FinalRate;
            public float GlobalContribution;
            public float LocalContribution;
            public float GlobalMultiplier;
            public float LocalMultiplier;
            public List<BonusAppliedInfo> AppliedBonuses = new();
        }
        #endregion

        #region - Update from Consumers
        /// <summary>
        /// Recalcula todos los productores activos, aplicando bonuses según tipo (Production / Consumption)
        /// </summary>
        public void UpdateFromConsumers(Dictionary<int, ResourceManager.ResourceBuilding> consumers)
        {
            producers.Clear();
            producersDetailed.Clear();

            foreach (var consumer in consumers.Values)
            {
                if (!consumer.isOperative) continue;

                if (!consumer.rates.TryGetValue(Name, out float baseRate)) continue;
                if (Mathf.Approximately(baseRate, 0f)) continue;

                BonusTarget target = baseRate > 0 ? BonusTarget.Production : BonusTarget.Consumption;

                var detail = BonusManager.ApplyBonusesDetailed(baseRate, Name, consumer);

                producers[consumer.id] = detail.FinalValue;

                float globalApplied = detail.AppliedBonuses
                    .Where(b => b.OriginType == BonusOriginType.Global)
                    .Sum(b => b.AppliedAmount);
                float localApplied = detail.AppliedBonuses
                    .Where(b => b.OriginType == BonusOriginType.Local)
                    .Sum(b => b.AppliedAmount);

                producersDetailed[consumer.id] = new ProducerInfo
                {
                    ConsumerId = consumer.id,
                    ConsumerCodeName = consumer.codeName,
                    BaseRate = baseRate,
                    FinalRate = detail.FinalValue,
                    GlobalMultiplier = detail.GlobalMultiplier,
                    LocalMultiplier = detail.LocalMultiplier,
                    GlobalContribution = globalApplied,
                    LocalContribution = localApplied,
                    AppliedBonuses = detail.AppliedBonuses
                };
            }
        }
        #endregion

        #region - Update Amount
        public void UpdateAmount()
        {
            Amount += Rate;
        }

        public void Add(float amount) => Amount += amount;
        public bool TryConsume(float amount)
        {
            if (Amount >= amount)
            {
                Amount -= amount;
                return true;
            }
            return false;
        }
        #endregion

        #region - Queries
        public IEnumerable<ProducerInfo> GetAllProducersDetailed() => producersDetailed.Values;
        public float GetBaseRateSum() => producersDetailed.Values.Sum(p => p.BaseRate);
        public float GetGlobalContributionSum() => producersDetailed.Values.Sum(p => p.GlobalContribution);
        public float GetLocalContributionSum() => producersDetailed.Values.Sum(p => p.LocalContribution);
        public bool TryGetProducerInfo(int consumerId, out ProducerInfo info) => producersDetailed.TryGetValue(consumerId, out info);
        #endregion
    }
    #endregion
    /*
    [System.Serializable]
    public enum Resource { Work, Sand }

    [System.Serializable]
    public class ResourceClass
    {
        public Resource Name { get; private set; }
        public float Amount { get; private set; }

        // producers: consumerId -> finalRate (con bonuses ya aplicados)
        public Dictionary<int, float> producers = new Dictionary<int, float>();

        // Detailed info por productor (consumer)
        public class ProducerInfo
        {
            public int ConsumerId;
            public string ConsumerCodeName;
            public float BaseRate;            // rate sin bonuses
            public float FinalRate;           // rate después de aplicar bonuses
            public float GlobalContribution;  // cantidad extra/producida por global bonuses (aplicada)
            public float LocalContribution;   // cantidad extra/producida por local bonuses (aplicada)
            public float GlobalMultiplier;    // producto de global multipliers
            public float LocalMultiplier;     // producto de local multipliers
            public List<BonusAppliedInfo> AppliedBonuses = new List<BonusAppliedInfo>();
        }

        // Map consumerId -> ProducerInfo
        private Dictionary<int, ProducerInfo> producersDetailed = new Dictionary<int, ProducerInfo>();

        public float Rate => producers.Values.Sum(); // final neto (suma de finalRates)

        public ResourceClass(Resource name, float initialAmount = 0f)
        {
            Name = name;
            Amount = initialAmount;
        }

        /// <summary>
        /// Recalcula producers y producersDetailed usando la lista de consumers activos.
        /// Llama a BonusManager.ApplyBonusesDetailed para obtener el breakdown.
        /// </summary>
        public void UpdateFromConsumers(Dictionary<int, ResourceManager.ResourceBuilding> consumers)
        {
            producers.Clear();
            producersDetailed.Clear();

            foreach (var consumer in consumers.Values)
            {
                if (!consumer.isOperative) continue;

                // busca rate base del consumer para este recurso
                if (!consumer.rates.TryGetValue(Name, out float baseRate)) continue;
                if (Mathf.Approximately(baseRate, 0f)) continue;

                // pedimos detalle de bonuses
                var detail = BonusManager.ApplyBonusesDetailed(baseRate, Name, consumer);

                // guardamos final y breakdown
                producers[consumer.id] = detail.FinalValue;

                var pi = new ProducerInfo
                {
                    ConsumerId = consumer.id,
                    ConsumerCodeName = consumer.codeName,
                    BaseRate = baseRate,
                    FinalRate = detail.FinalValue,
                    GlobalMultiplier = detail.GlobalMultiplier,
                    LocalMultiplier = detail.LocalMultiplier,
                    GlobalContribution = 0f,
                    LocalContribution = 0f
                };

                // calcular contribuciones (informativo)
                float globalApplied = 0f;
                float localApplied = 0f;
                foreach (var b in detail.AppliedBonuses)
                {
                    if (b.OriginType == BonusOriginType.Global)
                        globalApplied += b.AppliedAmount;
                    else
                        localApplied += b.AppliedAmount;
                }

                pi.GlobalContribution = globalApplied;
                pi.LocalContribution = localApplied;
                pi.AppliedBonuses = detail.AppliedBonuses;

                producersDetailed[consumer.id] = pi;
            }
        }

        /// <summary>
        /// Aplica la tasa neta al recurso (suma Rate).
        /// </summary>
        public void UpdateAmount()
        {
            Amount += Rate;
        }

        // Métodos de consulta útiles para la UI

        public float GetBaseRateSum()
        {
            return producersDetailed.Values.Sum(p => p.BaseRate);
        }

        public float GetGlobalContributionSum()
        {
            return producersDetailed.Values.Sum(p => p.GlobalContribution);
        }

        public float GetLocalContributionSum()
        {
            return producersDetailed.Values.Sum(p => p.LocalContribution);
        }

        public IEnumerable<ProducerInfo> GetAllProducersDetailed() => producersDetailed.Values;

        public bool TryGetProducerInfo(int consumerId, out ProducerInfo info)
        {
            return producersDetailed.TryGetValue(consumerId, out info);
        }

        // Operaciones sobre Amount
        public void Add(float amount)
        {
            Amount += amount;
        }

        public bool TryConsume(float amount)
        {
            if (Amount >= amount)
            {
                Amount -= amount;
                return true;
            }
            return false;
        }
    }
    */
}
