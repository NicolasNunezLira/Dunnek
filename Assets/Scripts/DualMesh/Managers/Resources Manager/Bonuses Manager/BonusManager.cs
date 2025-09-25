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
        public static float ApplyBonuses(
            float baseValue,
            Resource resource,
            ResourceManager.ResourceBuilding building
        )
        {
            float result = baseValue;

            // Globales
            foreach (var global in globalBonuses)
            {
                if (global.Resource != resource) continue;
                result = global.Apply(result, building.instance);
            }

            // Locales
            foreach (var local in localBonuses)
            {
                if (local.Resource != resource) continue;
                result = local.Apply(result, building.instance);
            }

            return result;
        }

        public static BonusApplicationResult ApplyBonusesDetailed(
            float baseValue,
            Resource resource,
            ResourceManager.ResourceBuilding building
        )
        {
            var result = new BonusApplicationResult();
            result.FinalValue = baseValue;

            float runningValue = baseValue;

            // Globales
            foreach (var g in globalBonuses)
            {
                if (g.Resource != resource) continue;
                if (!((g.Target == BonusTarget.Production && baseValue > 0) ||
                      (g.Target == BonusTarget.Consumption && baseValue < 0)))
                    continue;

                float before = runningValue;
                runningValue = g.Apply(runningValue, building.instance);
                float applied = runningValue - before;

                result.GlobalMultiplier *= g.Multiplier;
                result.AppliedBonuses.Add(new BonusAppliedInfo
                {
                    OriginType = BonusOriginType.Global,
                    Target = g.Target,
                    ProviderId = (g as IProviderInfo)?.ProviderId,
                    ProviderName = (g as IProviderInfo)?.ProviderName,
                    Multiplier = g.Multiplier,
                    AppliedAmount = applied,
                    Description = $"Global {g.Target}: {(g.Multiplier - 1f) * 100f:+0.#;-0.#}%"
                });
            }

            // Locales
            foreach (var l in localBonuses)
            {
                if (l.Resource != resource) continue;
                if (!l.AffectsBuilding(building.instance)) continue;
                if (!((l.Target == BonusTarget.Production && baseValue > 0) ||
                      (l.Target == BonusTarget.Consumption && baseValue < 0)))
                    continue;

                float before = runningValue;
                runningValue = l.Apply(runningValue, building.instance);
                float applied = runningValue - before;

                result.LocalMultiplier *= l.Multiplier;
                result.AppliedBonuses.Add(new BonusAppliedInfo
                {
                    OriginType = BonusOriginType.Local,
                    Target = l.Target,
                    ProviderId = (l as IProviderInfo)?.ProviderId,
                    ProviderName = (l as IProviderInfo)?.ProviderName,
                    Multiplier = l.Multiplier,
                    Radius = (int?)l.Radius,
                    AppliedAmount = applied,
                    Description = $"Local {l.Target}: {(l.Multiplier - 1f) * 100f:+0.#;-0.#}%"
                });
                result.Radius = (int?)l.Radius;
            }

            result.FinalValue = runningValue;
            return result;
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
            var toRemove = localBonuses.FindAll(lb => lb.Building.Obj == providerObj);

            foreach (var local in toRemove)
            {
                foreach (var building in ResourceManager.AllBuildings.Values)
                {
                    if (local.AffectsBuilding(building.instance))
                    {
                        building.RemoveLocalBonus(local);
                    }
                }

                localBonuses.Remove(local);
            }
        }

        public static void RemoveBonusesByProvider(GameObject providerObj)
        {
            localBonuses.RemoveAll(lb => lb.Building.Obj == providerObj);

            globalBonuses.RemoveAll(gb => gb.Building.Obj == providerObj);
        }

        #endregion

        #region - Debug/Tooltip
        public static IEnumerable<GlobalBonus> GetGlobalBonuses() => globalBonuses;
        public static IEnumerable<LocalBonus> GetLocalBonuses() => localBonuses;
        #endregion
    }
}
