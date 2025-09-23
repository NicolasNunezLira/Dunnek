using UnityEngine;
using ResourceSystem;
using System.Collections.Generic;
using Vector2 = UnityEngine.Vector2;
using UnityEditor.VersionControl;
using System.Text.RegularExpressions;

namespace BonusSystem
{
    #region - Enums
    public enum BonusType { Local, Global }
    public enum BonusTarget { Production, Consumption, Cost }
    #endregion

    #region - Abstract Bonus
    public abstract class Bonus
    {
        public GameObject Obj { get; protected set; }
        public BonusType Type { get; protected set; }
        public BonusTarget Target { get; protected set; }
        public Resource Resource { get; protected set; }
        public float Multiplier { get; protected set; }

        protected Bonus(
            Resource resource,
            float multiplier,
            BonusTarget target,
            GameObject obj
        )
        {
            Resource = resource;
            Multiplier = multiplier;
            Target = target;
            Obj = obj;
        }

        public abstract float Apply(
            float baseValue, ResourceManager.Consumer consumer
        );
    }
    #endregion

    #region - Global Bonus
    public class GlobalBonus : Bonus
    {
        public GlobalBonus(Resource resource, float multiplier, BonusTarget target, GameObject obj)
            : base(resource, multiplier, target, obj)
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
    public class LocalBonus : Bonus, IProviderInfo
    {
        public int? ProviderId { get; private set; } 
        public string ProviderName { get; private set; }
        public GameObject ProviderObject { get; private set; }

        private Vector2Int sourcePos;
        public float Radius { get; private set; }

        private readonly List<ResourceManager.Consumer> affectedBuildings = new();

        public LocalBonus(Resource resource, float multiplier, BonusTarget target, GameObject obj, Vector2Int source, int radius)
            : base(resource, multiplier, target, obj)
        {
            Type = BonusType.Local;
            sourcePos = source;
            this.Radius = radius * DualMesh.Instance.tileSize;

            ProviderId = int.Parse(Regex.Match(Obj.name, @"\d+$").Value);
            ProviderName = Obj.name;
            ProviderObject = Obj;
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
            Vector2 pos = new Vector2(consumer.instance.Position.x, consumer.instance.Position.z);
            return IsWithinRadius(sourcePos, pos, Radius);
        }

        public override float Apply(float baseValue, ResourceManager.Consumer consumer)
        {
            if (AffectsConsumer(consumer))
                return baseValue * Multiplier;

            return baseValue;
        }

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

        private bool IsWithinRadius(Vector2 source, Vector2 target, float radius)
        {
            return (source - target).sqrMagnitude <= radius * radius;
        }
    }
    #endregion
}