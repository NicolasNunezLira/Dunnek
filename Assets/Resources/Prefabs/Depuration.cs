using UnityEngine;

public class GizmoDirection : MonoBehaviour
{
    void OnDrawGizmos()
    {
        // Dibuja una línea roja desde el objeto hacia adelante (Z+ local)
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * 2);
    }
}
