using UnityEngine;
using System.Collections.Generic;
using Data;
using Utils;

public class ActionConfig : Singleton<ActionConfig>
{
    public Dictionary<DualMesh.ActionMode, ConfigData> actionsConfig = new();

    protected override void Awake()
    {
        base.Awake();
        LoadActionsConfig();
    }

    [System.Serializable]
    public class ResourceCost
    {
        public int WorkForce;
        public int Sand;
    }

    [System.Serializable]
    public class ResourceProduction
    {
        public int[] WorkForce;
        public int[] Sand;
    }

    [System.Serializable]
    public class ConfigData
    {
        public string type;
        public ResourceCost cost;
        public string prefab;
        [System.NonSerialized]
        public GameObject loadedPrefab;
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
        }

        ConfigDataList dataList = JsonUtility.FromJson<ConfigDataList>(jsonText.text);
        foreach (var item in dataList.configs)
        {
            if (!System.Enum.TryParse(item.type, out DualMesh.ActionMode type))
            {
                Debug.LogError($"Tipo de construcción no reconocido: {item.type}");
                continue;
            }

            item.loadedPrefab = Resources.Load<GameObject>(item.prefab);
            if (item.loadedPrefab == null)
            {
                Debug.LogError($"No se encontró el prefab en Resources/{item.prefab}");
            }
            actionsConfig[type] = item;
        }
    }
}