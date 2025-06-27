using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class MeshErosion2 : MonoBehaviour
{
    public float erosionSpeed = 0.01f; // Qué tanto bajan los vértices por frame
    public Vector3 windDirection = Vector3.right; // Dirección del viento (ej: +x)
    private Mesh deformingMesh;
    private Vector3[] originalVertices;
    private Vector3[] deformedVertices;

    void Start()
    {
        deformingMesh = GetComponent<MeshFilter>().mesh;

        // Copiamos los vértices
        originalVertices = deformingMesh.vertices;
        deformedVertices = deformingMesh.vertices;
    }

    void Update()
    {
        for (int i = 0; i < deformedVertices.Length; i++)
        {
            Vector3 worldPos = transform.TransformPoint(deformedVertices[i]);
            float exposure = Vector3.Dot(windDirection.normalized, worldPos.normalized); // qué tan expuesto está al viento

            if (exposure > 0.5f || deformedVertices[i].y > 0.1f) // erosiona solo los más expuestos o altos
            {
                // Reducimos la altura y desplazamos levemente en dirección del viento
                deformedVertices[i].y -= erosionSpeed * Time.deltaTime;
                deformedVertices[i] += windDirection.normalized * (erosionSpeed * 0.1f * Time.deltaTime);
            }
        }

        deformingMesh.vertices = deformedVertices;
        deformingMesh.RecalculateNormals();
    }
}
