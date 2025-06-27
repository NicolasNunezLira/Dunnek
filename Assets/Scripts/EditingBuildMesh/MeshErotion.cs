using UnityEngine;
using Deform;
using DeformEditor;

[RequireComponent(typeof(MeshFilter))]
public class MeshErosion : MonoBehaviour
{
    [Header("Configuración de erosión")]
    public Vector3 windDirection = Vector3.forward; // Dirección del viento en coordenadas globales
    public float erosionSpeed = 0.01f;              // Velocidad del desplazamiento
    public float windThreshold = 0.8f;              // Cuánto debe alinearse el normal con el viento
    public float heightThreshold = 0.5f;            // Qué tan alto debe estar para erosionarse

    public float bendAngle = 0.1f;

    private MeshFilter meshFilter;
    private Mesh workingMesh;
    private Vector3[] baseVertices;
    private Vector3[] currentVertices;
    private Vector3[] normals;

    private BendDeformer bend;

    private void Start()
    {
        bend = GetComponentInChildren<BendDeformer>();
    }

    private void Update()
    {
        if (bend != null)
        {
            // Simular erosión empujando en dirección del viento
            bend.Angle = bendAngle * Time.deltaTime;
        }
    }

    void ErodeMesh()
    {
        // Convertir dirección del viento a espacio local
        Vector3 localWind = transform.InverseTransformDirection(windDirection.normalized);

        for (int i = 0; i < currentVertices.Length; i++)
        {
            Vector3 vertex = currentVertices[i];
            Vector3 normal = normals[i];

            // Qué tanto enfrenta el viento
            float facingWind = Vector3.Dot(normal.normalized, localWind);
            bool exposedToWind = facingWind > windThreshold;

            // Qué tan alto está
            bool highEnough = vertex.y > heightThreshold;

            if (exposedToWind && highEnough)
            {
                // Desplazar el vértice en dirección del viento
                currentVertices[i] -= localWind * erosionSpeed * Time.deltaTime;
            }
        }

        workingMesh.vertices = currentVertices;
        workingMesh.RecalculateNormals();
        workingMesh.RecalculateBounds();
    }

    public void ResetMesh()
    {
        baseVertices.CopyTo(currentVertices, 0);
        workingMesh.vertices = currentVertices;
        workingMesh.RecalculateNormals();
        workingMesh.RecalculateBounds();
    }
}

