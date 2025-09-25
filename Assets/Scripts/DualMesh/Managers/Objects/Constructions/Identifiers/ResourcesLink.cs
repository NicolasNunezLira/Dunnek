using UnityEngine;
using ResourceSystem;
using ConstructionSystem;
using System.Collections.Generic;
using System.Text;
using BonusSystem;

public class ResourcesLink : MonoBehaviour
{
    #region - Properties
    public ConstructionInstance Building { get; private set; }
    public int? ConsumerId { get; private set; }
    public bool IsConsumer { get; private set; }
    public bool IsBonusProvider { get; private set; }

    public Dictionary<Resource, float> Rates { get; private set; }
    public List<ConstructionConfig.BonusEntry> AllBonuses { get; private set; }
    public List<ConstructionConfig.BonusEntry> GlobalBonuses { get; private set; }
    public List<ConstructionConfig.BonusEntry> LocalBonuses { get; private set; }
    #endregion

    private string codeName;

    #region - Initializer
    public void Init(ConstructionInstance instance, string codeName, int? consumerId = null)
    {
        this.codeName = codeName;
        Building = instance;
        ConsumerId = consumerId;

        var config = ConstructionConfig.Instance.ConstructionConfigs[codeName];
        Rates = config.rate;
        AllBonuses = config.bonusList;

        // Consumidor si alguna tasa es negativa
        IsConsumer = Rates != null && HasConsumption(Rates);

        // Proveedor de bonus si tiene lista
        IsBonusProvider = AllBonuses != null && AllBonuses.Count > 0;
        if (IsBonusProvider)
        {
            GlobalBonuses = new List<ConstructionConfig.BonusEntry>();
            LocalBonuses = new List<ConstructionConfig.BonusEntry>();

            foreach (var b in AllBonuses)
            {
                if (b.bonusType == "Global")
                    GlobalBonuses.Add(b);
                else if (b.bonusType == "Local")
                    LocalBonuses.Add(b);
            }
        }
    }

    private bool HasConsumption(Dictionary<Resource, float> rates)
    {
        foreach (float value in rates.Values)
        {
            if (value < 0f) return true;
        }
        return false;
    }
    #endregion

    #region - Tooltip Info
    public string GetInfoString()
    {
        var sb = new StringBuilder();

        // Producción/consumo base
        if (Rates != null && Rates.Count > 0)
        {
            sb.AppendLine("<b>Base rates:</b>");
            foreach (var (resource, value) in Rates)
            {
                if (Mathf.Approximately(value, 0f)) continue;
                string sign = value > 0 ? "+" : "-";
                sb.AppendLine($"• {sign}{Mathf.Abs(value)} {resource}");
            }
            sb.AppendLine();
        }

        // Proveedor de bonus
        if (IsBonusProvider)
        {
            if (GlobalBonuses.Count > 0)
            {
                sb.AppendLine("<b>Global bonuses:</b>");
                foreach (var g in GlobalBonuses)
                {
                    foreach (var e in g.effects)
                        sb.AppendLine($"• {e.resource} {e.pct * 100:0.#}%");
                }
                sb.AppendLine();
            }

            if (LocalBonuses.Count > 0)
            {
                sb.AppendLine("<b>Local bonuses:</b>");
                foreach (var l in LocalBonuses)
                {
                    foreach (var e in l.effects)
                        sb.AppendLine($"• {e.resource} {e.pct * 100:0.#}% (radius {l.radius})");
                }
                sb.AppendLine();
            }
        }

        return sb.Length > 0 ? sb.ToString() : null;
    }

    public string GetInfoStringWithBonuses()
    {
        // Si no es consumidor → mostrar solo info de provider
        if (ConsumerId == null)
        {
            var sbProvider = new StringBuilder();

            if (IsBonusProvider)
            {
                if (GlobalBonuses.Count > 0)
                {
                    sbProvider.AppendLine("<b>Global:</b>");
                    foreach (var g in GlobalBonuses)
                    {
                        foreach (var e in g.effects)
                        {
                            string sign = e.pct >= 0 ? "+" : "-";
                            sbProvider.AppendLine($"- {sign}{Mathf.Abs(e.pct * 100):0.#}% {FormatEffect(e)}");
                        }
                    }
                    sbProvider.AppendLine();
                }

                if (LocalBonuses.Count > 0)
                {
                    sbProvider.AppendLine("<b>Local:</b>");
                    foreach (var l in LocalBonuses)
                    {
                        sbProvider.AppendLine($"(Radius {l.radius})");
                        foreach (var e in l.effects)
                        {
                            string sign = e.pct >= 0 ? "+" : "-";
                            sbProvider.AppendLine($"- {sign}{Mathf.Abs(e.pct * 100):0.#}% {FormatEffect(e)}");
                        }
                    }
                    sbProvider.AppendLine();
                }
            }

            return sbProvider.ToString();
        }

        // Si es consumidor → mostrar tasas finales y bonuses aplicados
        var sb = new StringBuilder();
        var consumers = ResourceManager.GetAllConsumers();

        if (!consumers.TryGetValue(ConsumerId.Value, out var consumer))
            return GetInfoString();

        sb.AppendLine("<b>Effective rates:</b>");

        foreach (var (resource, baseRate) in consumer.rates)
        {
            if (Mathf.Approximately(baseRate, 0f)) continue;

            var detail = BonusManager.ApplyBonusesDetailed(baseRate, resource, consumer);

            string sign = detail.FinalValue > 0 ? "+" : "-";
            sb.AppendLine($"• {resource}: {sign}{Mathf.Abs(detail.FinalValue):0.00}");

            // Desglose por bonus aplicado → porcentaje en vez de multiplicador
            foreach (var applied in detail.AppliedBonuses)
            {
                float pct = (applied.Multiplier - 1f) * 100f;
                if (Mathf.Approximately(pct, 0f)) continue;

                string pctSign = pct >= 0 ? "+" : "-";
                //sb.AppendLine($"    - {applied.ProviderName} ({applied.OriginType}) {pctSign}{Mathf.Abs(pct):0.#}%");
                sb.AppendLine($"    - {pctSign}{Mathf.Abs(pct):0.#}%");
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Convierte un efecto a string legible (ej: "producción trabajo" o "consumo arena")
    /// </summary>
    private string FormatEffect(ConstructionConfig.BonusEffect e)
    {
        // Heurística: si pct es positivo y recurso es producido, llamarlo producción,
        // si es negativo y recurso es consumido, llamarlo consumo.
        // Ajusta según tu lógica de negocio.
        return (e.pct >= 0 ? "producción" : "consumo") + $" {e.resource}";
    }

    #endregion

    #region - On destroy
    private void OnDestroy()
    {
        if (IsBonusProvider) BonusManager.RemoveBonusesByProvider(gameObject);
    }
    #endregion
}
