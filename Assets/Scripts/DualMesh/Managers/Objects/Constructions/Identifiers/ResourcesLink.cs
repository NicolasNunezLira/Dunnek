using UnityEngine;
using ResourceSystem;
using ConstructionSystem;
using System.Collections.Generic;
using System.Text;
using System;
using BonusSystem;

public class ResourcesLink : MonoBehaviour
{
    public int? ConsumerId { get; private set; }
    public bool IsConsumer { get; private set; }
    public bool IsBonusProvider { get; private set; }

    public Dictionary<Resource, float> rates { get; private set; }
    public List<ConstructionConfig.BonusEntry> bonus { get; private set; }
    public List<ConstructionConfig.BonusEntry> GlobalBonuses { get; private set; }
    public List<ConstructionConfig.BonusEntry> LocalBonuses { get; private set; }

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
