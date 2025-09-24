using System.Collections.Generic;
using ResourceSystem;
using UnityEngine;

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
                foreach (var building in ResourceManager.AllBuildings.Values)
                {
                    local.AddBuildingIfInRange(building.instance);
                    building.AddLocalBonus(local);
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

        #region - Application
        /// <summary>
        /// Versión simplificada: aplica todos los bonus aditivamente.
        /// </summary>
        public static float ApplyBonuses(
            float baseValue,
            Resource resource,
            ResourceManager.ResourceBuilding building
        )
        {
            float totalPct = 0f;

            // Globales
            foreach (var global in globalBonuses)
            {
                if (global.Resource == resource)
                    totalPct += global.Multiplier - 1f; // convertir multiplicador en delta porcentual
            }

            // Locales
            foreach (var local in localBonuses)
            {
                if (local.Resource == resource && local.AffectsBuilding(building.instance))
                    totalPct += local.Multiplier - 1f;
            }

            return baseValue * (1f + totalPct);
        }

        /// <summary>
        /// Versión detallada que devuelve desglose de bonus aplicados.
        /// </summary>
        public static BonusApplicationResult ApplyBonusesDetailed(
            float baseValue,
            Resource resource,
            ResourceManager.ResourceBuilding building
        )
        {
            var result = new BonusApplicationResult();
            result.FinalValue = baseValue;

            float totalPct = 0f;

            // Globales
            foreach (var g in globalBonuses)
            {
                if (g.Resource != resource) continue;

                float delta = g.Multiplier - 1f;
                totalPct += delta;

                var info = new BonusAppliedInfo
                {
                    OriginType = BonusOriginType.Global,
                    ProviderId = (g as IProviderInfo)?.ProviderId,
                    ProviderName = (g as IProviderInfo)?.ProviderName,
                    Multiplier = g.Multiplier,
                    AppliedAmount = baseValue * delta,
                    Description = $"Global: +{delta * 100f:0.#}%"
                };
                result.AppliedBonuses.Add(info);
            }

            // Locales
            foreach (var l in localBonuses)
            {
                if (l.Resource != resource) continue;
                if (!l.AffectsBuilding(building.instance)) continue;

                float delta = l.Multiplier - 1f;
                totalPct += delta;

                var info = new BonusAppliedInfo
                {
                    OriginType = BonusOriginType.Local,
                    ProviderId = (l as IProviderInfo)?.ProviderId,
                    ProviderName = (l as IProviderInfo)?.ProviderName,
                    Multiplier = l.Multiplier,
                    Radius = (int?)l.Radius,
                    AppliedAmount = baseValue * delta,
                    Description = $"Local: +{delta * 100f:0.#}%" //(radius {l.Radius})"
                };
                result.Radius = (int?)l.Radius;
                result.AppliedBonuses.Add(info);
            }

            // Valor final = base * (1 + suma de porcentajes)
            result.FinalValue = baseValue * (1f + totalPct);

            // Guardar acumulados para referencia
            result.GlobalMultiplier = 1f + SumGlobalPct(resource);
            result.LocalMultiplier = 1f + SumLocalPct(resource, building);

            return result;
        }

        private static float SumGlobalPct(Resource resource)
        {
            float pct = 0f;
            foreach (var g in globalBonuses)
                if (g.Resource == resource)
                    pct += g.Multiplier - 1f;
            return pct;
        }

        private static float SumLocalPct(Resource resource, ResourceManager.ResourceBuilding building)
        {
            float pct = 0f;
            foreach (var l in localBonuses)
                if (l.Resource == resource && l.AffectsBuilding(building.instance))
                    pct += l.Multiplier - 1f;
            return pct;
        }
        #endregion

        #region - Consumers
        public static void RegisterConsumerInLocalBonuses(ResourceManager.ResourceBuilding building)
        {
            foreach (var local in localBonuses)
            {
                local.AddBuildingIfInRange(building.instance);
            }
        }

        public static void UnregisterConsumerFromLocalBonuses(ResourceManager.ResourceBuilding building)
        {
            foreach (var local in localBonuses)
            {
                local.RemoveBuilding(building.instance);
            }
        }

        public static void RemoveLocalBonusesByProvider(GameObject providerObj)
        {
            localBonuses.RemoveAll(lb => lb.Building.Obj == providerObj);
        }
        #endregion

        #region - Debug/Tooltip
        public static IEnumerable<GlobalBonus> GetGlobalBonuses() => globalBonuses;
        public static IEnumerable<LocalBonus> GetLocalBonuses() => localBonuses;
        #endregion
    }
}
