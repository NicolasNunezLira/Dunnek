using UnityEngine;
using ResourceSystem;
using System.Collections.Generic;
using Vector2 = UnityEngine.Vector2;

namespace BonusSystem
{
    #region - Enums
    public enum BonusType { Local, Global }
    public enum BonusTarget { Production, Consumption, Cost }
    #endregion

    #region - Abstract Bonus
    public abstract class Bonus
    {
        public BonusType Type { get; protected set; }
        public BonusTarget Target { get; protected set; }
        public Resource Resource { get; protected set; }
        public float Multiplier { get; protected set; }

        protected Bonus(
            Resource resource,
            float multiplier,
            BonusTarget target
        )
        {
            Resource = resource;
            Multiplier = multiplier;
            Target = target;
        }

        public abstract float Apply(
            float baseValue, ResourceManager.Consumer consumer
        );
    }
    #endregion

    #region - Global Bonus
    public class GlobalBonus : Bonus
    {
        public GlobalBonus(Resource resource, float multiplier, BonusTarget target)
            : base(resource, multiplier, target)
        {
            Type = BonusType.Global;
        }

        public override float Apply(float baseValue, ResourceManager.Consumer consumer)
        {
            return baseValue * Multiplier;
        }
    }
    #endregion

    #region - Local Bonus
    public class LocalBonus : Bonus
    {
        private Vector2Int sourcePos;
        public int Radius { get; private set; }

        private readonly List<ResourceManager.Consumer> affectedBuildings = new();

        public LocalBonus(Resource resource, float multiplier, BonusTarget target, Vector2Int source, int radius)
            : base(resource, multiplier, target)
        {
            Type = BonusType.Local;
            sourcePos = source;
            this.Radius = radius;
        }

        public void AddConsumerIfInRange(ResourceManager.Consumer consumer)
        {
            Vector2 pos = new Vector2(consumer.instance.Position.x, consumer.instance.Position.z);
            if (IsWithinRadius(sourcePos, pos, Radius) && !affectedBuildings.Contains(consumer))
            {
                affectedBuildings.Add(consumer);
            }
        }

        public void RemoveConsumer(ResourceManager.Consumer consumer)
        {
            affectedBuildings.Remove(consumer);
        }

        public bool AffectsConsumer(ResourceManager.Consumer consumer)
        {
            return affectedBuildings.Contains(consumer);
        }

        public override float Apply(float baseValue, ResourceManager.Consumer consumer)
        {
            if (AffectsConsumer(consumer))
                return baseValue * Multiplier;

            return baseValue;
        }

        private bool IsWithinRadius(Vector2 source, Vector2 target, int radius)
        {
            return (source - target).sqrMagnitude <= radius * radius;
        }
    }
    #endregion
}