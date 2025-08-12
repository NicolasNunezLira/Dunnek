using System.Collections;
using Unity.Collections;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class VertexRaycaster : MonoBehaviour
{
    [SerializeField] private LayerMask hitMask;
    [SerializeField] private float rayLength = 100f;

    private MeshFilter meshFilter;

    private int width, height;
    private float size;

    private bool[,] windGrid;
    private bool[,] erosionGrid;
    private DesertPrefabSpawner.DesertElement[,] vegetationGrid;

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        width = DualMesh.Instance.xResolution + 1;
        height = DualMesh.Instance.zResolution + 1;
        size = DualMesh.Instance.size / DualMesh.Instance.simXResolution; // tamaño celda

        windGrid = new bool[width, height];
        erosionGrid = new bool[width, height];
        vegetationGrid = new DesertPrefabSpawner.DesertElement[width, height];
    }

    private void OnEnable()
    {
        DualMesh.OnDesertGenerated += RunRaycast;
    }

    private void OnDisable()
    {
        DualMesh.OnDesertGenerated -= RunRaycast;
    }

    public void RunRaycast()
    {
        StartCoroutine(CastRaysFromVertices());
    }

    private IEnumerator CastRaysFromVertices()
    {
        Mesh mesh = meshFilter.sharedMesh;
        Vector3[] vertices = mesh.vertices;

        // Limpiamos matrices antes de usar
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                windGrid[x, y] = erosionGrid[x, y] = false;

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 worldPos = meshFilter.transform.TransformPoint(vertices[i]);
            Ray ray = new Ray(worldPos + Vector3.up * rayLength, Vector3.down);

            if (Physics.Raycast(ray, out RaycastHit hit, rayLength * 2f, hitMask))
            {
                // Aquí determinamos si el objeto que tocamos es vegetación y de qué tipo
                var vegetation = hit.collider.GetComponentInParent<VegetationIdentifier>();
                Bounds bounds = hit.collider.GetComponent<Renderer>().bounds;

                if (vegetation != null)
                {
                    Vector2Int gridPos = WorldToGrid(worldPos);

                    if (gridPos.x < 0 || gridPos.x >= width || gridPos.y < 0 || gridPos.y >= height)
                        continue; // Fuera de límites

                    vegetationGrid[gridPos.x, gridPos.y] = vegetation.element;

                    // Añadimos un radio de 1 alrededor (chequear límites)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dz = -1; dz <= 1; dz++)
                        {
                            int nx = gridPos.x + dx;
                            int nz = gridPos.y + dz;

                            if (nx >= 0 && nx < width && nz >= 0 && nz < height)
                            {
                                switch (vegetation.element)
                                {
                                    case DesertPrefabSpawner.DesertElement.Tree:
                                        windGrid[nx, nz] = true;
                                        erosionGrid[nx, nz] = true;
                                        AddShadow(nx, nz, bounds);
                                        break;
                                    case DesertPrefabSpawner.DesertElement.Bush:
                                        erosionGrid[nx, nz] = true;
                                        break;
                                    case DesertPrefabSpawner.DesertElement.Rock:
                                        AddShadow(nx, nz, bounds);
                                        break;
                                }
                            }
                        }
                    }
                }
            }

            if (i % 1000 == 0) // Ceder frame para no congelar
                yield return null;
        }

        VegetationManager.SetEffectGrids(windGrid, erosionGrid, vegetationGrid);
    }

    private Vector2Int WorldToGrid(Vector3 worldPos)
    {
        int x = Mathf.Clamp(Mathf.RoundToInt(worldPos.x / size), 0, width - 1);
        int y = Mathf.Clamp(Mathf.RoundToInt(worldPos.z / size), 0, height - 1);
        return new Vector2Int(x, y);
    }

    private void AddShadow(int x, int z, Bounds bounds)
    {
        DualMesh.Instance.duneModel.terrainShadow[x, z] += bounds.max.y - 0.05f;
        DualMesh.Instance.duneModel.terrainShadowChanges.AddChanges(x, z);
        DualMesh.Instance.duneModel.UpdateShadow(
            x, z,
            DualMesh.Instance.duneModel.dx,
            DualMesh.Instance.duneModel.dz);
    }
}
