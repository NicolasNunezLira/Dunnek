using UnityEngine;
using DunefieldModel_DualMeshJobs;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;
using UnityEditor;
using UnityEditor.EditorTools;
using DunefieldModel;
using System.Linq;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class DualMeshJob : MonoBehaviour
{
    [Header("Plane Settings")]

    [Tooltip("The number of subdivisions along each axis.")]
    [Range(31, 511)]
    public int xResolution = 127;
    [Range(31, 511)]
    public int zResolution = 127;

    [Header("Mesh Settings")]
    [Tooltip("The size of the plane in world units.")]
    public float size = 10f;

    [Header("Terrain Settings")]
    [Tooltip("Meterial.")]
    public Material terrainMaterial;

    [Tooltip("Perlin noise parameters.")]
    public float terrainScale1 = 0.003f;
    public float terrainAmplitude1 = 8f;
    public float terrainScale2 = 0.01f;
    public float terrainAmplitude2 = 4f;
    public float terrainScale3 = 0.05f;
    public float terrainAmplitude3 = 2f;

    [Header("Sand Settings")]
    [Tooltip("Meterial.")]
    public Material sandMaterial;

    [Tooltip("Perlin noise parameters.")]
    public float sandScale1 = 0.02f;
    public float sandAmplitude1 = 1.5f;
    public float sandScale2 = 0.06f;
    public float sandAmplitude2 = 0.7f;
    public float sandScale3 = 0.1f;
    public float sandAmplitude3 = 0.3f;

    public NativeArray<float> terrain, sand, shadow;

    [Header("Simulation Settings")]
    [Tooltip("The height variation of the terrain.")]
    public float heightVariation = 0.1f;

    [Tooltip("Probabilities for deposition.")]
    public float pSand = 0.6f;
    public float pNoSand = 0.4f;

    [Tooltip("The slope of the terrain.")]
    public float slope = 0.2f;

    [Tooltip("The slope for avalanches.")]
    public float avalancheSlope = .5f;
    public float criticalSlopeThreshold = 2f;

    [Tooltip("Amount of avalanche process per grain")]
    public int iter = 5;

    [Tooltip("The hop length for the simulation.")]
    public int hopLength = 1;

    [Tooltip("Shadow slope for the simulation.")]
    public float shadowSlope = 0.803847577f; // 3 * tan(15 degrees) ~ 0.803847577f

    [Tooltip("The direction of the wind.")]
    public Vector2 windDirection = new Vector2(1, 0);
    [Tooltip("The number of grains per step.")]
    public int grainsPerStep = 500;

    private GameObject terrainGO, sandGO;

    private NativeList<SandChanges> sandChanges;
    private List<ShadowChanges> shadowChanges;
    private NativeArray<Unity.Mathematics.Random> rngArray;

    private DualMeshConstructor dualMeshConstructor;
    

    void Start()
    {
        // Initialize the terrain and sand meshes
        dualMeshConstructor = new DualMeshConstructor(xResolution, zResolution, size, terrainScale1, terrainScale2, terrainScale3, terrainAmplitude1, terrainAmplitude2, terrainAmplitude3,
            sandScale1, sandScale2, sandScale3, sandAmplitude1, sandAmplitude2, sandAmplitude3, terrainMaterial, sandMaterial, this.transform);

        dualMeshConstructor.Initialize(out terrainGO, out sandGO, out terrain, out sand);
        sandGO.GetComponent<MeshFilter>().mesh.MarkDynamic();

        // Initialize the shadow
        if (!shadow.IsCreated)
        {
            shadow = new NativeArray<float>(xResolution * zResolution, Allocator.Persistent);
        }

        for (int i = 0; i < shadow.Length; i++)
        {
            shadow[i] = 0;
        }

        shadowChanges = new List<ShadowChanges>();
        ModelDMJ.ShadowInit(
            (int)windDirection.x, (int)windDirection.y,
            sand, terrain,
            xResolution, zResolution,
            shadow, shadowSlope, ref shadowChanges
        );

        ApplyShadowChanges(ref shadow, shadowChanges);
        shadowChanges.Clear();



        // Initialize the list for parallel changes
        sandChanges = new NativeList<SandChanges>(Allocator.Persistent);
        sandChanges.Capacity = grainsPerStep * 3;
        //shadowChanges = new NativeList<ShadowChanges>(Allocator.Persistent);

        // Initialize randoms
        rngArray = new NativeArray<Unity.Mathematics.Random>(10000, Allocator.Persistent);

        uint seed = (uint)UnityEngine.Random.Range(1, int.MaxValue);
        for (int i = 0; i < 10000; i++)
        {
            // Es importante que cada Random tenga una semilla única
            rngArray[i] = new Unity.Mathematics.Random(seed + (uint)i + 1);
        }
    }

    void Update()
    {
        NativeArray<int> randomsX = new NativeArray<int>(grainsPerStep, Allocator.TempJob);
        NativeArray<int> randomsZ = new NativeArray<int>(grainsPerStep, Allocator.TempJob);

        // Llenar randomIndices con valores únicos aleatorios entre 0 y totalNodes-1
        HashSet<(int, int)> usedPairs = new HashSet<(int, int)>();
        System.Random rnd = new System.Random();

        int filled = 0;
        while (filled < grainsPerStep)
        {
            int x = rnd.Next(0, xResolution);
            int z = rnd.Next(0, zResolution);

            if (!usedPairs.Contains((x,z)))
            {
                usedPairs.Add((x,z));
                randomsX[filled] = x;
                randomsZ[filled] = z;
                filled++;
            }
        }

        
        var sandJob = new DuneFieldSimulation
        {
            randomsX = randomsX,
            randomsZ = randomsZ,
            sandChanges = sandChanges.AsParallelWriter(),
            sand = sand,
            terrain = terrain,
            shadow = shadow,
            xResolution = xResolution,
            zResolution = zResolution,
            size = size,
            depositeHeight = heightVariation,
            erosionHeight = heightVariation,
            slope = slope,
            shadowSlope = shadowSlope,
            avalancheSlope = avalancheSlope,
            slopeThreshold = criticalSlopeThreshold,
            dx = (int)windDirection.x,
            dz = (int)windDirection.y,
            HopLength = hopLength,
            pSand = pSand,
            pNoSand = pNoSand,
            iter = iter,
            rng = rngArray,
            openEnded = false,
            verbose = false
        };

        JobHandle handle = sandJob.Schedule(grainsPerStep, 64);
        handle.Complete();


        //randomsX.Dispose();
        //randomsZ.Dispose();


        // Aplicar cambios de la arena erosionada
        ApplySandChanges(ref sand, sandChanges);
        sandChanges.Clear();

        dualMeshConstructor.ApplyHeightMapToMesh(
            sandGO.GetComponent<MeshFilter>().mesh,
            sand,
            xResolution, zResolution
        );

        // Actualizar sombra
        shadowChanges = new List<ShadowChanges>();
        ModelDMJ.ShadowInit(
            (int)windDirection.x, (int)windDirection.y,
            sand, terrain,
            xResolution, zResolution,
            shadow, shadowSlope, ref shadowChanges
        );
        ApplyShadowChanges(ref shadow, shadowChanges);
        shadowChanges.Clear();
    }

    void OnDestroy()
    {
        sand.Dispose();
        terrain.Dispose();
        shadow.Dispose();
        sandChanges.Dispose();
        rngArray.Dispose();
    }

    private void ApplyShadowChanges(ref NativeArray<float> shadow, List<ShadowChanges> shadowChanges)
    {
        foreach (ShadowChanges change in shadowChanges)
        {
            shadow[change.index] = change.value;
        }
    }

    private void ApplySandChanges(ref NativeArray<float> sand, NativeList<SandChanges> sandChanges)
    {
        foreach (SandChanges change in sandChanges)
        {
            if (change.index < 0 || change.index >= sand.Length)
            {
                Debug.LogWarning($"Invalid index: {change.index}. Array length: {sand.Length}");
                continue; // evitar crash
            }
            sand[change.index] += change.delta;
        }
    }

}
