using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class EditMesh : MonoBehaviour
{
    private Mesh deformingMesh;

    private Vector3[] originalVertices, deformedVertices;

    private int frame;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        deformingMesh = GetComponent<MeshFilter>().mesh;

        // Copiamos los vértices
        originalVertices = deformingMesh.vertices;
        deformedVertices = deformingMesh.vertices;
    }

    // Update is called once per frame
    void Update()
    {
        for (int i = 0; i < deformedVertices.Length; i++)
        {
            Vector3 worldPos = transform.TransformPoint(deformedVertices[i]);

            if (frame > 500) // erosiona solo los más expuestos o altos
            {
                Debug.Log($"Vertice {i} cambiado");
                // Reducimos la altura y desplazamos levemente en dirección del viento
                deformedVertices[i] /= 2;
            }
        }

        deformingMesh.vertices = deformedVertices;
        deformingMesh.RecalculateNormals();

        frame++;
    }
}
