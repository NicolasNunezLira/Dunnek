// SandDuneSimulationGPU.cs - Integración completa con Unity
using UnityEngine;
using System.Collections.Generic;
using System.Runtime.InteropServices;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SandDuneSimulationGPU : MonoBehaviour
{
    [Header("Compute Shader")]
    public ComputeShader sandSimulationShader;
    
    [Header("Simulation Parameters")]
    [SerializeField] private int width = 512;
    [SerializeField] private int height = 512;
    [SerializeField] private int grainsPerStep = 1000;
    [SerializeField] private Vector2Int windDirection = new Vector2Int(1, 0);
    [SerializeField] private float erosionHeight = 0.1f;
    [SerializeField] private float depositeHeight = 0.1f;
    [SerializeField] private int hopLength = 10;
    [SerializeField] private float slopeThreshold = 0.1f;
    [SerializeField] private float pSand = 0.8f;
    [SerializeField] private float pNoSand = 0.1f;
    [SerializeField] private int maxCellsPerFrame = 100;
    
    [Header("Input Textures")]
    public Texture2D initialSandTexture;
    public Texture2D terrainShadowTexture;
    public Texture2D shadowTexture;
    
    [Header("Visual")]
    public Material terrainMaterial;
    public bool autoUpdateVisualization = true;
    public bool runSimulation = false;
    
    // Compute Buffers
    private ComputeBuffer sandBuffer;
    private ComputeBuffer terrainShadowBuffer;
    private ComputeBuffer shadowBuffer;
    private ComputeBuffer constructionGridBuffer;
    private ComputeBuffer constructionDataBuffer;
    private ComputeBuffer randomSeedBuffer;
    private ComputeBuffer changesBuffer;
    
    // Kernel indices
    private int erosionKernel;
    private int depositKernel;
    private int avalancheKernel;
    
    // Mesh components
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Mesh terrainMesh;
    
    // Estructuras para GPU (deben coincidir exactamente con el compute shader)
    [System.Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct ConstructionData
    {
        public float buildHeight;
        public int active;
        
        public ConstructionData(float height, bool isActive)
        {
            buildHeight = height;
            active = isActive ? 1 : 0;
        }
    }
    
    [System.Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct CellChange
    {
        public int x;
        public int z;
        public int changed;
        
        public CellChange(int posX, int posZ, bool hasChanged)
        {
            x = posX;
            z = posZ;
            changed = hasChanged ? 1 : 0;
        }
    }
    
    // Variables de estado
    private bool isInitialized = false;
    private float[] currentSandData;
    private Dictionary<int, ConstructionData> constructions = new Dictionary<int, ConstructionData>();
    
    void Start()
    {
        InitializeComponents();
        InitializeComputeShader();
        SetupBuffers();
        InitializeData();
        CreateTerrainMesh();
        isInitialized = true;
    }
    
    void InitializeComponents()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        
        if (meshRenderer.material != terrainMaterial && terrainMaterial != null)
        {
            meshRenderer.material = terrainMaterial;
        }
    }
    
    void InitializeComputeShader()
    {
        if (sandSimulationShader == null)
        {
            Debug.LogError("Sand Simulation Compute Shader not assigned!");
            return;
        }
        
        erosionKernel = sandSimulationShader.FindKernel("ErodeGrains");
        depositKernel = sandSimulationShader.FindKernel("DepositGrains");
        avalancheKernel = sandSimulationShader.FindKernel("RunAvalanche");
    }
    
    void SetupBuffers()
    {
        int totalCells = width * height;
        
        // Liberar buffers existentes si los hay
        ReleaseBuffers();
        
        // Crear buffers principales
        sandBuffer = new ComputeBuffer(totalCells, sizeof(float));
        terrainShadowBuffer = new ComputeBuffer(totalCells, sizeof(float));
        shadowBuffer = new ComputeBuffer(totalCells, sizeof(int));
        constructionGridBuffer = new ComputeBuffer(totalCells, sizeof(int));
        
        // Buffer para datos de construcciones
        constructionDataBuffer = new ComputeBuffer(1000, Marshal.SizeOf(typeof(ConstructionData)));
        
        // Buffer para semillas aleatorias
        randomSeedBuffer = new ComputeBuffer(grainsPerStep, sizeof(uint));
        
        // Buffer para cambios
        changesBuffer = new ComputeBuffer(grainsPerStep, Marshal.SizeOf(typeof(CellChange)));
        
        // Configurar buffers en todos los kernels
        SetBuffersToKernel(erosionKernel);
        SetBuffersToKernel(depositKernel);
        SetBuffersToKernel(avalancheKernel);
        
        // Configurar parámetros constantes
        sandSimulationShader.SetInt("width", width);
        sandSimulationShader.SetInt("height", height);
        sandSimulationShader.SetInt("hopLength", hopLength);
        sandSimulationShader.SetFloat("slopeThreshold", slopeThreshold);
        sandSimulationShader.SetFloat("pSand", pSand);
        sandSimulationShader.SetFloat("pNoSand", pNoSand);
    }
    
    void SetBuffersToKernel(int kernelIndex)
    {
        sandSimulationShader.SetBuffer(kernelIndex, "sandHeights", sandBuffer);
        sandSimulationShader.SetBuffer(kernelIndex, "terrainShadow", terrainShadowBuffer);
        sandSimulationShader.SetBuffer(kernelIndex, "shadow", shadowBuffer);
        sandSimulationShader.SetBuffer(kernelIndex, "constructionGrid", constructionGridBuffer);
        sandSimulationShader.SetBuffer(kernelIndex, "constructionData", constructionDataBuffer);
        sandSimulationShader.SetBuffer(kernelIndex, "randomSeeds", randomSeedBuffer);
        sandSimulationShader.SetBuffer(kernelIndex, "changes", changesBuffer);
    }
    
    void InitializeData()
    {
        int totalCells = width * height;
        
        // Inicializar datos de arena
        currentSandData = new float[totalCells];
        if (initialSandTexture != null)
        {
            LoadTextureToArray(initialSandTexture, currentSandData);
        }
        else
        {
            // Datos por defecto
            for (int i = 0; i < totalCells; i++)
            {
                currentSandData[i] = 0.1f; // Arena base
            }
        }
        sandBuffer.SetData(currentSandData);
        
        // Inicializar terrain shadow
        float[] terrainData = new float[totalCells];
        if (terrainShadowTexture != null)
        {
            LoadTextureToArray(terrainShadowTexture, terrainData);
        }
        terrainShadowBuffer.SetData(terrainData);
        
        // Inicializar sombras
        int[] shadowData = new int[totalCells];
        if (shadowTexture != null)
        {
            LoadTextureToIntArray(shadowTexture, shadowData);
        }
        shadowBuffer.SetData(shadowData);
        
        // Inicializar construction grid (vacío inicialmente)
        int[] constructionGrid = new int[totalCells];
        constructionGridBuffer.SetData(constructionGrid);
        
        // Inicializar construction data
        ConstructionData[] constructionDataArray = new ConstructionData[1000];
        for (int i = 0; i < 1000; i++)
        {
            constructionDataArray[i] = new ConstructionData(0f, false);
        }
        constructionDataBuffer.SetData(constructionDataArray);
        
        // Inicializar semillas aleatorias
        uint[] seeds = new uint[grainsPerStep];
        System.Random rnd = new System.Random();
        for (int i = 0; i < grainsPerStep; i++)
        {
            seeds[i] = (uint)rnd.Next();
        }
        randomSeedBuffer.SetData(seeds);
        
        // Inicializar buffer de cambios
        CellChange[] changes = new CellChange[grainsPerStep];
        changesBuffer.SetData(changes);
    }
    
    void LoadTextureToArray(Texture2D texture, float[] array)
    {
        Color[] pixels = texture.GetPixels();
        for (int i = 0; i < pixels.Length && i < array.Length; i++)
        {
            array[i] = pixels[i].grayscale;
        }
    }
    
    void LoadTextureToIntArray(Texture2D texture, int[] array)
    {
        Color[] pixels = texture.GetPixels();
        for (int i = 0; i < pixels.Length && i < array.Length; i++)
        {
            array[i] = pixels[i].grayscale > 0.5f ? 1 : 0;
        }
    }
    
    void CreateTerrainMesh()
    {
        if (meshFilter == null) return;
        
        terrainMesh = new Mesh();
        terrainMesh.name = "Sand Terrain";
        
        // Crear vértices para un plano
        Vector3[] vertices = new Vector3[(width + 1) * (height + 1)];
        Vector2[] uvs = new Vector2[vertices.Length];
        
        for (int z = 0, i = 0; z <= height; z++)
        {
            for (int x = 0; x <= width; x++, i++)
            {
                vertices[i] = new Vector3(x, 0, z);
                uvs[i] = new Vector2((float)x / width, (float)z / height);
            }
        }
        
        // Crear triángulos
        int[] triangles = new int[width * height * 6];
        for (int z = 0, ti = 0, vi = 0; z < height; z++, vi++)
        {
            for (int x = 0; x < width; x++, ti += 6, vi++)
            {
                triangles[ti] = vi;
                triangles[ti + 3] = triangles[ti + 2] = vi + 1;
                triangles[ti + 4] = triangles[ti + 1] = vi + width + 1;
                triangles[ti + 5] = vi + width + 2;
            }
        }
        
        terrainMesh.vertices = vertices;
        terrainMesh.triangles = triangles;
        terrainMesh.uv = uvs;
        terrainMesh.RecalculateNormals();
        
        meshFilter.mesh = terrainMesh;
    }
    
    // Función principal equivalente a tu SimulationMode()
    public void SimulationTick()
    {
        if (!isInitialized || sandSimulationShader == null) return;
        
        // Actualizar parámetros dinámicos
        sandSimulationShader.SetInt("windDx", windDirection.x);
        sandSimulationShader.SetInt("windDz", windDirection.y);
        sandSimulationShader.SetFloat("erosionHeight", erosionHeight);
        sandSimulationShader.SetFloat("depositeHeight", depositeHeight);
        sandSimulationShader.SetInt("grainsPerStep", grainsPerStep);
        
        // Ejecutar simulación si hay viento
        if (windDirection.x != 0 || windDirection.y != 0)
        {
            // Erosión y deposición (equivalente a tu Tick)
            int threadGroups = Mathf.CeilToInt(grainsPerStep / 64.0f);
            sandSimulationShader.Dispatch(erosionKernel, threadGroups, 1, 1);
            sandSimulationShader.Dispatch(depositKernel, threadGroups, 1, 1);
        }
        
        // Ejecutar avalanchas (equivalente a tu RunAvalancheBurst)
        for (int i = 0; i < 100; i++)
        {
            int avalancheThreadGroupsX = Mathf.CeilToInt(width / 8.0f);
            int avalancheThreadGroupsY = Mathf.CeilToInt(height / 8.0f);
            sandSimulationShader.Dispatch(avalancheKernel, avalancheThreadGroupsX, avalancheThreadGroupsY, 1);
        }
    }
    
    public void UpdateVisualization()
    {
        if (!isInitialized || sandBuffer == null) return;
        
        // Obtener datos actualizados
        sandBuffer.GetData(currentSandData);
        
        // Crear textura de altura
        Texture2D heightTexture = new Texture2D(width, height, TextureFormat.RFloat, false);
        heightTexture.SetPixelData(currentSandData, 0);
        heightTexture.Apply();
        
        // Aplicar al material
        if (terrainMaterial != null)
        {
            terrainMaterial.SetTexture("_HeightMap", heightTexture);
            terrainMaterial.SetFloat("_Width", width);
            terrainMaterial.SetFloat("_Height", height);
        }
        
        // También puedes actualizar la geometría del mesh si quieres deformación 3D
        UpdateMeshVertices();
    }
    
    void UpdateMeshVertices()
    {
        if (terrainMesh == null) return;
        
        Vector3[] vertices = terrainMesh.vertices;
        
        for (int z = 0, i = 0; z <= height && z < height; z++)
        {
            for (int x = 0; x <= width && x < width; x++, i++)
            {
                if (i < vertices.Length && (z * width + x) < currentSandData.Length)
                {
                    vertices[i].y = currentSandData[z * width + x] * 10f; // Escalar altura
                }
            }
        }
        
        terrainMesh.vertices = vertices;
        terrainMesh.RecalculateNormals();
    }
    
    // Funciones públicas para control externo
    public void AddConstruction(int x, int z, float height)
    {
        if (!IsValidCell(x, z)) return;
        
        int index = z * width + x;
        int constructionId = constructions.Count + 1;
        
        constructions[constructionId] = new ConstructionData(height, true);
        
        // Actualizar grid en CPU y GPU
        int[] constructionGrid = new int[width * (int)height];
        constructionGridBuffer.GetData(constructionGrid);
        constructionGrid[index] = constructionId;
        constructionGridBuffer.SetData(constructionGrid);
        
        // Actualizar construction data
        ConstructionData[] constructionDataArray = new ConstructionData[1000];
        constructionDataBuffer.GetData(constructionDataArray);
        constructionDataArray[constructionId] = constructions[constructionId];
        constructionDataBuffer.SetData(constructionDataArray);
    }
    
    public void SetWindDirection(Vector2Int newDirection)
    {
        windDirection = newDirection;
    }
    
    public void SetSimulationParameters(int newGrainsPerStep, float newErosionHeight, float newDepositeHeight)
    {
        grainsPerStep = newGrainsPerStep;
        erosionHeight = newErosionHeight;
        depositeHeight = newDepositeHeight;
        
        // Recrear buffer de semillas si cambió el número de granos
        if (randomSeedBuffer.count != grainsPerStep)
        {
            randomSeedBuffer?.Release();
            randomSeedBuffer = new ComputeBuffer(grainsPerStep, sizeof(uint));
            
            uint[] seeds = new uint[grainsPerStep];
            System.Random rnd = new System.Random();
            for (int i = 0; i < grainsPerStep; i++)
            {
                seeds[i] = (uint)rnd.Next();
            }
            randomSeedBuffer.SetData(seeds);
            
            SetBuffersToKernel(erosionKernel);
            SetBuffersToKernel(depositKernel);
        }
    }
    
    bool IsValidCell(int x, int z)
    {
        return x >= 0 && x < width && z >= 0 && z < height;
    }
    
    void Update()
    {
        // Ejecutar simulación si está activada
        if (runSimulation)
        {
            SimulationTick();
            
            if (autoUpdateVisualization)
            {
                UpdateVisualization();
            }
        }
        
        // Controles de teclado para testing
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SimulationTick();
            UpdateVisualization();
        }
        
        if (Input.GetKeyDown(KeyCode.R))
        {
            InitializeData(); // Reset
        }
    }
    
    void ReleaseBuffers()
    {
        sandBuffer?.Release();
        terrainShadowBuffer?.Release();
        shadowBuffer?.Release();
        constructionGridBuffer?.Release();
        constructionDataBuffer?.Release();
        randomSeedBuffer?.Release();
        changesBuffer?.Release();
    }
    
    void OnDestroy()
    {
        ReleaseBuffers();
    }
    
    void OnDisable()
    {
        ReleaseBuffers();
    }
}