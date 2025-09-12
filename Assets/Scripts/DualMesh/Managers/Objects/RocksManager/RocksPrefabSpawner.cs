using System.Collections.Generic;
using DunefieldModel_DualMesh;
using UnityEngine;
using Utils;

public class RockPrefabSpawner : Singleton<RockPrefabSpawner>
{
    #region Variables
    public GameObject[] archPrefabs;
    public GameObject[] boulderPrefabs;
    public GameObject[] cliffPrefabs;
    public GameObject[] highCliffPrefabs;
    public GameObject[] rockPrefabs;
    public GameObject[] rubblePrefabs;

    public int seed = 42;

    public float rockThreshold = 0.75f;

    public float scale = 10f;

    [Tooltip("Parent for rock elements")]
    public Transform rockTransform;

    private int xDOF, zDOF;
    private float size;
    NativeGrid sand, terrain;

    public enum RockType
    {
        None = 0, Arch, Boulder, Cliff, HighCliff, Rock, Rubble
    }

    private Dictionary<RockType, List<float>> randomExtremes = new Dictionary<RockType, List<float>>
    {
        { RockType.Arch, new List<float> {0.05f, 0.12f}},
        { RockType.Boulder, new List<float> {0.2f, 0.25f}},
        { RockType.Cliff, new List<float> {0.2f, 0.25f}},
        { RockType.HighCliff, new List<float> {0.02f, 0.05f}},
        { RockType.Rock, new List<float> {0.2f, 0.25f}},
        { RockType.Rubble, new List<float> {0.2f, 0.25f}}
    };

    public RockType[,] rockElements;

    private Dictionary<RockType, GameObject[]> typeDict;
    #endregion

    #region Awake
    protected override void Awake()
    {
        base.Awake();

        typeDict = new Dictionary<RockType, GameObject[]>
        {
            {RockType.Arch, archPrefabs},
            {RockType.Boulder, boulderPrefabs},
            {RockType.Cliff, cliffPrefabs},
            {RockType.HighCliff, highCliffPrefabs},
            {RockType.Rock, rockPrefabs},
            {RockType.Rubble, rubblePrefabs}
        };
    }
    #endregion

    #region Spawn
    public void Spawn()
    {
        xDOF = DualMesh.Instance.xResolution + 1;
        zDOF = DualMesh.Instance.zResolution + 1;
        size = DualMesh.Instance.size;
        sand = DualMesh.Instance.sand;
        terrain = DualMesh.Instance.terrain;

        Random.InitState(seed);

        rockElements = new RockType[xDOF, zDOF];

        for (int x = 0; x < xDOF; x++)
        {
            for (int z = 0; z < zDOF; z++)
            {
                if (rockElements[x, z] != 0) continue;
                float height = terrain[x, z];
                float nx = (float)x / xDOF;
                float nz = (float)z / zDOF;
                float noise = Mathf.PerlinNoise(nx * scale + seed, nz * scale + seed);

                Vector3 pos = new Vector3(nx * size, height, nz * size);

                if (noise > rockThreshold)
                {
                    RockType randomType = (RockType)Random.Range(1, System.Enum.GetValues(typeof(RockType)).Length);
                    PlaceRandomPrefab(typeDict[randomType], pos, randomType);
                }
            }
        }

        void PlaceRandomPrefab(GameObject[] prefabs, Vector3 position, RockType element)
        {
            if (prefabs.Length == 0) return;

            GameObject prefab = prefabs[Random.Range(0, prefabs.Length)];
            Quaternion rot = Quaternion.Euler(0, Random.Range(0f, 360f), 0);

            GameObject temp = Instantiate(prefab, position, rot);
            float scaleRand = Random.Range(randomExtremes[element][0], randomExtremes[element][1]);
            temp.transform.localScale *= scaleRand;
            temp.SetActive(false);

            Bounds bounds = temp.GetComponentInChildren<Renderer>().bounds;

            float cellSize = size / (xDOF - 1);

            int xMin = Mathf.Clamp(Mathf.FloorToInt(bounds.min.x / cellSize), 0, xDOF - 1);
            int xMax = Mathf.Clamp(Mathf.CeilToInt(bounds.max.x / cellSize), 0, xDOF - 1);
            int zMin = Mathf.Clamp(Mathf.FloorToInt(bounds.min.z / cellSize), 0, zDOF - 1);
            int zMax = Mathf.Clamp(Mathf.CeilToInt(bounds.max.z / cellSize), 0, zDOF - 1);

            for (int x = xMin; x <= xMax; x++)
            {
                for (int z = zMin; z <= zMax; z++)
                {
                    if (rockElements[x, z] != RockType.None)
                    {
                        Destroy(temp);
                        return;
                    }
                }
            }

            temp.transform.parent = rockTransform;
            temp.SetActive(true);

            var id = temp.GetComponent<RockIdentifier>();
            if (id == null) id = temp.AddComponent<RockIdentifier>();
            id.element = element;

        
            for (int x = xMin; x <= xMax; x++)
            {
                for (int z = zMin; z <= zMax; z++)
                {
                    rockElements[x, z] = element;
                }
            }
        }

        #endregion
    }
}