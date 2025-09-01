using System.Collections.Generic;
using UnityEngine;
using Utils;

public class ConstructionDatabase : Singleton<ConstructionDatabase>
{
    public Dictionary<string, ConstructionData> constructionLookup;

    protected override void Awake()
    {
        base.Awake();
        LoadConstructions();
    }

    public void LoadConstructions()
    {
        constructionLookup = new Dictionary<string, ConstructionData>();

        // Carga el JSON desde Resources (Assets/Resources/ConstructionData.json)
        TextAsset jsonText = Resources.Load<TextAsset>("ConstructionData");
        if (jsonText == null)
        {
            Debug.LogError("No se encontró ConstructionData.json en Resources");
            return;
        }

        ConstructionDataList dataList = JsonUtility.FromJson<ConstructionDataList>(jsonText.text);
        foreach (var c in dataList.constructions)
        {
            // Si en prefabs tienes solo strings, aquí puedes cargar los GameObjects
            // Ejemplo usando Resources.Load
            List<GameObject> loadedPrefabs = new List<GameObject>();
            foreach (var prefabName in c.prefabs)
            {
                GameObject prefab = Resources.Load<GameObject>(prefabName);
                if (prefab != null)
                    loadedPrefabs.Add(prefab);
                else
                    Debug.LogWarning($"No se encontró prefab {prefabName} en Resources");
            }
            c.prefabs = loadedPrefabs;

            // Agregar al diccionario
            constructionLookup[c.id] = c;
        }

        Debug.Log($"Se cargaron {constructionLookup.Count} construcciones.");
    }

    public ConstructionData GetConstructionById(string id)
    {
        if (constructionLookup.TryGetValue(id, out ConstructionData data))
            return data;
        return null;
    }
}
