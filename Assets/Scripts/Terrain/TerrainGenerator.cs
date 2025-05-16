using UnityEngine;

public class DuneGenerator : MonoBehaviour
{
    public GameObject tilePrefab;
    public int width = 100;
    public int height = 100;
    public float tileSize = 1f;
    public float reposeAngle = 0.65f; // tan(33º)
    public int grainsPerStep = 5000;

    public float noiseScale = 0.1f;
    public float noiseAmplitude = 2f;

    public float depositeHeight = 0.2f; // altura de deposición de la arena
    public Vector2Int windDirection = new Vector2Int(1, 0); // viento este

    private DuneCell[,] duneGrid;

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
                float heightValue = Mathf.PerlinNoise(x * noiseScale, z * noiseScale) * noiseAmplitude;
                Vector3 position = new Vector3(x * tileSize, 0f, z * tileSize);
                GameObject tile = Instantiate(tilePrefab, position, Quaternion.identity, transform);
                tile.transform.localScale = new Vector3(tileSize, 0.1f, tileSize);

                duneGrid[x, z] = new DuneCell(tile, heightValue);
            }
        }
    }

    void SimulateWerner()
    {
        //Debug.Log("Simulando...");
        for (int i = 0; i < grainsPerStep; i++)
        {
            int x = Random.Range(0, width);
            int z = Random.Range(0, height);

            Vector2Int pos = new Vector2Int(x, z);
            Vector2Int dir = windDirection;

            if (!IsInBounds(pos + dir)) continue;

            Vector2Int target = pos + dir;

            while (IsInBounds(target))
            {
                float slope = duneGrid[pos.x, pos.y].height - duneGrid[target.x, target.y].height;
                //Debug.Log($"Slope: {slope}");

                if (slope < reposeAngle)
                {
                    pos = target;
                    target += dir;
                }
                else
                {
                    break;
                }
            }

            if (IsInBounds(target))
            {
                duneGrid[target.x, target.y].height += depositeHeight;
            }

            
        }

        RelaxSlopes();
        UpdateTiles();
    }

    void RelaxSlopes()
    {
        float relaxAmount = 0.05f; // .25f

        for (int x = 1; x < width - 1; x++)
        {
            for (int z = 1; z < height - 1; z++)
            {
                foreach (Vector2Int dir in new Vector2Int[] {
                    Vector2Int.right, Vector2Int.left,
                    Vector2Int.up, Vector2Int.down })
                {
                    Vector2Int n = new Vector2Int(x, z) + dir;
                    if (!IsInBounds(n)) continue;

                    float diff = duneGrid[x, z].height - duneGrid[n.x, n.y].height;
                    if (diff > reposeAngle)
                    {
                        float delta = (diff - reposeAngle) * relaxAmount;
                        duneGrid[x, z].height -= delta;
                        duneGrid[n.x, n.y].height += delta;
                    }
                }
            }
        }
    }

    void UpdateTiles()
    {
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                duneGrid[x, z].UpdateVisual(tileSize);
            }
        }
    }

    bool IsInBounds(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < width && pos.y >= 0 && pos.y < height;
    }
}

public class DuneCell
{
    public float height;
    public GameObject tileObj;

    public DuneCell(GameObject obj, float h)
    {
        tileObj = obj;
        height = h;
    }

    public void UpdateVisual(float tileSize)
    {
        float visualHeightScale = 1.5f;
        float clampedHeight = Mathf.Max(height * visualHeightScale, 0.1f);
        tileObj.transform.localScale = new Vector3(tileSize, clampedHeight, tileSize);
        tileObj.transform.position = new Vector3(tileObj.transform.position.x, clampedHeight / 2f, tileObj.transform.position.z);
        // Color segun altura
        float normalized = Mathf.InverseLerp(0f, 10f, height);
        Color sandColor = Color.Lerp(new Color(0.9f, 0.8f, 0.6f), new Color(1f, 0.5f, 0f), normalized);
        tileObj.GetComponent<Renderer>().material.color = sandColor;
    }
}