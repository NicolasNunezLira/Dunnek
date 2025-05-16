using UnityEngine;

public class GridInteraction : MonoBehaviour
{
    public GridSystem gridSystem;
    public GameObject buildingPrefab; // El prefab que se colocará (puede ser un edificio básico)
    public Camera mainCamera;
    public LayerMask groundLayer; // Capa para el raycast
    
    private GameObject previewObject; // Objeto para mostrar una vista previa
    private bool isBuilding = false;
    
    private void Start()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
        
        if (gridSystem == null)
        {
            gridSystem = FindObjectOfType<GridSystem>();
            if (gridSystem == null)
            {
                Debug.LogError("No se encontró el GridSystem. Asigna uno en el inspector.");
            }
        }
    }
    
    private void Update()
    {
        // Activa/desactiva el modo construcción con la tecla B
        if (Input.GetKeyDown(KeyCode.B))
        {
            isBuilding = !isBuilding;
            
            if (isBuilding)
            {
                CreatePreviewObject();
            }
            else
            {
                DestroyPreview();
            }
        }
        
        // Si estamos en modo construcción
        if (isBuilding)
        {
            HandleBuildingMode();
        }
    }
    
    private void HandleBuildingMode()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, 100f, groundLayer))
        {
            // Convierte la posición mundial a coordenadas de grilla
            Vector2Int gridPos = gridSystem.WorldToGridPosition(hit.point);
            
            // Actualiza la posición de la vista previa
            if (previewObject != null)
            {
                Vector3 cellCenter = gridSystem.GridToWorldPosition(gridPos.x, gridPos.y);
                previewObject.transform.position = cellCenter;
                
                // Cambia el color según si el lugar está disponible o no
                Renderer renderer = previewObject.GetComponent<Renderer>();
                if (renderer != null)
                {
                    bool isAvailable = gridSystem.IsCellAvailable(gridPos.x, gridPos.y);
                    renderer.material.color = isAvailable ? 
                        new Color(0, 1, 0, 0.5f) :  // Verde si está disponible
                        new Color(1, 0, 0, 0.5f);   // Rojo si no está disponible
                }
            }
            
            // Si hacemos clic para construir
            if (Input.GetMouseButtonDown(0))
            {
                if (gridSystem.IsCellAvailable(gridPos.x, gridPos.y))
                {
                    // Instancia un nuevo objeto y lo coloca en la grilla
                    GameObject newBuilding = Instantiate(buildingPrefab);
                    gridSystem.PlaceObject(newBuilding, gridPos.x, gridPos.y);
                }
                else
                {
                    Debug.Log("No se puede construir aquí. La celda está ocupada.");
                }
            }
            
            // Si hacemos clic derecho para eliminar
            if (Input.GetMouseButtonDown(1))
            {
                gridSystem.RemoveObject(gridPos.x, gridPos.y);
            }
        }
    }
    
    private void CreatePreviewObject()
    {
        if (buildingPrefab == null)
        {
            Debug.LogError("No hay un prefab de edificio asignado.");
            return;
        }
        
        previewObject = Instantiate(buildingPrefab);
        
        // Configura el material para que sea transparente
        Renderer renderer = previewObject.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material previewMaterial = new Material(Shader.Find("Transparent/Diffuse"));
            previewMaterial.color = new Color(0, 1, 0, 0.5f);
            renderer.material = previewMaterial;
        }
        
        // Deshabilita cualquier collider para que no interfiera
        Collider collider = previewObject.GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = false;
        }
    }
    
    private void DestroyPreview()
    {
        if (previewObject != null)
        {
            Destroy(previewObject);
            previewObject = null;
        }
    }
    
    private void OnDestroy()
    {
        DestroyPreview();
    }
}