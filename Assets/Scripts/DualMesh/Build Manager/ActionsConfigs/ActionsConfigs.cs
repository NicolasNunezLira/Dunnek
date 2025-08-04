using UnityEngine;
using System.Collections.Generic;
using Data;
using Utils;
using ResourceSystem;

public class ActionConfig : Singleton<ActionConfig>
{
    public Dictionary<DualMesh.ActionMode, ConfigData> actionsConfig = new();

    protected override void Awake()
    {
        base.Awake();
        LoadActionsConfig();
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
        public List<ResourceAmount> productionList;

        // Estos son los diccionarios reales que se usarán en código
        [System.NonSerialized]
        public ResourceCost cost;

        [System.NonSerialized]
        public ResourceCost production;

        public string prefab;
        [System.NonSerialized]
        public GameObject loadedPrefab;

        public void InitializeResources()
        {
            cost = new ResourceCost(costList);
            production = new ResourceCost(productionList);
        }
    }

    [System.Serializable]
    public class ConfigDataList
    {
        public List<ConfigData> configs;
    }

    void LoadActionsConfig()
    {
        string path = "Configs/ActionsProperties";
        TextAsset jsonText = Resources.Load<TextAsset>(path);

        if (jsonText == null)
        {
            Debug.LogError($"{path} no encontrado.");
            return;
        }

        ConfigDataList dataList = JsonUtility.FromJson<ConfigDataList>(jsonText.text);
        foreach (var item in dataList.configs)
        {
            if (!System.Enum.TryParse(item.type, out DualMesh.ActionMode type))
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

            actionsConfig[type] = item;
        }
    }

}