public static class VegetationManager
{
    static public DesertPrefabSpawner.DesertElement[,] vegetationGrid;
    static public bool[,] windEffectGrid;
    static public bool[,] erosionEffectGrid;

    static private int width, height;

    public static void Initialize(int width_, int height_)
    {
        width = width_;
        height = height_;

        windEffectGrid = new bool[width, height];
        erosionEffectGrid = new bool[width, height];
        vegetationGrid = new DesertPrefabSpawner.DesertElement[width, height];
        Clear();
    }

    public static void Clear()
    {
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                windEffectGrid[x, y] = erosionEffectGrid[x, y] = false;
    }

    // Este método lo llamará VertexRaycaster para setear nodos afectados.
    // Parámetros: matriz booleana para viento y erosión ya calculadas.
    public static void SetEffectGrids(
        bool[,] windGrid,
        bool[,] erosionGrid,
        DesertPrefabSpawner.DesertElement[,] vegetationGrid_)
    {
        windEffectGrid = windGrid;
        erosionEffectGrid = erosionGrid;
        vegetationGrid = vegetationGrid_;
    }

    public static bool IsAffectedByWind(int x, int y) => windEffectGrid != null && windEffectGrid[x, y];
    public static bool IsAffectedByErosion(int x, int y) => erosionEffectGrid != null && erosionEffectGrid[x, y];
}
