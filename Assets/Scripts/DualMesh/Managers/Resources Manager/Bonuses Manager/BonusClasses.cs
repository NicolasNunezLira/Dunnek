using UnityEngine;
using ResourceSystem;
using System.Collections.Generic;
using Vector2 = UnityEngine.Vector2;
using ConstructionSystem;

namespace BonusSystem
{
    #region - Enums
    public enum BonusType { Local, Global, Resource }
    public enum BonusTarget { Production, Consumption, Cost, Resource }
    #endregion

    #region - Abstract Bonus
    public abstract class Bonus
    {
        public ConstructionInstance Building { get; protected set; }
        public List<string> AffectedBuildType { get; private set; }
        public BonusType Type { get; protected set; }
        public BonusTarget Target { get; protected set; }
        public Resource Resource { get; protected set; }
        public float Multiplier { get; protected set; }

        protected Bonus(
            Resource resource,
            float multiplier,
            BonusTarget target,
            List<string> affectedBuildType,
            ConstructionInstance building = null
        )
        {
            Resource = resource;
            Multiplier = multiplier;
            Target = target;
            AffectedBuildType = affectedBuildType;
            Building = building;
        }

        /// <summary>
        /// Aplica el bonus solo si corresponde al signo (positivo/negativo) y al Target.
        /// </summary>
        public abstract float Apply(float baseValue, ConstructionInstance consumer = null);

        /// <summary>
        /// Para bonuses de tipo Resource. Por defecto no hace nada.
        /// </summary>
        public virtual void ApplyResource() { }
        
        protected bool ShouldAffect(float baseValue)
        {
            return (Target == BonusTarget.Production && baseValue > 0f) ||
                   (Target == BonusTarget.Consumption && baseValue < 0f) ||
                   (Target == BonusTarget.Cost);
        }
    }
    #endregion

    #region - Global Bonus
    public class GlobalBonus : Bonus
    {
        public GlobalBonus(
            Resource resource,
            float multiplier,
            BonusTarget target,
            List<string> affectedBuildTypes,
            ConstructionInstance building)
            : base(resource, multiplier, target, affectedBuildTypes, building)
        {
            Type = BonusType.Global;
        }

        public override float Apply(float baseValue, ConstructionInstance consumer = null)
        {
            if (!(ShouldAffect(baseValue) && AffectedBuildType.Contains(consumer.Config.codeName))) return baseValue;
            return baseValue * Multiplier;
        }
    }
    #endregion

    #region - Local Bonus
    public class LocalBonus : Bonus, IProviderInfo
    {
        public int? ProviderId { get; private set; }
        public string ProviderName { get; private set; }
        public ConstructionInstance ProviderBuilding { get; private set; }

        private Vector2Int sourcePos;
        public float Radius { get; private set; }

        private readonly List<ConstructionInstance> affectedBuildings = new();

        public LocalBonus(
            Resource resource,
            float multiplier,
            BonusTarget target,
            List<string> affectedBuildType,
            ConstructionInstance building,
            Vector2Int source,
            int radius)
            : base(resource, multiplier, target, affectedBuildType, building)
        {
            Type = BonusType.Local;
            sourcePos = source;
            this.Radius = radius * DualMesh.Instance.tileSize;

            ProviderId = building.id;
            ProviderName = building.Obj.name;
            ProviderBuilding = building;
        }

        public void AddBuildingIfInRange(ConstructionInstance building)
        {
            Vector2 pos = new Vector2(building.Position.x, building.Position.z);
            if (IsWithinRadius(sourcePos, pos, Radius) && !affectedBuildings.Contains(building))
            {
                if (DoesBonusAffect(building))
                {
                    affectedBuildings.Add(building);
                    building.AddAffectingBonus(this);
                }
            }
        }

        private bool DoesBonusAffect(ConstructionInstance building)
        {
            bool res = true;

            foreach ((Resource resource, float rate) in building.Config.rate)
            {
                if (Target == BonusTarget.Production && resource == this.Resource && rate <= 0)
                {
                    res = false; break;
                }
                
                if (Target == BonusTarget.Consumption && resource == this.Resource && rate >= 0)
                {
                    res = false; break;
                }
            }

            return res;
        }

        public void RemoveBuilding(ConstructionInstance building)
        {
            affectedBuildings.Remove(building);
        }

        public bool AffectsBuilding(ConstructionInstance building)
        {
            Vector2 pos = new Vector2(building.Position.x, building.Position.z);
            return IsWithinRadius(sourcePos, pos, Radius);
        }

        public override float Apply(float baseValue, ConstructionInstance building)
        {
            if (!(ShouldAffect(baseValue) && AffectedBuildType.Contains(building.Config.codeName))) return baseValue;
            if (AffectsBuilding(building))
                return baseValue * Multiplier;

            return baseValue;
        }

        public void RecalculateAffectedBuildings()
        {
            affectedBuildings.Clear();
            var allBuildings = DualMesh.Instance.builder.constructions.Values;
            foreach (ConstructionInstance building in allBuildings)
            {
                if (building.Category == ConstructionCategory.Wall || building.Category == ConstructionCategory.BonusProvider) continue;

                Vector2 pos = new Vector2(building.Position.x, building.Position.z);
                if (IsWithinRadius(sourcePos, pos, Radius))
                {
                    affectedBuildings.Add(building);
                }
            }
        }

        private bool IsWithinRadius(Vector2 source, Vector2 target, float radius)
        {
            return (source - target).sqrMagnitude <= radius * radius;
        }
    }
    #endregion

    #region - Resource amount Bonus
    public class ResourceBonus : Bonus
    {
        private float amount;
        private bool appliedOnce = false;

        public ResourceBonus(Resource resource, float amount)
            : base(resource, amount, BonusTarget.Resource, null, null)
        {
            Type = BonusType.Resource;
            this.amount = amount;
        }

        // No usado en este tipo de bonus
        public override float Apply(float baseValue, ConstructionInstance consumer = null)
        {
            return baseValue;
        }

        public override void ApplyResource()
        {
            if (appliedOnce) return;

            ResourceManager.AddResource(Resource, amount);
            appliedOnce = true;
        }
    }
    #endregion

    #region - Provider Interface
    public interface IProviderInfo
    {
        int? ProviderId { get; }
        string ProviderName { get; }
        ConstructionInstance ProviderBuilding { get; }
    }    
    #endregion
}