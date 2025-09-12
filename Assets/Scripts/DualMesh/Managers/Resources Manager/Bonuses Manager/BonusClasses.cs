using UnityEngine;
using ResourceSystem;

namespace BonusSystem
{
    #region Enums
    public enum BonusType { Local, Global }
    public enum BonusTarget { Production, Consumption, Cost }
    #endregion

    #region Abstract Bonus
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

    #region Global Bonus
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

    #region Local Bonus
    public class LocalBonus : Bonus
    {
        private int sourceConsumerId;

        public LocalBonus(Resource resource, float multiplier, BonusTarget target, int sourceId)
            : base(resource, multiplier, target)
        {
            Type = BonusType.Local;
            sourceConsumerId = sourceId;
        }

        public override float Apply(float baseValue, ResourceManager.Consumer consumer)
        {
            if (IsAdjacent(sourceConsumerId, consumer.id))
            {
                return baseValue * Multiplier;
            }
            return baseValue;
        }

        private bool IsAdjacent(int sourceId, int targerId)
        {
            return true;
        }
    }
    #endregion
}