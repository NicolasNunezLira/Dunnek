using UnityEngine;
using ResourceSystem;
using ConstructionSystem;
using System.Collections.Generic;
using System.Text;
using System;

public class ResourcesLink : MonoBehaviour
{
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
}