using System.Collections.Generic;
using DunefieldModel_DualMesh;
using UnityEngine;
using Utils;

public class DesertPrefabSpawner : Singleton<DesertPrefabSpawner>
{
    #region Variables
    public GameObject[] rockPrefabs;
    public GameObject[] treePrefabs;
    public GameObject[] bushPrefabs;

    public int seed = 42;

    public float treeThreshold = 0.6f;
    public float rockThreshold = 0.75f;

    public float scale = 10f;

    [Tooltip("Parent for rocks")]
    public Transform rockParent;
    public Transform treeParent;
    public Transform bushParent;

    private int simXDOF, simZDOF;
    private float size;
    NativeGrid sand, terrain;

    public enum DesertElement
    {
        Rock, Tree, Bush
    }

    private Dictionary<DesertElement, Transform> parent => new Dictionary<DesertElement, Transform>
    {
        {DesertElement.Rock, rockParent},
        {DesertElement.Tree, treeParent},
        {DesertElement.Bush, bushParent}
    };

    private Dictionary<DesertElement, List<float>> randomExtremes = new Dictionary<DesertElement, List<float>> {
        {DesertElement.Rock, new List<float> {0.05f, 0.08f} },
        {DesertElement.Tree, new List<float> {0.08f, 0.12f} },
        {DesertElement.Bush, new List<float> {0.1f, 0.2f}}
    };
    #endregion

    #region Awake
    protected override void Awake()
    {
        base.Awake();


    }
    #endregion

    #region Spawn
    public void Spawn()
    {
        simXDOF = DualMesh.Instance.simXResolution;
        simZDOF = DualMesh.Instance.simZResolution;
        size = DualMesh.Instance.size;
        sand = DualMesh.Instance.sand;
        terrain = DualMesh.Instance.terrain;

        Random.InitState(seed);

        bool[,] occupied = new bool[simXDOF, simZDOF];

        for (int x = 0; x < simXDOF; x++)
        {
            for (int z = 0; z < simZDOF; z++)
            {
                if (occupied[x, z]) continue;
                float height = Mathf.Max(terrain[x, z], sand[x, z]);
                float nx = (float)x / simXDOF;
                float nz = (float)z / simZDOF;
                float noise = Mathf.PerlinNoise(nx * scale + seed, nz * scale + seed);

                Vector3 pos = new Vector3(nx * size, height, nz * size);

                if (noise > rockThreshold)
                {
                    PlaceRandomPrefab(rockPrefabs, pos, occupied, DesertElement.Rock);
                }
                else if (noise > treeThreshold)
                {
                    if (Random.value > 0.5)
                        PlaceRandomPrefab(treePrefabs, pos, occupied, DesertElement.Tree);
                    else
                        PlaceRandomPrefab(bushPrefabs, pos, occupied, DesertElement.Bush);
                }
            }
        }
    }

    void PlaceRandomPrefab(
        GameObject[] prefabs,
        Vector3 position,
        bool[,] occupiedNodes,
        DesertElement element
    )
    {
        if (prefabs.Length == 0) return;

        GameObject prefab = prefabs[Random.Range(0, prefabs.Length)];
        Quaternion rot = Quaternion.Euler(0, Random.Range(0f, 360f), 0);

        GameObject obj = Instantiate(prefab, position, rot, parent[element]);

        float scaleRand = Random.Range(randomExtremes[element][0], randomExtremes[element][1]);
        obj.transform.localScale *= scaleRand;

        Collider col = obj.GetComponentInChildren<Collider>();
        if (col == null)
        {
            Debug.LogWarning("Prefab sin collider: " + prefab.name);
            return;
        }

        Bounds bounds = col.bounds;

        // Convertir límites a índice de grilla
        int minX = Mathf.Clamp(Mathf.RoundToInt(bounds.min.x / size * (simXDOF - 1)), 0, simXDOF - 1);
        int maxX = Mathf.Clamp(Mathf.RoundToInt(bounds.max.x / size * (simXDOF - 1)), 0, simXDOF - 1);
        int minZ = Mathf.Clamp(Mathf.RoundToInt(bounds.min.z / size * (simZDOF - 1)), 0, simZDOF - 1);
        int maxZ = Mathf.Clamp(Mathf.RoundToInt(bounds.max.z / size * (simZDOF - 1)), 0, simZDOF - 1);

        // Usar raycasts para verificar nodos realmente ocupados
        for (int x = minX; x <= maxX; x++)
        {
            for (int z = minZ; z <= maxZ; z++)
            {
                Vector3 rayOrigin = new Vector3(
                    (float)x / (simXDOF - 1) * size,
                    bounds.max.y + 1f, // un poco por encima del objeto
                    (float)z / (simZDOF - 1) * size
                );

                if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, Mathf.Infinity))
                {
                    if (hit.collider != null && hit.collider.transform.IsChildOf(obj.transform))
                    {
                        occupiedNodes[x, z] = true;
                    }
                }
            }
        }

        if (element != DesertElement.Rock)
        {
            VegetationManager.AddVegetation(
                element,
                obj
            );
        }
    }
    #endregion
}