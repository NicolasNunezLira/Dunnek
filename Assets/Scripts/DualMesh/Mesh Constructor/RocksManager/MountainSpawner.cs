using System;
using DunefieldModel_DualMesh;
using UnityEngine;
using Utils;
using Random = UnityEngine.Random;

public class MountainPrefabSpawner : Singleton<MountainPrefabSpawner>
{
    #region Variables
    public GameObject[] mountainPrefabs;

    public int seed = 42;

    public float rockThreshold = 0.75f;

    public float scale = 10f;

    [Tooltip("Parent for rock elements")]
    public Transform rockTransform;

    private int xDOF, zDOF;
    private float size;
    NativeGrid terrain, terrainShadow;

    private bool[,] mountainElements;
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
        xDOF = DualMesh.Instance.xResolution + 1;
        zDOF = DualMesh.Instance.zResolution + 1;
        size = DualMesh.Instance.size;
        terrain = DualMesh.Instance.terrain;
        terrainShadow = DualMesh.Instance.terrainShadow;

        Random.InitState(seed);

        mountainElements = new bool[xDOF, zDOF];

        for (int x = 0; x < xDOF; x++)
        {
            for (int z = 0; z < zDOF; z++)
            {
                if (mountainElements[x, z]) continue;
                float height = terrain[x, z];
                float nx = (float)x / xDOF;
                float nz = (float)z / zDOF;
                float noise = Mathf.PerlinNoise(nx * scale + seed, nz * scale + seed);

                Vector3 pos = new Vector3(
                    nx * size,
                    height * 0.9f,
                    nz * size);

                if (noise > rockThreshold)
                {
                    PlaceRandomPrefab(mountainPrefabs, pos);
                }
            }
        }
    }

    void PlaceRandomPrefab(GameObject[] prefabs, Vector3 position)
    {
        if (prefabs.Length == 0) return;

        GameObject prefab = prefabs[Random.Range(0, prefabs.Length)];
        Quaternion rot = Quaternion.Euler(0, Random.Range(0f, 360f), 0);

        GameObject temp = Instantiate(prefab, position, rot);
        temp.transform.localScale *= Random.Range(0.4f, 0.8f);
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
                if (mountainElements[x, z])
                {
                    Destroy(temp);
                    return;
                }
            }
        }

        temp.transform.parent = rockTransform;
        temp.SetActive(true);

        UpdateTerrainHeight(xMin, xMax, zMin, zMax);
    }

    #endregion

    #region Update terrain
    private void UpdateTerrainHeight(int xMin, int xMax, int zMin, int zMax)
    {
        float rayHeight = 50f; // altura desde la que lanzas los rayos
        LayerMask mountainLayer = LayerMask.GetMask("Mountains"); // capa de montañas

        for (int x = Math.Max(xMin - 1, 0); x < Math.Min(xDOF, xMax); x++)
        {
            for (int z = Math.Max(zMin - 1, 0); z < Math.Min(zDOF, zMax + 1); z++)
            {
                Vector3 rayOrigin = new Vector3((x * size) / xDOF, rayHeight, (z * size) / zDOF);
                Ray ray = new Ray(rayOrigin, Vector3.down);

                if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, mountainLayer))
                {
                    terrainShadow[x, z] = hit.point.y;
                    mountainElements[x, z] = true;
                }
            }
        }
    }
    #endregion
}