using UnityEngine;

public class GridSystem : MonoBehaviour
{
    // Singleton instance
    public static GridSystem Instance { get; private set; }
    
    [Header("Grid Settings")]
    public int width = 10;
    public int length = 10;
    public float cellSize = 1.0f;
    public bool showDebug = true;
    
    [Header("Visual Settings")]
    public Color lineColor = Color.white;
    public Material gridMaterial;
    
    private GameObject gridVisual;
    
    // Matriz para almacenar el estado de cada celda
    private CellData[,] grid;
    
    // Estructura para almacenar información de cada celda
    public struct CellData
    {
        public bool isOccupied;
        public GameObject placedObject;
        // Agrega más datos según necesites (tipo de terreno, altura, etc.)
    }
    
    private void Awake()
    {
        // Configuración del Singleton
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Más de una instancia de GridSystem encontrada. Eliminando la duplicada.");
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        
        // Inicializa la matriz de la grilla
        InitializeGrid();
        
        // Crea la representación visual de la grilla
        if (showDebug)
        {
            CreateGridVisual();
        }
        
        Debug.Log("GridSystem inicializado como Singleton.");
    }
    
    private void InitializeGrid()
    {
        grid = new CellData[width, length];
        
        // Inicializa cada celda de la grilla
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < length; z++)
            {
                grid[x, z] = new CellData
                {
                    isOccupied = false,
                    placedObject = null
                };
            }
        }
        
        Debug.Log("Grid initialized: " + width + "x" + length);
    }
    
    private void CreateGridVisual()
    {
        // Elimina la visualización anterior si existe
        if (gridVisual != null)
        {
            Destroy(gridVisual);
        }
        
        gridVisual = new GameObject("GridVisual");
        gridVisual.transform.SetParent(transform);
        
        // Crea un LineRenderer para cada línea de la grilla
        for (int x = 0; x <= width; x++)
        {
            CreateLine(new Vector3(x * cellSize, 0.01f, 0), new Vector3(x * cellSize, 0.01f, length * cellSize));
        }
        
        for (int z = 0; z <= length; z++)
        {
            CreateLine(new Vector3(0, 0.01f, z * cellSize), new Vector3(width * cellSize, 0.01f, z * cellSize));
        }
    }
    
    private void CreateLine(Vector3 start, Vector3 end)
    {
        GameObject line = new GameObject("GridLine");
        line.transform.SetParent(gridVisual.transform);
        
        LineRenderer lr = line.AddComponent<LineRenderer>();
        lr.startWidth = 0.05f;
        lr.endWidth = 0.05f;
        lr.positionCount = 2;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        
        if (gridMaterial != null)
        {
            lr.material = gridMaterial;
        }
        else
        {
            lr.startColor = lineColor;
            lr.endColor = lineColor;
        }
    }
    
    // Convierte posición del mundo a coordenadas de grilla
    public Vector2Int WorldToGridPosition(Vector3 worldPosition)
    {
        int x = Mathf.FloorToInt(worldPosition.x / cellSize);
        int z = Mathf.FloorToInt(worldPosition.z / cellSize);
        
        // Asegúrate de que las coordenadas estén dentro de los límites
        x = Mathf.Clamp(x, 0, width - 1);
        z = Mathf.Clamp(z, 0, length - 1);
        
        return new Vector2Int(x, z);
    }
    
    // Convierte coordenadas de grilla a posición del mundo (centro de la celda)
    public Vector3 GridToWorldPosition(int x, int z)
    {
        float worldX = x * cellSize + cellSize / 2;
        float worldZ = z * cellSize + cellSize / 2;
        
        return new Vector3(worldX, 0, worldZ);
    }
    
    // Verifica si una celda está disponible
    public bool IsCellAvailable(int x, int z)
    {
        // Verifica que esté dentro de los límites
        if (x < 0 || x >= width || z < 0 || z >= length)
        {
            return false;
        }
        
        return !grid[x, z].isOccupied;
    }
    
    // Coloca un objeto en la grilla
    public bool PlaceObject(GameObject obj, int x, int z)
    {
        if (!IsCellAvailable(x, z))
        {
            return false;
        }
        
        // Obtiene la posición central de la celda en el mundo
        Vector3 worldPos = GridToWorldPosition(x, z);
        
        // Coloca el objeto
        obj.transform.position = worldPos;
        
        // Actualiza el estado de la celda
        CellData cellData = grid[x, z];
        cellData.isOccupied = true;
        cellData.placedObject = obj;
        grid[x, z] = cellData;
        
        return true;
    }
    
    // Elimina un objeto de la grilla
    public void RemoveObject(int x, int z)
    {
        if (x < 0 || x >= width || z < 0 || z >= length)
        {
            return;
        }
        
        CellData cellData = grid[x, z];
        
        if (cellData.isOccupied && cellData.placedObject != null)
        {
            Destroy(cellData.placedObject);
        }
        
        cellData.isOccupied = false;
        cellData.placedObject = null;
        grid[x, z] = cellData;
    }
    
    // Para visualizar la grilla en el editor
    private void OnDrawGizmos()
    {
        if (!showDebug) return;
        
        Gizmos.color = lineColor;
        
        // Dibuja líneas horizontales
        for (int x = 0; x <= width; x++)
        {
            Gizmos.DrawLine(
                new Vector3(x * cellSize, 0, 0),
                new Vector3(x * cellSize, 0, length * cellSize)
            );
        }
        
        // Dibuja líneas verticales
        for (int z = 0; z <= length; z++)
        {
            Gizmos.DrawLine(
                new Vector3(0, 0, z * cellSize),
                new Vector3(width * cellSize, 0, z * cellSize)
            );
        }
    }
}