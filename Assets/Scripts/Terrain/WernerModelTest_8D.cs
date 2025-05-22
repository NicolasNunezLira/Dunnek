using UnityEngine;
using DunefieldModel_8D;
using Unity.Mathematics;

public class WernerModelTest_8D : MonoBehaviour
{
    [SerializeField]
    public GameObject tilePrefab;
    public int width = 100;
    public int height = 100;
    public float tileSize = 1f;
    public float depositeHeight = 0.1f;

    public float erosionHeight = 0.1f;

    public float slope = 0.2f;

    public Vector2 dir = new Vector2(1, 0);

    public int grainsPerStep = 5000;

    //public Vector2Int windDirection = new Vector2Int(1, 0);
    [Header("Perlin Noise Layers")]
    public float scale1 = 0.01f;
    public float amplitude1 = 5f;
    public float scale2 = 0.05f;
    public float amplitude2 = 2f;
    public float scale3 = 0.15f;
    public float amplitude3 = 1f;

    private DuneCell[,] duneGrid;
    //private bool[,] isShadowed;
    private Model8D duneModel;
    private FindSlopeMooreDeterministic slopeFinder;

    void Start()
    {
        float[,] Elev = new float[width, height];
        GenerateGrid();
        int dx = (int)dir.x;
        int dy = (int)dir.y;
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                Elev[x, z] = duneGrid[x, z].height;
            }
        }
        slopeFinder = new FindSlopeMooreDeterministic();
        duneModel = new Model8D(slopeFinder, Elev, width, height, slope, dx, dy);
        duneModel.SetOpenEnded(true);
        duneModel.UsesSandProbabilities();
        duneModel.shadowInit();


        //InvokeRepeating(nameof(Update), 0f, updateInterval);
    }

    void GenerateGrid()
    {
        duneGrid = new DuneCell[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                float heightValue = GetMultiScalePerlinHeight(x, z);
                Vector3 pos = new Vector3(x * tileSize, 0f, z * tileSize);
                GameObject tile = Instantiate(tilePrefab, pos, Quaternion.identity, transform);
                tile.transform.localScale = new Vector3(tileSize, 0.1f, tileSize);

                duneGrid[x, z] = new DuneCell(tile, heightValue);
                //Elev[x, z] = heightValue;

                duneGrid[x, z].UpdateVisual(tileSize);
            }
        }
    }

    void Update()
    {
        duneModel.Tick(grainsPerStep, (int)dir.x, (int)dir.y, erosionHeight, depositeHeight);

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                duneGrid[x, z].height = duneModel.Elev[x, z];
                duneGrid[x, z].shadow = duneModel.Shadow[x, z];
                duneGrid[x, z].UpdateVisual(tileSize);
            }
        }

    }


    float GetMultiScalePerlinHeight(int x, int z)
    {
        float h1 = Mathf.PerlinNoise(x * scale1, z * scale1) * amplitude1;
        float h2 = Mathf.PerlinNoise(x * scale2, z * scale2) * amplitude2;
        float h3 = Mathf.PerlinNoise(x * scale3, z * scale3) * amplitude3;
        return h1 + h2 + h3;
    }
}
