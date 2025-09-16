using UnityEngine;
using System.Collections.Generic;
using Utils;
using ResourceSystem;

namespace ConstructionSystem
{
    public enum ConstructionCategory
    {
        Housing,
        Wall,
        Consumer,
        BonusProvider
    }

    public class ConstructionConfig : Singleton<ConstructionConfig>
    {
        public Dictionary<string, ConfigData> ConstructionConfigs { get; private set; } = new();

        protected override void Awake()
        {
            base.Awake();
            LoadConfig();
        }

        [System.Serializable]
        public class ResourceAmount
        {
            public string type;
            public float value;
        }

        [System.Serializable]
        public class BonusEffect
        {
            public string resource;
            public float pct;
        }

        [System.Serializable]
        public class BonusEntry
        {
            public string bonusType;            // "Global" o "Local"
            public string target;               // "Production" o "Consumption"
            public float radius;                // solo si es local
            public List<BonusEffect> effects;
        }

        [System.Serializable]
        public class PrefabData
        {
            public string part;
            public string path;
        }

        [System.Serializable]
        public class ResourceCost : Dictionary<Resource, float>
        {
            public ResourceCost() : base() { }

            public ResourceCost(List<ResourceAmount> raw)
            {
                foreach (var entry in raw)
                {
                    if (System.Enum.TryParse(entry.type, out Resource type))
                    {
                        this[type] = entry.value;
                    }
                    else
                    {
                        Debug.LogWarning($"Recurso desconocido: {entry.type}");
                    }
                }
            }

            public void PrintDebug()
            {
                foreach (var kvp in this)
                {
                    Debug.Log($"{kvp.Key}: {kvp.Value}");
                }
            }
        }


        [System.Serializable]
        public class ConfigData
        {
            public string codeName;
            public string constructionCategory;
            public string iconPath;

            // JSON input
            public List<ResourceAmount> costList;
            public List<ResourceAmount> rateList;
            public float recycleWorkCost;
            public int duration;
            public List<PrefabData> prefabs;
            public List<BonusEntry> bonusList;

            // Diccionarios procesados
            [System.NonSerialized] public ResourceCost cost;
            [System.NonSerialized] public ResourceCost rate;
            [System.NonSerialized] public Dictionary<string, GameObject> loadedPrefabs;
            [System.NonSerialized] public ConstructionCategory category;
            [System.NonSerialized] public Sprite icon;

            public void InitializeResources()
            {
                // Costos y tasas
                cost = new ResourceCost(costList);
                rate = new ResourceCost(rateList);

                // Categoría
                if (System.Enum.TryParse(constructionCategory, out ConstructionCategory parsed))
                {
                    category = parsed;
                }
                else
                {
                    Debug.LogWarning($"Category not found: {constructionCategory}");
                }

                // Prefabs
                loadedPrefabs = new Dictionary<string, GameObject>();
                foreach (PrefabData prefab in prefabs)
                {
                    var go = Resources.Load<GameObject>(prefab.path);
                    if (go != null)
                    {
                        loadedPrefabs[prefab.part] = go;
                    }
                    else
                    {
                        Debug.LogError($"Prefab not found in Resources/{prefab.path}");
                    }
                }

                // Icono
                if (!string.IsNullOrEmpty(iconPath))
                {
                    icon = Resources.Load<Sprite>(iconPath);
                }
            }

            public string GetBonusDescription()
            {
                if (bonusList == null || bonusList.Count == 0) return null;

                string text = "";
                foreach (var bonus in bonusList)
                {
                    string type = bonus.bonusType;
                    string target = bonus.target;
                    string radiusText = bonus.bonusType == "Local" ? $" (Radio {bonus.radius})" : "";

                    foreach (var eff in bonus.effects)
                    {
                        text += $"• {type} {target}: +{eff.pct * 100}% {eff.resource}{radiusText}\n";
                    }
                }
                return text;
            }
        }


        [System.Serializable]
        public class ConfigDataList
        {
            public List<ConfigData> configs;
        }

        void LoadConfig()
        {
            string path = "Configs/ConstructionsProperties";
            TextAsset jsonText = Resources.Load<TextAsset>(path);

            if (jsonText == null)
            {
                Debug.LogError($"{path} no encontrado.");
                return;
            }

            ConfigDataList dataList = JsonUtility.FromJson<ConfigDataList>(jsonText.text);
            foreach (var item in dataList.configs)
            {
                item.InitializeResources();
                ConstructionConfigs[item.codeName] = item;
            }
        }

    }
}

#region - Json example
/*
Example for json structure:

{
    "configs": [
    {
        "codeName": "houseSand",
        "constructionCategory": "Housing",
        "iconPath": "Icons/houseSandIcon",
        "costList": [
            { "type": "Work", "value": -1 },
            { "type": "Sand", "value": -10 }
        ],
        "rateList": [
            { "type": "Work", "value": 1 },
            { "type": "Sand", "value": 0 }
        ],
        "recycleWorkCost": 0,
        "duration": 1000,
        "prefabs": [
            { "part": "building", "path": "Prefabs/Dunek01" }
        ]
    },
    {
        "codeName": "wallSand",
        "constructionCategory": "Wall",
        "iconPath": "Icons/wallSandIcon",
        "costList": [
            { "type": "Work", "value": -1 },
            { "type": "Sand", "value": -5 }
        ],
        "rateList": [
            { "type": "Work", "value": 0 },
            { "type": "Sand", "value": 0 }
        ],
        "recycleWorkCost": -2,
        "duration": 1000,
        "prefabs": [
            { "part": "segmentWall", "path": "Prefabs/wall2" },
            { "part": "tower", "path": "Prefabs/tower" }
        ]
    },
    {
        "codeName": "Cantera",
        "constructionCategory": "Consumer",
        "iconPath": "Icons/canteraIcon",
        "costList": [
            { "type": "Work", "value": 0 },
            { "type": "Sand", "value": 0 }
        ],
        "rateList": [
            { "type": "Work", "value": -3 },
            { "type": "Sand", "value": 2 }
        ],
        "recycleWorkCost": -10,
        "duration": 1000,
        "prefabs": [
            { "part": "building", "path": "Prefabs/Cantera" }
        ]
    },
    {
        "codeName": "initialTemple",
        "constructionCategory": "BonusProvider",
        "iconPath": "Icons/initialTempleIcon",
        "costList": [
            { "type": "Work", "value": 10},
            { "type": "Sand", "value": 10}
        ],
        "bonusList": [
            {
                "bonusType": "Global",
                "target": "Production",
                "effects": [
                    { "resource": "Work", "pct": 0.1 }
                ]
            },
            {
                "bonusType": "Local",
                "target": "Consumption",
                "radius": 10,
                "effects": [
                    { "resource": "Sand", "pct": 0.25 },
                    { "resource": "Work", "pct": 0.05 }
                ]
            }
        ],
        "recycleWorkCost": 20,
        "duration": 1000,
        "prefabs": [
            { "part": "building", "path": "Prefabs/Dunek TorreArena" }
        ]
    }
  ]
}
*/
#endregion