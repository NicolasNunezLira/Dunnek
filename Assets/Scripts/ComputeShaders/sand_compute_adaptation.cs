// SandSimulationCompute.cs - Adaptación del algoritmo a GPU
using UnityEngine;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public class SandSimulationCompute : MonoBehaviour
{
    [Header("Compute Shader")]
    public ComputeShader sandSimulationShader;
    
    [Header("Simulation Parameters")]
    public int width = 512;
    public int height = 512;
    public int grainsPerStep = 1000;
    public Vector2Int windDirection = new Vector2Int(1, 0);
    public float erosionHeight = 0.1f;
    public float depositeHeight = 0.1f;
    public int hopLength = 10;
    public float slopeThreshold = 0.1f;
    public float pSand = 0.8f;
    public float pNoSand = 0.1f;
    
    [Header("Visual")]
    public Material terrainMaterial;
    public Texture2D initialSandTexture;
    public Texture2D terrainShadowTexture;
    
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
    
    // Structs for GPU
    [System.Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct ConstructionData
    {
        public float buildHeight;
        public int active; // bool en GPU
    }
    
    [System.Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct GrainData
    {
        public int startX;
        public int startZ;
        public float depositeAmount;
        public int active;
    }
    
    [System.Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct CellChange
    {
        public int x;
        public int z;
        public int changed;
    }
    
    void Start()
    {
        InitializeComputeShader();
        SetupBuffers();
        InitializeData();
    }
    
    void InitializeComputeShader()
    {
        erosionKernel = sandSimulationShader.FindKernel("ErodeGrains");
        depositKernel = sandSimulationShader.FindKernel("DepositGrains");
        avalancheKernel = sandSimulationShader.FindKernel("RunAvalanche");
    }
    
    void SetupBuffers()
    {
        int totalCells = width * height;
        
        // Buffers principales
        sandBuffer = new ComputeBuffer(totalCells, sizeof(float));
        terrainShadowBuffer = new ComputeBuffer(totalCells, sizeof(float));
        shadowBuffer = new ComputeBuffer(totalCells, sizeof(int));
        constructionGridBuffer = new ComputeBuffer(totalCells, sizeof(int));
        
        // Buffer para datos de construcciones (máximo 1000 construcciones)
        constructionDataBuffer = new ComputeBuffer(1000, Marshal.SizeOf(typeof(ConstructionData)));
        
        // Buffer para semillas aleatorias (una por thread)
        randomSeedBuffer = new ComputeBuffer(grainsPerStep, sizeof(uint));
        
        // Buffer para cambios
        changesBuffer = new ComputeBuffer(grainsPerStep * 2, Marshal.SizeOf(typeof(CellChange)));
        
        // Configurar buffers en shader
        sandSimulationShader.SetBuffer(erosionKernel, "sandHeights", sandBuffer);
        sandSimulationShader.SetBuffer(erosionKernel, "terrainShadow", terrainShadowBuffer);
        sandSimulationShader.SetBuffer(erosionKernel, "shadow", shadowBuffer);
        sandSimulationShader.SetBuffer(erosionKernel, "constructionGrid", constructionGridBuffer);
        sandSimulationShader.SetBuffer(erosionKernel, "constructionData", constructionDataBuffer);
        sandSimulationShader.SetBuffer(erosionKernel, "randomSeeds", randomSeedBuffer);
        sandSimulationShader.SetBuffer(erosionKernel, "changes", changesBuffer);
        
        sandSimulationShader.SetBuffer(depositKernel, "sandHeights", sandBuffer);
        sandSimulationShader.SetBuffer(depositKernel, "terrainShadow", terrainShadowBuffer);
        sandSimulationShader.SetBuffer(depositKernel, "shadow", shadowBuffer);
        sandSimulationShader.SetBuffer(depositKernel, "constructionGrid", constructionGridBuffer);
        sandSimulationShader.SetBuffer(depositKernel, "constructionData", constructionDataBuffer);
        sandSimulationShader.SetBuffer(depositKernel, "changes", changesBuffer);
        
        // Parámetros del shader
        sandSimulationShader.SetInt("width", width);
        sandSimulationShader.SetInt("height", height);
        sandSimulationShader.SetInt("hopLength", hopLength);
        sandSimulationShader.SetFloat("slopeThreshold", slopeThreshold);
        sandSimulationShader.SetFloat("pSand", pSand);
        sandSimulationShader.SetFloat("pNoSand", pNoSand);
    }
    
    void InitializeData()
    {
        // Inicializar datos de arena desde textura
        if (initialSandTexture != null)
        {
            float[] sandData = new float[width * height];
            for (int z = 0; z < height; z++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color pixel = initialSandTexture.GetPixel(x, z);
                    sandData[z * width + x] = pixel.grayscale;
                }
            }
            sandBuffer.SetData(sandData);
        }
        
        // Inicializar terrainShadow desde textura
        if (terrainShadowTexture != null)
        {
            float[] terrainData = new float[width * height];
            for (int z = 0; z < height; z++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color pixel = terrainShadowTexture.GetPixel(x, z);
                    terrainData[z * width + x] = pixel.grayscale;
                }
            }
            terrainShadowBuffer.SetData(terrainData);
        }
        
        // Inicializar semillas aleatorias
        uint[] seeds = new uint[grainsPerStep];
        System.Random rnd = new System.Random();
        for (int i = 0; i < grainsPerStep; i++)
        {
            seeds[i] = (uint)rnd.Next();
        }
        randomSeedBuffer.SetData(seeds);
    }
    
    public void SimulationTick()
    {
        // Actualizar parámetros dinámicos
        sandSimulationShader.SetInt("windDx", windDirection.x);
        sandSimulationShader.SetInt("windDz", windDirection.y);
        sandSimulationShader.SetFloat("erosionHeight", erosionHeight);
        sandSimulationShader.SetFloat("depositeHeight", depositeHeight);
        sandSimulationShader.SetInt("grainsPerStep", grainsPerStep);
        
        // Ejecutar erosión y deposición
        int threadGroups = Mathf.CeilToInt(grainsPerStep / 64.0f);
        sandSimulationShader.Dispatch(erosionKernel, threadGroups, 1, 1);
        sandSimulationShader.Dispatch(depositKernel, threadGroups, 1, 1);
        
        // Ejecutar avalanchas
        for (int i = 0; i < 100; i++)
        {
            int avalancheThreadGroups = Mathf.CeilToInt((width * height) / 64.0f);
            sandSimulationShader.Dispatch(avalancheKernel, avalancheThreadGroups, 1, 1);
        }
    }
    
    public void UpdateVisualization()
    {
        // Obtener datos actualizados para visualización
        float[] sandData = new float[width * height];
        sandBuffer.GetData(sandData);
        
        // Crear textura para el material
        Texture2D sandTexture = new Texture2D(width, height, TextureFormat.RFloat, false);
        sandTexture.SetPixelData(sandData, 0);
        sandTexture.Apply();
        
        // Aplicar al material
        if (terrainMaterial != null)
        {
            terrainMaterial.SetTexture("_SandHeights", sandTexture);
        }
    }
    
    void Update()
    {
        if (Input.GetKey(KeyCode.Space))
        {
            SimulationTick();
            UpdateVisualization();
        }
    }
    
    void OnDestroy()
    {
        // Liberar buffers
        sandBuffer?.Release();
        terrainShadowBuffer?.Release();
        shadowBuffer?.Release();
        constructionGridBuffer?.Release();
        constructionDataBuffer?.Release();
        randomSeedBuffer?.Release();
        changesBuffer?.Release();
    }
}

// ============ COMPUTE SHADER ============
/*
#pragma kernel ErodeGrains
#pragma kernel DepositGrains
#pragma kernel RunAvalanche

// Buffers
RWStructuredBuffer<float> sandHeights;
StructuredBuffer<float> terrainShadow;
StructuredBuffer<int> shadow;
StructuredBuffer<int> constructionGrid;
StructuredBuffer<ConstructionData> constructionData;
RWStructuredBuffer<uint> randomSeeds;
RWStructuredBuffer<CellChange> changes;

// Parámetros
int width, height;
int windDx, windDz;
float erosionHeight, depositeHeight;
int grainsPerStep;
int hopLength;
float slopeThreshold;
float pSand, pNoSand;

// Estructuras
struct ConstructionData
{
    float buildHeight;
    int active;
};

struct CellChange
{
    int x;
    int z;
    int changed;
};

// Función de random mejorada para GPU
uint rng_state;

uint rand_xorshift()
{
    rng_state ^= rng_state << 13;
    rng_state ^= rng_state >> 17;
    rng_state ^= rng_state << 5;
    return rng_state;
}

float rand_float()
{
    return float(rand_xorshift()) / 4294967296.0;
}

int rand_int(int min, int max)
{
    return min + int(rand_float() * (max - min));
}

// Función auxiliar para índices
int GetIndex(int x, int z)
{
    return z * width + x;
}

bool IsValidCell(int x, int z)
{
    return x >= 0 && x < width && z >= 0 && z < height;
}

// Erosión de granos
float ErodeGrain(int x, int z)
{
    int index = GetIndex(x, z);
    
    if (shadow[index] > 0 || terrainShadow[index] >= sandHeights[index] - erosionHeight * 0.1f)
        return 0.0f;
    
    float eroded = min(erosionHeight, sandHeights[index] - terrainShadow[index]);
    if (eroded > 0)
    {
        sandHeights[index] -= eroded;
    }
    
    return eroded;
}

// Deposición de granos
void DepositGrain(int x, int z, float amount)
{
    if (IsValidCell(x, z))
    {
        int index = GetIndex(x, z);
        sandHeights[index] += amount;
    }
}

// Algoritmo de deposición adaptado
void AlgorithmDeposit(int startX, int startZ, int dx, int dz, float depositeH, int threadId)
{
    int i = hopLength;
    int xCurr = startX;
    int zCurr = startZ;
    
    // Conteo inicial de terrain
    int countTerrain = 0;
    for (int j = 1; j <= i; j++)
    {
        int xAux = xCurr + j * dx;
        int zAux = zCurr + j * dz;
        
        if (IsValidCell(xAux, zAux))
        {
            if (terrainShadow[GetIndex(xAux, zAux)] >= sandHeights[GetIndex(xAux, zAux)])
                countTerrain++;
        }
    }
    
    int maxIterations = 1000; // Prevenir bucles infinitos
    int iterations = 0;
    
    while (iterations < maxIterations)
    {
        iterations++;
        
        // Comportamiento con estructuras (simplificado para GPU)
        int steps = max(abs(dx), abs(dz));
        if (steps > 0)
        {
            int stepX = dx / steps;
            int stepZ = dz / steps;
            
            for (int s = 1; s <= steps; s++)
            {
                int checkX = xCurr + s * stepX + dx;
                int checkZ = zCurr + s * stepZ + dz;
                
                if (IsValidCell(checkX, checkZ))
                {
                    int constructionId = constructionGrid[GetIndex(checkX, checkZ)];
                    
                    if (constructionId > 0 && constructionId < 1000)
                    {
                        ConstructionData currentConstruction = constructionData[constructionId];
                        
                        if (currentConstruction.active > 0)
                        {
                            int xPrev = checkX - dx;
                            int zPrev = checkZ - dz;
                            
                            if (IsValidCell(xPrev, zPrev))
                            {
                                float acumulacionBarlovento = terrainShadow[GetIndex(checkX, checkZ)] - 
                                                            sandHeights[GetIndex(xPrev, zPrev)];
                                
                                if (acumulacionBarlovento <= currentConstruction.buildHeight * 0.1f)
                                {
                                    DepositGrain(checkX, checkZ, depositeH);
                                    changes[threadId].x = checkX;
                                    changes[threadId].z = checkZ;
                                    changes[threadId].changed = 1;
                                    return;
                                }
                                else
                                {
                                    int stopX = xCurr + (s - 1) * stepX;
                                    int stopZ = zCurr + (s - 1) * stepZ;
                                    DepositGrain(stopX, stopZ, depositeH);
                                    changes[threadId].x = stopX;
                                    changes[threadId].z = stopZ;
                                    changes[threadId].changed = 1;
                                    return;
                                }
                            }
                        }
                    }
                }
            }
        }
        
        // Comportamiento de campo abierto
        xCurr += dx;
        zCurr += dz;
        
        if (!IsValidCell(xCurr, zCurr)) break;
        
        int currIndex = GetIndex(xCurr, zCurr);
        
        // Deposición en sombra
        if (shadow[currIndex] > 0 && sandHeights[currIndex] > terrainShadow[currIndex])
        {
            DepositGrain(xCurr, zCurr, depositeH);
            changes[threadId].x = xCurr;
            changes[threadId].z = zCurr;
            changes[threadId].changed = 1;
            return;
        }
        
        // Lógica de terrain
        if (terrainShadow[currIndex] >= sandHeights[currIndex] &&
            terrainShadow[currIndex] >= sandHeights[GetIndex(startX, startZ)])
        {
            countTerrain -= (terrainShadow[currIndex] >= sandHeights[currIndex]) ? 1 : 0;
            continue;
        }
        
        // Deposición lateral cuando hay mucho terrain
        if (countTerrain >= i - 1)
        {
            int dxLateral[2] = {-dz, dz};
            int dzLateral[2] = {dx, -dx};
            
            for (int j = 0; j < 2; j++)
            {
                for (int k = 1; k <= i; k++)
                {
                    int lx = xCurr + dxLateral[j] * k;
                    int lz = zCurr + dzLateral[j] * k;
                    
                    if (IsValidCell(lx, lz))
                    {
                        int lIndex = GetIndex(lx, lz);
                        float heightL = max(terrainShadow[lIndex], sandHeights[lIndex]);
                        float heightCurr = max(terrainShadow[currIndex], sandHeights[currIndex]);
                        
                        if (heightL < heightCurr - slopeThreshold)
                        {
                            DepositGrain(lx, lz, depositeH);
                            changes[threadId].x = lx;
                            changes[threadId].z = lz;
                            changes[threadId].changed = 1;
                            return;
                        }
                    }
                }
            }
        }
        
        countTerrain -= (terrainShadow[currIndex] >= sandHeights[currIndex]) ? 1 : 0;
        
        // Lógica de hop
        if (--i <= 0)
        {
            float prob = (sandHeights[currIndex] > terrainShadow[currIndex]) ? pSand : pNoSand;
            
            if (rand_float() < prob)
            {
                DepositGrain(xCurr, zCurr, depositeH);
                changes[threadId].x = xCurr;
                changes[threadId].z = zCurr;
                changes[threadId].changed = 1;
                return;
            }
            i = hopLength;
        }
    }
}

[numthreads(64, 1, 1)]
void ErodeGrains(uint3 id : SV_DispatchThreadID)
{
    if ((int)id.x >= grainsPerStep) return;
    
    // Inicializar random state
    rng_state = randomSeeds[id.x] + id.x;
    
    // Seleccionar posición aleatoria
    int x = rand_int(0, width);
    int z = rand_int(0, height);
    
    // Erosionar grano
    float depositeH = ErodeGrain(x, z);
    
    if (depositeH > 0.0f)
    {
        // Ejecutar algoritmo de deposición
        AlgorithmDeposit(x, z, windDx, windDz, depositeH, (int)id.x);
    }
    
    // Actualizar semilla para próximo uso
    randomSeeds[id.x] = rng_state;
}

[numthreads(64, 1, 1)]
void DepositGrains(uint3 id : SV_DispatchThreadID)
{
    // Esta función se ejecuta después de ErodeGrains
    // Los cambios ya están aplicados en AlgorithmDeposit
}

[numthreads(64, 1, 1)]
void RunAvalanche(uint3 id : SV_DispatchThreadID)
{
    int totalCells = width * height;
    if ((int)id.x >= totalCells) return;
    
    int x = (int)id.x % width;
    int z = (int)id.x / width;
    int index = GetIndex(x, z);
    
    float currentHeight = sandHeights[index];
    float terrainHeight = terrainShadow[index];
    
    // Verificar estabilidad con vecinos
    float maxSlope = 0.0f;
    int bestX = x, bestZ = z;
    
    for (int dx = -1; dx <= 1; dx++)
    {
        for (int dz = -1; dz <= 1; dz++)
        {
            if (dx == 0 && dz == 0) continue;
            
            int nx = x + dx;
            int nz = z + dz;
            
            if (IsValidCell(nx, nz))
            {
                int nIndex = GetIndex(nx, nz);
                float neighborHeight = max(sandHeights[nIndex], terrainShadow[nIndex]);
                float currentTotalHeight = max(currentHeight, terrainHeight);
                float slope = currentTotalHeight - neighborHeight;
                
                if (slope > slopeThreshold && slope > maxSlope)
                {
                    maxSlope = slope;
                    bestX = nx;
                    bestZ = nz;
                }
            }
        }
    }
    
    // Realizar avalancha si es necesario
    if (maxSlope > slopeThreshold && currentHeight > terrainHeight)
    {
        float transfer = min(maxSlope * 0.1f, currentHeight - terrainHeight);
        
        if (transfer > 0.001f)
        {
            sandHeights[index] -= transfer;
            int bestIndex = GetIndex(bestX, bestZ);
            sandHeights[bestIndex] += transfer;
        }
    }
}
*/