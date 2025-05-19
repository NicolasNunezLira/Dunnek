using UnityEngine;

public class DuneGenerator : MonoBehaviour
{
    [SerializeField]
    public GameObject tilePrefab;
    public int width = 100;
    public int height = 100;
    public float tileSize = 1f;
    public float depositeHeight = 0.2f;
    public float erosionHeight = 0.1f;
    public int grainsPerStep = 5000;

    public Vector2Int windDirection = new Vector2Int(1, 0);
    [Header("Perlin Noise Layers")]
    public float scale1 = 0.01f;
    public float amplitude1 = 5f;
    public float scale2 = 0.05f;
    public float amplitude2 = 2f;
    public float scale3 = 0.15f;
    public float amplitude3 = 1f;

    private DuneCell[,] duneGrid;
    private bool[,] isShadowed;

    void Start()
    {
        GenerateGrid();
        InvokeRepeating(nameof(SimulateWerner), 0.05f, 0.05f);
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
            }
        }
    }

    void SimulateWerner()
    {
        isShadowed = new bool[width, height];

        for (int i = 0; i < grainsPerStep; i++)
        {
            int x = Random.Range(1, width - 1);
            int z = Random.Range(1, height - 1);

            Vector2Int pos = new Vector2Int(x, z);
            Vector2Int down = GetDownslope(pos);
            Vector2Int up = GetUpslope(pos);

            if (IsInBounds(down))
            {
                duneGrid[down.x, down.y].height += depositeHeight;
                duneGrid[pos.x, pos.y].height -= erosionHeight;

                MarkShadowAlongWind(down);
            }
        }

        UpdateTiles();
    }

    void MarkShadowAlongWind(Vector2Int from)
    {
        Vector2Int pos = from + windDirection;
        while (IsInBounds(pos))
        {
            isShadowed[pos.x, pos.y] = true;
            pos += windDirection;
        }
    }

    Vector2Int GetDownslope(Vector2Int center)
    {
        Vector2Int steepest = center;
        float h = duneGrid[center.x, center.y].height;
        float maxDrop = 0;

        Vector2Int[] dirs = {
            new Vector2Int(0, 1),  // down
            new Vector2Int(1, 0),  // right
            new Vector2Int(-1, 0), // left
            new Vector2Int(0, -1)  // up
        };

        foreach (var d in dirs)
        {
            Vector2Int neighbor = center + d;
            if (!IsInBounds(neighbor)) continue;

            float diff = h - duneGrid[neighbor.x, neighbor.y].height;
            if (diff >= 0.2f && diff > maxDrop)
            {
                maxDrop = diff;
                steepest = neighbor;
            }
        }

        return steepest;
    }

    Vector2Int GetUpslope(Vector2Int center)
    {
        Vector2Int upslope = center;
        float h = duneGrid[center.x, center.y].height;
        float maxRise = 0;

        Vector2Int[] dirs = {
            new Vector2Int(0, 1),  // down
            new Vector2Int(1, 0),  // right
            new Vector2Int(-1, 0), // left
            new Vector2Int(0, -1)  // up
        };

        foreach (var d in dirs)
        {
            Vector2Int neighbor = center + d;
            if (!IsInBounds(neighbor)) continue;

            float diff = duneGrid[neighbor.x, neighbor.y].height - h;
            if (diff > maxRise)
            {
                maxRise = diff;
                upslope = neighbor;
            }
        }

        return upslope;
    }

    void UpdateTiles()
    {
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                bool shadow = isShadowed != null && isShadowed[x, z];
                duneGrid[x, z].UpdateVisual(tileSize, shadow);
            }
        }
    }

    bool IsInBounds(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < width && pos.y >= 0 && pos.y < height;
    }

    float GetMultiScalePerlinHeight(int x, int z)
{
    float h1 = Mathf.PerlinNoise(x * scale1, z * scale1) * amplitude1;
    float h2 = Mathf.PerlinNoise(x * scale2, z * scale2) * amplitude2;
    float h3 = Mathf.PerlinNoise(x * scale3, z * scale3) * amplitude3;
    return h1 + h2 + h3;
}
}
