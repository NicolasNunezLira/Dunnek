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
        public class BonusData
        {
            public string type;
            public BonusEffect value;

            [System.Serializable]
            public class BonusEffect
            {
                public float radius;
                public float pct;
            }
        }

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

            // Estos se usan solo para cargar desde JSON
            public List<ResourceAmount> costList;
            public List<ResourceAmount> rateList;
            public float recycleWorkCost;
            public int duration; // Duration in turns until pull down is needed 
            public List<PrefabData> prefabs;
            public List<BonusData> bonus;

            // Estos son los diccionarios reales que se usarán en código
            [System.NonSerialized] public ResourceCost cost;
            [System.NonSerialized] public ResourceCost rate;
            [System.NonSerialized] public Dictionary<string, GameObject> loadedPrefabs;
            [System.NonSerialized] public ConstructionCategory category;

            public void InitializeResources()
            {
                cost = new ResourceCost(costList);
                rate = new ResourceCost(rateList);

                if (System.Enum.TryParse(constructionCategory, out ConstructionCategory parsed))
                {
                    category = parsed;
                }
                else
                {
                    Debug.LogWarning($"Category not found: {constructionCategory}");
                }

                loadedPrefabs = new Dictionary<string, GameObject>();
                foreach ( PrefabData prefab in prefabs)
                {
                    var go = Resources.Load<GameObject>(prefab.path);
                    if (go != null)
                    {
                        loadedPrefabs[prefab.part] = go;
                    }
                    else
                    {
                        Debug.LogError($"Prefab not foun in Resources/{prefab.path}");
                    }
                }
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

/*
Example for json structure:

{
    "configs": [
    {
        "codeName": "houseSand",
        "constructionCategory": "Housing",
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
        "category": "Wall",
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
        "costList": [
            { "type": "Work", "value": 10},
            { "type": "Sand", "value": 10}
        ],
        "bonus": [
            { "type" : "Work", "value": { "radius": 10, "pct": 0.1 } }
        ]
        "recycleWorkCost" : 20,
        "duration": 1000,
        "prefabs": [
            { "part": "building", "path": "Prefabs/Dunek TorreArena" }
        ]
    }
  ]
}
*/