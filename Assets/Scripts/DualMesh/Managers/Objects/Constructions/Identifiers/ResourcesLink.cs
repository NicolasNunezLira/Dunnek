using UnityEngine;
using ResourceSystem;
using ConstructionSystem;
using System.Collections.Generic;
using System.Text;
using System;
using BonusSystem;
using Building;

public class ResourcesLink : MonoBehaviour
{
    public int? ConsumerId { get; private set; }
    public bool IsConsumer { get; private set; }
    public bool IsBonusProvider { get; private set; }

    public Dictionary<Resource, float> rates { get; private set; }
    public List<ConstructionConfig.BonusEntry> bonus { get; private set; }
    public List<ConstructionConfig.BonusEntry> GlobalBonuses { get; private set; }
    public List<ConstructionConfig.BonusEntry> LocalBonuses { get; private set; }
    public List<ConstructionInstance> AffectingProviders { get; private set; }

    private string codeName;

    public void Init(string codeName, int? consumerId = null)
    {
        this.codeName = codeName;
        ConsumerId = consumerId;

        var config = ConstructionConfig.Instance.ConstructionConfigs[codeName];
        rates = config.rate;
        bonus = config.bonusList;

        // Determinar si es consumidor
        IsConsumer = false;
        if (rates != null)
        {
            foreach (float value in rates.Values)
            {
                if (value < 0)
                {
                    IsConsumer = true;
                    break;
                }
            }
        }

        // Determinar si es proveedor de bonus
        IsBonusProvider = bonus != null && bonus.Count > 0;

        if (IsBonusProvider)
        {
            GlobalBonuses = new List<ConstructionConfig.BonusEntry>();
            LocalBonuses = new List<ConstructionConfig.BonusEntry>();

            foreach (var b in bonus)
            {
                if (b.bonusType == "Global")
                {
                    GlobalBonuses.Add(b);
                }
                else if (b.bonusType == "Local")
                {
                    LocalBonuses.Add(b);
                }
            }
        }
    }

    public void RefreshAffectingProviders()
    {
        AffectingProviders.Clear();

        if (ConsumerId == null) return;

        var consumers = ResourceManager.GetAllConsumers();
        if (!consumers.ContainsKey(ConsumerId.Value)) return;

        var consumer = consumers[ConsumerId.Value];

        foreach (var local in BonusManager.GetLocalBonuses())
        {
            if (local.AffectsConsumer(consumer))
            {
                var providerId = (local as IProviderInfo)?.ProviderId;
                if (providerId != null && DualMesh.Instance.builder.constructions.TryGetValue(providerId.Value, out var providerInstance))
                {
                    AffectingProviders.Add(providerInstance);
                }
            }
        }
    }

    public string GetInfoString()
    {
        StringBuilder sb = new StringBuilder();

        if (rates != null && rates.Count > 0)
        {
            sb.AppendLine("Production rates:");
            foreach (var kvp in rates)
            {
                Resource resource = kvp.Key;
                float value = kvp.Value;

                if (Mathf.Approximately(value, 0f)) continue;

                string sign = value > 0 ? "+" : "-";
                sb.AppendLine($"• {sign}{Mathf.Abs(value)} {resource}");
            }
        }

        // Mostrar bonus
        if (IsBonusProvider)
        {
            if (GlobalBonuses.Count > 0)
            {
                sb.AppendLine("Global bonuses:");
                foreach (var g in GlobalBonuses)
                {
                    foreach (var effects in g.effects)
                        sb.AppendLine($"• {effects.resource} {effects.pct * 100}%");
                }
                sb.AppendLine();
            }

            if (LocalBonuses.Count > 0)
            {
                sb.AppendLine("Local bonuses:");
                foreach (var l in LocalBonuses)
                {
                    foreach (var effects in l.effects)
                        sb.AppendLine($"• {effects.resource} {effects.pct * 100}% (radius {l.radius})");
                }
                sb.AppendLine();
            }
        }

        return sb.Length > 0 ? sb.ToString() : null;
    }
    
    public string GetInfoStringWithBonuses()
    {
        if (ConsumerId == null)
            return GetInfoString(); // si no es consumidor, mostramos lo mismo que antes

        var sb = new StringBuilder();

        // Obtenemos el consumer desde ResourceManager
        var consumers = ResourceManager.GetAllConsumers();
        if (!consumers.ContainsKey(ConsumerId.Value))
            return GetInfoString();

        var consumer = consumers[ConsumerId.Value];

        sb.AppendLine("<b>Production rates:</b>");

        foreach (var (resource, baseRate) in consumer.rates)
        {
            if (Mathf.Approximately(baseRate, 0f)) continue;

            // usamos el cálculo detallado
            var detail = BonusManager.ApplyBonusesDetailed(baseRate, resource, consumer);

            string sign = detail.FinalValue > 0 ? "+" : "-";
            sb.AppendLine($"• {resource}: {sign}{Mathf.Abs(detail.FinalValue):0.00}");

            // extra: mostrar desglose
            if (detail.GlobalMultiplier != 1f)
                sb.AppendLine($"    Global x{detail.GlobalMultiplier:0.00}");
            if (detail.LocalMultiplier != 1f)
                sb.AppendLine($"    Local x{detail.LocalMultiplier:0.00}");

            /*
            foreach (var applied in detail.AppliedBonuses)
                sb.AppendLine($"    - {applied.ProviderName} ({applied.OriginType}) x{applied.Multiplier:0.00}");
            */
        }

        return sb.ToString();
    }
}

    /*
    public int ConsumerId { get; private set; }
    public bool IsConsumer { get; private set; }
    public Dictionary<Resource, float> rates { get; private set; }

    public void Init(int consumerId, string codeName)
    {
        ConsumerId = consumerId;
        rates = ConstructionConfig.Instance.ConstructionConfigs[codeName].rate;
        IsConsumer = false;
        foreach (float value in rates.Values)
        {
            if (value < 0)
            {
                IsConsumer = true;
                break;
            }
        }
    }

    public string GetInfoString()
    {
        if (rates == null || rates.Count == 0) return null;

        StringBuilder sb = new StringBuilder();

        foreach (var kvp in rates)
        {
            Resource resource = kvp.Key;
            float value = kvp.Value;

            if (Mathf.Approximately(value, 0f)) continue;

            string sign = value > 0 ? "+" : "-";
            sb.AppendLine($"{sign}{Mathf.Abs(value)} {resource}");
        }

        return sb.ToString();
    }
    */
