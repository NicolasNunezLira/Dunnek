using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ConstructionSystem;
using ResourceSystem;
using BonusSystem;

namespace DraftSystem
{
    public static class ProceduralCardGenerator
    {
        [MenuItem("Tools/Generate Procedural Cards")]
        public static void Generate()
        {
            string path = "Assets/Resources/GeneratedCards";
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder("Assets/Resources", "GeneratedCards");

            var table = Resources.Load<CardGenerationTable>("CardGenerationTable");
            var constructions = ConstructionConfig.Instance.ConstructionConfigs.Values.ToList();

            foreach (var rule in table.rules)
            {
                foreach (var rarity in System.Enum.GetValues(typeof(Rarity)).Cast<Rarity>())
                {
                    int value = GetValueForRarity(rule, rarity);

                    foreach (var config in constructions)
                    {
                        // PRODUCE MÁS
                        if (rule.type == ProceduralEffectType.IncreaseProduction)
                        {
                            foreach (var kvp in config.rate.Where(r => r.Value > 0))
                                CreateCard(rule.type, rarity, value, config, kvp.Key);
                        }

                        // CONSUME MENOS
                        if (rule.type == ProceduralEffectType.ReduceConsumption)
                        {
                            foreach (var kvp in config.rate.Where(r => r.Value < 0))
                                CreateCard(rule.type, rarity, value, config, kvp.Key);
                        }

                        // COSTO MÁS BARATO (solo recursos en costo)
                        if (rule.type == ProceduralEffectType.ReduceCostSingle)
                        {
                            foreach (var kvp in config.cost)
                                CreateCard(rule.type, rarity, value, config, kvp.Key);
                        }

                        // COSTO MÁS BARATO EN TODOS
                        if (rule.type == ProceduralEffectType.ReduceCostAll)
                        {
                            CreateCard(rule.type, rarity, value, config, Resource.None);
                        }
                    }

                    // CARTAS GLOBALES
                    if (rule.type == ProceduralEffectType.GlobalProductionBoost ||
                        rule.type == ProceduralEffectType.GlobalConsumptionReduction ||
                        rule.type == ProceduralEffectType.InstantGain)
                    {
                        foreach (Resource res in System.Enum.GetValues(typeof(Resource)))
                        {
                            if (res == Resource.None) continue;
                            CreateCard(rule.type, rarity, value, null, res);
                        }
                    }
                }
            }

            // 🔑 NUEVO: Generar cartas de desbloqueo de construcciones
            foreach (var config in constructions)
            {
                CreateUnlockCard(config);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        static int GetValueForRarity(CardGenerationTable.Rule rule, Rarity rarity)
        {
            return rarity switch
            {
                Rarity.Common => rule.common,
                Rarity.Uncommon => rule.uncommon,
                Rarity.Rare => rule.rare,
                Rarity.Legendary => rule.legendary,
                _ => rule.common
            };
        }

        static void CreateCard(ProceduralEffectType type, Rarity rarity, int value,
            ConstructionConfig.ConfigData construction, Resource resource)
        {
            Card card = ScriptableObject.CreateInstance<Card>();
            card.rarity = rarity;
            card.effects = new List<ICardEffect>();

            string resourceName = resource != Resource.None ? resource.ToString() : "recursos";
            string constructionName = construction != null ? construction.displayName : "todas las construcciones";
            string constructionCode = construction?.codeName;

            switch (type)
            {
                case ProceduralEffectType.IncreaseProduction:
                    card.cardName = $"{constructionName} produce más {resourceName}";
                    card.description = $"{constructionName} producen +{value}% {resourceName}";
                    card.effects.Add(new GrantGlobalBonusEffect(resource, 1f + value / 100f, BonusTarget.Production,
                        constructionCode != null ? new List<string> { constructionCode } : new List<string>()));
                    break;

                case ProceduralEffectType.ReduceConsumption:
                    card.cardName = $"{constructionName} consume menos {resourceName}";
                    card.description = $"{constructionName} consumen {value}% menos {resourceName}";
                    card.effects.Add(new GrantGlobalBonusEffect(resource, 1f - value / 100f, BonusTarget.Consumption,
                        constructionCode != null ? new List<string> { constructionCode } : new List<string>()));
                    break;

                case ProceduralEffectType.ReduceCostSingle:
                    card.cardName = $"{constructionName} cuesta menos {resourceName}";
                    card.description = $"{constructionName} cuestan {value}% menos {resourceName}";
                    card.effects.Add(new GrantGlobalBonusEffect(resource, 1f - value / 100f, BonusTarget.Cost,
                        constructionCode != null ? new List<string> { constructionCode } : new List<string>()));
                    break;

                case ProceduralEffectType.ReduceCostAll:
                    card.cardName = $"Todas las construcciones son más baratas";
                    card.description = $"Todas las construcciones cuestan {value}% menos recursos";
                    card.effects.Add(new GrantGlobalBonusEffect(Resource.None, 1f - value / 100f, BonusTarget.Cost,
                        new List<string>()));
                    break;

                case ProceduralEffectType.InstantGain:
                    card.cardName = $"Obtén {value} {resourceName}";
                    card.description = $"Recibes {value} {resourceName} inmediatamente";
                    card.effects.Add(new GrantResourceBonusEffect(resource, value));
                    break;

                case ProceduralEffectType.GlobalProductionBoost:
                    card.cardName = $"{resourceName} se produce más";
                    card.description = $"{resourceName} se produce un {value}% más globalmente";
                    card.effects.Add(new GrantGlobalBonusEffect(resource, 1f + value / 100f, BonusTarget.Production,
                        new List<string>()));
                    break;

                case ProceduralEffectType.GlobalConsumptionReduction:
                    card.cardName = $"{resourceName} se consume menos";
                    card.description = $"{resourceName} se consume un {value}% menos globalmente";
                    card.effects.Add(new GrantGlobalBonusEffect(resource, 1f - value / 100f, BonusTarget.Consumption,
                        new List<string>()));
                    break;
            }

            SaveCardAsset(card, $"{rarity}_{card.cardName}");
        }

        // 🔑 NUEVO: Método para crear cartas de desbloqueo
        static void CreateUnlockCard(ConstructionConfig.ConfigData construction)
        {
            Card card = ScriptableObject.CreateInstance<Card>();
            card.rarity = Rarity.Common; // Puedes parametrizar esto si quieres rarezas distintas
            card.effects = new List<ICardEffect>
            {
                new UnlockConstructionEffect(construction.codeName)
            };

            card.cardName = $"Desbloquear {construction.displayName}";
            card.description = $"Permite construir {construction.displayName}";

            SaveCardAsset(card, $"Unlock_{construction.displayName}");
        }

        static void SaveCardAsset(Card card, string fileName)
        {
            string folder = "Assets/Resources/GeneratedCards";
            string safeName = string.Concat(fileName.Split(System.IO.Path.GetInvalidFileNameChars()));
            AssetDatabase.CreateAsset(card, $"{folder}/{safeName}.asset");
        }
    }
}
