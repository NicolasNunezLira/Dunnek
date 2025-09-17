using System.Collections.Generic;
using ResourceSystem;

namespace BonusSystem
{
    public static class BonusManager
    {
        private static readonly List<GlobalBonus> globalBonuses = new();
        private static readonly List<LocalBonus> localBonuses = new();

        #region Management
        public static void AddBonus(Bonus bonus)
        {
            if (bonus is GlobalBonus global)
            {
                globalBonuses.Add(global);
            }
            else if (bonus is LocalBonus local)
            {
                localBonuses.Add(local);

                // Al añadir un bonus local, registrar consumidores ya existentes
                foreach (var consumer in ResourceManager.AllConsumers.Values)
                {
                    local.AddConsumerIfInRange(consumer);
                }
            }
        }

        public static void RemoveBonus(Bonus bonus)
        {
            if (bonus is GlobalBonus global)
            {
                globalBonuses.Remove(global);
            }
            else if (bonus is LocalBonus local)
            {
                localBonuses.Remove(local);
            }
        }

        public static void ClearAllBonuses()
        {
            globalBonuses.Clear();
            localBonuses.Clear();
        }
        #endregion

        #region Application
        public static float ApplyBonuses(
            float baseValue,
            Resource resource,
            ResourceManager.Consumer consumer
        )
        {
            float result = baseValue;

            // Aplicar globales
            foreach (var global in globalBonuses)
            {
                if (global.Resource == resource)
                {
                    result = global.Apply(result, consumer);
                }
            }

            // Aplicar locales
            foreach (var local in localBonuses)
            {
                if (local.Resource == resource)
                {
                    result = local.Apply(result, consumer);
                }
            }

            return result;
        }

        // Nuevo: devuelve detalle de aplicación de bonuses
        public static BonusApplicationResult ApplyBonusesDetailed(float baseValue, Resource resource, ResourceManager.Consumer consumer)
        {
            var result = new BonusApplicationResult();
            result.FinalValue = baseValue;

            // Aplicar globales: multiplicativo
            foreach (var g in globalBonuses)
            {
                if (g.Resource != resource) continue;
                // asumimos g.Multiplier está en forma multiplicativa (1.1 para +10%)
                result.GlobalMultiplier *= g.Multiplier;
                result.AppliedBonuses.Add(new BonusAppliedInfo
                {
                    OriginType = BonusOriginType.Global,
                    ProviderId = (g as IProviderInfo)?.ProviderId, // ver nota abajo
                    ProviderName = (g as IProviderInfo)?.ProviderName,
                    Multiplier = g.Multiplier,
                    Description = $"Global: x{g.Multiplier}"
                });
            }

            // Aplicar locales: solo si el local realmente afecta al consumer
            foreach (var l in localBonuses)
            {
                if (l.Resource != resource) continue;
                // Comprobamos si el local afecta al consumer (método público en LocalBonus)
                if (!l.AffectsConsumer(consumer)) continue;

                result.LocalMultiplier *= l.Multiplier;
                result.AppliedBonuses.Add(new BonusAppliedInfo
                {
                    OriginType = BonusOriginType.Local,
                    ProviderId = (l as IProviderInfo)?.ProviderId,
                    ProviderName = (l as IProviderInfo)?.ProviderName,
                    Multiplier = l.Multiplier,
                    Description = $"Local: x{l.Multiplier} (radius {l.Radius})"
                });
            }

            // Final
            result.FinalValue = baseValue * result.GlobalMultiplier * result.LocalMultiplier;

            // Para cada applied bonus también calculamos su "applied amount" informativo:
            foreach (var b in result.AppliedBonuses)
            {
                // applied amount = efecto marginal sobre la base, aproximado:
                // Si es global: base * (multiplier - 1)
                // If local: base * globalMultiplier * (localMultiplierPiece - 1)
                if (b.OriginType == BonusOriginType.Global)
                    b.AppliedAmount = baseValue * (b.Multiplier - 1f);
                else
                    b.AppliedAmount = baseValue * result.GlobalMultiplier * (b.Multiplier - 1f);
            }

            return result;
        }
        #endregion

        #region - Consumers
        public static void RegisterConsumerInLocalBonuses(ResourceManager.Consumer consumer)
        {
            foreach (var local in localBonuses)
            {
                local.AddConsumerIfInRange(consumer);
            }
        }

        public static void UnregisterConsumerFromLocalBonuses(ResourceManager.Consumer consumer)
        {
            foreach (var local in localBonuses)
            {
                local.RemoveConsumer(consumer);
            }
        }
        #endregion

        #region Debug/Tooltip
        public static IEnumerable<GlobalBonus> GetGlobalBonuses() => globalBonuses;
        public static IEnumerable<LocalBonus> GetLocalBonuses() => localBonuses;
        #endregion
    }

    public interface IProviderInfo
    {
        int? ProviderId { get; }
        string ProviderName { get; }
    }
}
