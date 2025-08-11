using System.Collections.Generic;
using UnityEngine;
using Utils;
using static DesertPrefabSpawner;


[System.Serializable]
public class VegetationData
{
    public DesertElement type;
    public GameObject gameObject;
    public HashSet<Vector2Int> affectedNodes; // Coordenadas en la grilla

    public VegetationData(DesertElement type, GameObject go)
    {
        this.type = type;
        this.gameObject = go;
        this.affectedNodes = new HashSet<Vector2Int>();
    }
}

public static class VegetationManager
{

    // Grillas para efectos (true = afectado por vegetación)
    static public bool[,] windEffectGrid;
    static public bool[,] erosionEffectGrid;

    // Lista general de vegetación
    static private List<VegetationData> vegetationElements = new List<VegetationData>();

    // Configuración del terreno
    static private int width, height;
    static private float size;

    public static void Initialize()
    {
        width = DualMesh.Instance.simXResolution + 1;
        height = DualMesh.Instance.simZResolution + 1;
        size = DualMesh.Instance.size;

        windEffectGrid = new bool[width, height];
        erosionEffectGrid = new bool[width, height];

        vegetationElements.Clear();
    }

    public static void AddVegetation(DesertElement type, GameObject go)
    {
        var data = new VegetationData(type, go);

        // Calcula nodos afectados
        HashSet<Vector2Int> nodes = CalculateAffectedNodes(go);
        data.affectedNodes = nodes;

        // Marca las grillas según el tipo
        foreach (var node in nodes)
        {
            if (type == DesertElement.Tree)
            {
                windEffectGrid[node.x, node.y] = true; // Árboles afectan viento
                erosionEffectGrid[node.x, node.y] = true; // y erosión
            }
            else if (type == DesertElement.Bush)
            {
                erosionEffectGrid[node.x, node.y] = true; // Arbustos solo erosión
            }
        }

        vegetationElements.Add(data);
    }

    private static HashSet<Vector2Int> CalculateAffectedNodes(GameObject go, int extraRadius = 1)
    {
        HashSet<Vector2Int> affected = new HashSet<Vector2Int>();

        Collider collider = go.GetComponent<Collider>();
        if (collider == null)
        {
            Debug.LogWarning($"Vegetation object {go.name} has no collider.");
            return affected;
        }

        Bounds bounds = collider.bounds;

        // Determinar el rango de celdas
        int minX = Mathf.FloorToInt(bounds.min.x / size);
        int maxX = Mathf.FloorToInt(bounds.max.x / size);
        int minY = Mathf.FloorToInt(bounds.min.z / size);
        int maxY = Mathf.FloorToInt(bounds.max.z / size);

        // Raycast para precisión
        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                Vector3 worldPos = new Vector3(x * size + size / 2f, bounds.max.y + 1f, y * size + size / 2f);
                if (Physics.Raycast(worldPos, Vector3.down, out RaycastHit hit, bounds.size.y + 2f))
                {
                    if (hit.collider.gameObject == go)
                    {
                        // Añadimos el nodo
                        affected.Add(new Vector2Int(x, y));

                        // Añadimos vecinos en el radio extra
                        for (int dx = -extraRadius; dx <= extraRadius; dx++)
                        {
                            for (int dy = -extraRadius; dy <= extraRadius; dy++)
                            {
                                int nx = x + dx;
                                int ny = y + dy;

                                // Comprobar que esté dentro de la grilla
                                if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                                {
                                    affected.Add(new Vector2Int(nx, ny));
                                }
                            }
                        }
                    }
                }
            }
        }

        return affected;
    }


    // Acceso rápido en el tick
    public static bool IsAffectedByWind(int x, int y) => windEffectGrid[x, y];
    public static bool IsAffectedByErosion(int x, int y) => erosionEffectGrid[x, y];
}
