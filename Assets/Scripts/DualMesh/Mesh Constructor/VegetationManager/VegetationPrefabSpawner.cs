using System.Collections.Generic;
using DunefieldModel_DualMesh;
using UnityEngine;
using Utils;
using Unity.Mathematics;
using Random = UnityEngine.Random;

public class VegetationManager : Singleton<VegetationManager>
{
    #region Variables
    public GameObject[] treePrefabs;
    public GameObject[] bushPrefabs;

    public int seed = 42;

    public float treeThreshold = 0.6f;

    public float scale = 10f;

    [Tooltip("Parent for desert elements")]
    public Transform treeParent;
    public Transform bushParent;

    private int xDOF, zDOF;
    private float size;
    NativeGrid sand, terrain;

    public enum VegetationType
    {
        None = 0, Tree, Bush
    }

    private Dictionary<VegetationType, Transform> parent => new Dictionary<VegetationType, Transform>
    {
        {VegetationType.Tree, treeParent},
        {VegetationType.Bush, bushParent}
    };

    private Dictionary<VegetationType, List<float>> randomExtremes = new Dictionary<VegetationType, List<float>> {
        {VegetationType.Tree, new List<float> {0.01f, 0.08f} },
        {VegetationType.Bush, new List<float> {0.05f, 0.1f}}
    };

    public VegetationGrid vegetationGrid;
    public int currentID = 0;
    #endregion

    #region Awake
    protected override void Awake()
    {
        base.Awake();
        currentID = 0;
        vegetationGrid = new VegetationGrid();
    }
    #endregion

    #region Spawn
    public void Spawn()
    {
        xDOF = DualMesh.Instance.xResolution + 1;
        zDOF = DualMesh.Instance.zResolution + 1;
        size = DualMesh.Instance.size;
        sand = DualMesh.Instance.sand;
        terrain = DualMesh.Instance.terrainShadow;

        Random.InitState(seed);

        for (int x = 0; x < xDOF; x++)
        {
            for (int z = 0; z < zDOF; z++)
            {
                if (vegetationGrid.HasVegetation(x, z) || sand[x, z] >= terrain[x, z] + 0.2f) continue;
                float height = terrain[x, z];
                float nx = (float)x / xDOF;
                float nz = (float)z / zDOF;
                float noise = Mathf.PerlinNoise(nx * scale + seed, nz * scale + seed);

                float biasX = terrain[x, z] > sand[x, z] ? 0 : Random.Range(-0.5f, 0.5f);
                float biasZ = terrain[x, z] > sand[x, z] ? 0 : Random.Range(-0.5f, 0.5f);
                Vector3 pos = new Vector3(nx * size + biasX, height, nz * size + biasZ);

                if (noise > treeThreshold)
                {
                    if (Random.value > 0.5)
                    {
                        PlaceRandomPrefab(treePrefabs, pos, VegetationType.Tree, new int2(x, z));
                    }
                    else
                    {
                        PlaceRandomPrefab(bushPrefabs, pos, VegetationType.Bush, new int2(x, z));
                    }
                }
            }
        }
    }

    public void PlaceRandomPrefab(
        GameObject[] prefabs,
        Vector3 position,
        VegetationType element,
        int2 gridPos
    )
    {
        if (prefabs.Length == 0) return;

        GameObject prefab = prefabs[Random.Range(0, prefabs.Length)];
        Quaternion rot = Quaternion.Euler(0, Random.Range(0f, 360f), 0);

        GameObject obj = Instantiate(prefab, position, rot, parent[element]);

        float scaleRand = Random.Range(randomExtremes[element][0], randomExtremes[element][1]);
        obj.transform.localScale *= scaleRand;

        var id = obj.GetComponent<VegetationIdentifier>();
        if (id == null) id = obj.AddComponent<VegetationIdentifier>();
        id.element = element;

        VegetationData veg = new VegetationData(currentID, element, obj, gridPos);
        vegetationGrid.AddVegetation(gridPos.x, gridPos.y , veg);
    }
    #endregion

    #region VegetationGrid
    public class VegetationGrid
    {
        public Dictionary<int2, VegetationData> data;

        public VegetationGrid(bool initialize = true)
        {
            data = initialize ? new Dictionary<int2, VegetationData>() : null;
        }

        public void AddVegetation(int x, int z, VegetationData vegData)
        {
            int2 cell = new int2(x, z);
            data[cell] = vegData;
        }

        public bool HasVegetation(int x, int z)
        {
            int2 cell = new int2(x, z);
            return data != null && data.ContainsKey(cell);
        }

         public VegetationData? GetVegetation(int x, int z)
        {
            if (HasVegetation(x, z))
            {
                int2 cell = new int2(x, z);
                return data[cell];
            }
            return null;
        }

        public bool AffectsErosion(int x, int z)
        {
            int2 cell = new int2(x, z);
            VegetationData? veg = GetVegetation(x, z);
            if (veg == null)
            {
                return false;
            }
            else 
            {
                return veg.Value.affectsErosion;
            }
        }

        public bool AffectsWind(int x, int z)
        {
            int2 cell = new int2(x, z);
            VegetationData? veg = GetVegetation(x, z);
            if (veg == null)
            {
                return false;
            }
            else 
            {
                return veg.Value.affectsWind;
            }
        }

        public void TryDestroy(int x, int z)
        {
            VegetationData? veg = GetVegetation(x, z);

            if (veg == null) return;

            Bounds bounds = veg.Value.go.GetComponent<Collider>().bounds;

            if (DualMesh.Instance.sand[x, z] >= (bounds.min.y + bounds.max.y) * 0.8f) RemoveVegetation(x, z);
        }

        public void RemoveVegetation(int x, int z)
        {
            if (HasVegetation(x, z))
            {
                int2 cell = new int2(x, z);
                if (data[cell].go != null)
                {
                    GameObject.Destroy(data[cell].go);
                }

                data.Remove(cell);
            }
        }
    }

    public struct VegetationData
    {
        public int id;
        public VegetationType type;
        public GameObject go;
        public int2 cell;

        public VegetationData(int id, VegetationType type, GameObject go, int2 cell)
        {
            this.id = id;
            this.type = type;
            this.go = go;
            this.cell = cell;
        }

        public bool affectsWind => type == VegetationType.Tree;
        public bool affectsErosion => type != VegetationType.None;
    }
    #endregion
}