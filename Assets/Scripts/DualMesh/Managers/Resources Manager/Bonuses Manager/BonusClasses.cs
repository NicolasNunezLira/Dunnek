using UnityEngine;
using ResourceSystem;
using System.Collections.Generic;
using Vector2 = UnityEngine.Vector2;
using ConstructionSystem;

namespace BonusSystem
{
    #region - Enums
    public enum BonusType { Local, Global }
    public enum BonusTarget { Production, Consumption, Cost }
    #endregion

    #region - Abstract Bonus
    public abstract class Bonus
    {
        public ConstructionInstance Building { get; protected set; }
        public BonusType Type { get; protected set; }
        public BonusTarget Target { get; protected set; }
        public Resource Resource { get; protected set; }
        public float Multiplier { get; protected set; }

        protected Bonus(
            Resource resource,
            float multiplier,
            BonusTarget target,
            ConstructionInstance building
        )
        {
            Resource = resource;
            Multiplier = multiplier;
            Target = target;
            Building = building;
        }

        public abstract float Apply(
            float baseValue, ConstructionInstance consumer
        );
    }
    #endregion

    #region - Global Bonus
    public class GlobalBonus : Bonus
    {
        public GlobalBonus(Resource resource, float multiplier, BonusTarget target, ConstructionInstance building)
            : base(resource, multiplier, target, building)
        {
            Type = BonusType.Global;
        }

        public override float Apply(float baseValue, ConstructionInstance consumer)
        {
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

        public LocalBonus(Resource resource, float multiplier, BonusTarget target, ConstructionInstance building, Vector2Int source, int radius)
            : base(resource, multiplier, target, building)
        {
            Type = BonusType.Local;
            sourcePos = source;
            this.Radius = radius * DualMesh.Instance.tileSize;

            ProviderId = building.id;
            ProviderName = building.Obj.name;
            ProviderBuilding = building;
        }

        /*
        public void AddConsumerIfInRange(ResourceManager.Consumer consumer)
        {
            Vector2 pos = new Vector2(consumer.instance.Position.x, consumer.instance.Position.z);
            if (IsWithinRadius(sourcePos, pos, Radius) && !affectedBuildings.Contains(consumer))
            {
                affectedBuildings.Add(consumer);
            }
        }
        */
        public void AddBuildingIfInRange(ConstructionInstance building)
        {
            Vector2 pos = new Vector2(building.Position.x, building.Position.z);
            if (IsWithinRadius(sourcePos, pos, Radius) && !affectedBuildings.Contains(building))
            {
                affectedBuildings.Add(building);
                building.AddAffectingBonus(this);
            }
        }

        /*
        public void RemoveConsumer(ResourceManager.Consumer consumer)
        {
            affectedBuildings.Remove(consumer);
        }
        */
        public void RemoveBuilding(ConstructionInstance building)
        {
            affectedBuildings.Remove(building);
        }

        /*
        public bool AffectsConsumer(ResourceManager.Consumer consumer)
        {
            Vector2 pos = new Vector2(consumer.instance.Position.x, consumer.instance.Position.z);
            return IsWithinRadius(sourcePos, pos, Radius);
        }
        */
        public bool AffectsBuilding(ConstructionInstance building)
        {
            Vector2 pos = new Vector2(building.Position.x, building.Position.z);
            return IsWithinRadius(sourcePos, pos, Radius);
        }

        /*
        public override float Apply(float baseValue, ResourceManager.Consumer consumer)
        {
            if (AffectsConsumer(consumer))
                return baseValue * Multiplier;

            return baseValue;
        }
        */
        public override float Apply(float baseValue, ConstructionInstance building)
        {
            if (AffectsBuilding(building))
                return baseValue * Multiplier;

            return baseValue;
        }

        /*
        public void RecalculateAffectedBuildings(IEnumerable<ResourceManager.Consumer> allConsumers)
        {
            affectedBuildings.Clear();
            foreach (var consumer in allConsumers)
            {
                Vector2 pos = new Vector2(consumer.instance.Position.x, consumer.instance.Position.z);
                if (IsWithinRadius(sourcePos, pos, Radius))
                {
                    affectedBuildings.Add(consumer);
                }
            }
        }
        */
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

    #region - Provider Interface
    public interface IProviderInfo
    {
        int? ProviderId { get; }
        string ProviderName { get; }
        ConstructionInstance ProviderBuilding { get; }
    }    
    #endregion
}