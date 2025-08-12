using UnityEngine;

[ExecuteAlways]
public class ShowVerticesGizmos : MonoBehaviour
{
    private MeshFilter meshFilter;

    void OnDrawGizmos()
    {
        if (meshFilter == null)
            meshFilter = GetComponentInChildren<MeshFilter>();

        if (meshFilter == null || meshFilter.sharedMesh == null)
            return;

        Vector3[] vertices = meshFilter.sharedMesh.vertices;
        Gizmos.color = Color.red;

        foreach (var vertex in vertices)
        {
            Vector3 worldPos = transform.TransformPoint(vertex);
            Gizmos.DrawSphere(worldPos, 0.05f);
        }
    }
}
