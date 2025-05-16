using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Movimiento")]
    public float panSpeed = 20f;
    public float panBorderThickness = 10f;  // Para movimiento cuando el mouse está en el borde
    public bool useScreenEdges = true;      // Activar/desactivar movimiento por bordes
    public Vector2 panLimit = new Vector2(50f, 50f);  // Límites X y Z del movimiento
    
    [Header("Zoom")]
    public float zoomSpeed = 10f;
    public float minZoom = 10f;  // Altura mínima (más cerca)
    public float maxZoom = 50f;  // Altura máxima (más lejos)
    
    [Header("Rotación")]
    public bool allowRotation = true;
    public float rotationSpeed = 100f;
    
    [Header("Movimiento con Mouse")]
    public bool allowMousePan = true;
    public float mousePanSpeed = 0.5f;
    
    // Variables internas
    private Vector3 lastMousePosition;
    private Vector3 dragStartPosition;
    private Quaternion dragStartRotation;
    private bool isDragging = false;
    private bool isRotating = false;
    
    private void Update()
    {
        // Vector para el movimiento de la cámara
        Vector3 position = transform.position;
        
        // ============= MOVER CON TECLADO =============
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");
        
        if (horizontalInput != 0 || verticalInput != 0)
        {
            // Calculamos el movimiento basado en la orientación de la cámara
            Vector3 forward = new Vector3(transform.forward.x, 0, transform.forward.z).normalized;
            Vector3 right = new Vector3(transform.right.x, 0, transform.right.z).normalized;
            
            position += forward * verticalInput * panSpeed * Time.deltaTime;
            position += right * horizontalInput * panSpeed * Time.deltaTime;
        }
        
        // ============= MOVER CON BORDES DE PANTALLA =============
        if (useScreenEdges)
        {
            if (Input.mousePosition.y >= Screen.height - panBorderThickness)
            {
                // Movimiento hacia adelante cuando el mouse está en el borde superior
                Vector3 forward = new Vector3(transform.forward.x, 0, transform.forward.z).normalized;
                position += forward * panSpeed * Time.deltaTime;
            }
            if (Input.mousePosition.y <= panBorderThickness)
            {
                // Movimiento hacia atrás cuando el mouse está en el borde inferior
                Vector3 forward = new Vector3(transform.forward.x, 0, transform.forward.z).normalized;
                position -= forward * panSpeed * Time.deltaTime;
            }
            if (Input.mousePosition.x >= Screen.width - panBorderThickness)
            {
                // Movimiento hacia la derecha cuando el mouse está en el borde derecho
                Vector3 right = new Vector3(transform.right.x, 0, transform.right.z).normalized;
                position += right * panSpeed * Time.deltaTime;
            }
            if (Input.mousePosition.x <= panBorderThickness)
            {
                // Movimiento hacia la izquierda cuando el mouse está en el borde izquierdo
                Vector3 right = new Vector3(transform.right.x, 0, transform.right.z).normalized;
                position -= right * panSpeed * Time.deltaTime;
            }
        }
        
        // ============= MOVER CON MOUSE (ARRASTRAR) =============
        if (allowMousePan)
        {
            // Botón central o botón derecho para arrastrar
            if (Input.GetMouseButtonDown(2) || (!isRotating && Input.GetMouseButtonDown(1)))
            {
                lastMousePosition = Input.mousePosition;
                isDragging = true;
            }
            
            if (isDragging && (Input.GetMouseButton(2) || (!isRotating && Input.GetMouseButton(1))))
            {
                Vector3 delta = Input.mousePosition - lastMousePosition;
                
                // Movimiento inverso a la dirección del arrastre
                Vector3 right = new Vector3(transform.right.x, 0, transform.right.z).normalized;
                Vector3 forward = new Vector3(transform.forward.x, 0, transform.forward.z).normalized;
                
                position -= right * delta.x * mousePanSpeed * Time.deltaTime * 50;
                position -= forward * delta.y * mousePanSpeed * Time.deltaTime * 50;
                
                lastMousePosition = Input.mousePosition;
            }
            
            if (Input.GetMouseButtonUp(2) || Input.GetMouseButtonUp(1))
            {
                isDragging = false;
            }
        }
        
        // ============= ZOOM CON RUEDA DEL MOUSE =============
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        if (scrollInput != 0)
        {
            // Ajusta la posición Y (altura) basada en el zoom
            position.y -= scrollInput * zoomSpeed * 10 * Time.deltaTime * 10;
            
            // Limita el zoom
            position.y = Mathf.Clamp(position.y, minZoom, maxZoom);
        }
        
        // ============= ROTACIÓN CON ALT + BOTÓN IZQUIERDO =============
        if (allowRotation)
        {
            if (Input.GetKey(KeyCode.LeftAlt) && Input.GetMouseButtonDown(0))
            {
                dragStartPosition = Input.mousePosition;
                dragStartRotation = transform.rotation;
                isRotating = true;
            }
            
            if (isRotating && Input.GetMouseButton(0) && Input.GetKey(KeyCode.LeftAlt))
            {
                float deltaX = (Input.mousePosition.x - dragStartPosition.x) * 0.5f;
                
                // Rota la cámara alrededor del eje Y
                transform.rotation = dragStartRotation * Quaternion.Euler(0, deltaX, 0);
            }
            
            if (Input.GetMouseButtonUp(0) || !Input.GetKey(KeyCode.LeftAlt))
            {
                isRotating = false;
            }
        }
        
        // ============= APLICAR Y LIMITAR POSICIÓN =============
        // Limita la posición para no salir de los límites
        position.x = Mathf.Clamp(position.x, -panLimit.x, panLimit.x);
        position.z = Mathf.Clamp(position.z, -panLimit.y, panLimit.y);
        
        // Asigna la nueva posición
        transform.position = position;
    }
}
