using UnityEngine;

[CreateAssetMenu(fileName = "CardGenerationTable", menuName = "Draft/CardGenerationTable")]
public class CardGenerationTable : ScriptableObject
{
    [System.Serializable]
    public class Rule
    {
        public ProceduralEffectType type;
        public int common;
        public int uncommon;
        public int rare;
        public int legendary;
    }

    public Rule[] rules;
}

public enum ProceduralEffectType
{
    IncreaseProduction,       // Construcción produce más X recurso
    ReduceConsumption,        // Construcción consume menos X recurso
    ReduceCostSingle,         // Construcción es más barata en X recurso
    ReduceCostAll,            // Todas las construcciones cuestan menos X recurso
    InstantGain,              // Obtén X recurso de una
    GlobalProductionBoost,    // Recurso X se produce más
    GlobalConsumptionReduction// Recurso X se consume menos
}