using UnityEngine;
using System.Collections.Generic;
using Data;
using Utils;
using ResourceSystem;

public class ConstructionConfig : Singleton<ConstructionConfig>
{
    public Dictionary<ConstructionType, ConfigData> constructionConfig = new();

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
        public string type;

        // Estos se usan solo para cargar desde JSON
        public List<ResourceAmount> costList;
        public List<ResourceAmount> rateList;
        public float recycleWorkCost;

        // Estos son los diccionarios reales que se usarán en código
        [System.NonSerialized]
        public ResourceCost cost;

        [System.NonSerialized]
        public ResourceCost rate;

        public string prefab;
        [System.NonSerialized]
        public GameObject loadedPrefab;

        public void InitializeResources()
        {
            cost = new ResourceCost(costList);
            rate = new ResourceCost(rateList);
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
            if (!System.Enum.TryParse(item.type, out ConstructionType type))
            {
                Debug.LogError($"Tipo de construcción no reconocido: {item.type}");
                continue;
            }

            item.InitializeResources(); // Convierte listas a diccionarios

            item.loadedPrefab = Resources.Load<GameObject>(item.prefab);
            if (item.loadedPrefab == null)
            {
                Debug.LogError($"No se encontró el prefab en Resources/{item.prefab}");
            }

            constructionConfig[type] = item;
        }
    }

}