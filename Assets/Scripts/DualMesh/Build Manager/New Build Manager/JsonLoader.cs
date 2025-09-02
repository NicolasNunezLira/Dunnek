using System.IO;
using UnityEngine;

public static class JsonLoader
{
    /// <summary>
    /// Carga un archivo JSON desde StreamingAssets y lo deserializa a ConstructionDataList.
    /// </summary>
    /// <param name="fileName">Nombre del archivo JSON (incluyendo extensión)</param>
    public static ConstructionDataList LoadConstructions(
        string fileName = "StramingAssets/ConstructionData.json"
        )
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, fileName);

        if (!File.Exists(filePath))
        {
            Debug.LogError($"[JsonLoader] No se encontró el archivo: {filePath}");
            return new ConstructionDataList();
        }

        string jsonContent = File.ReadAllText(filePath);

        try
        {
            return JsonUtility.FromJson<ConstructionDataList>(jsonContent);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[JsonLoader] Error al deserializar JSON: {ex.Message}");
            return new ConstructionDataList();
        }
    }
}
