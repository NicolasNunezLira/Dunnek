using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utils;

public class TooltipManager : Singleton<TooltipManager>
{
    [Header("Prefab del Tooltip")]
    [SerializeField] private GameObject tooltipPrefab;

    [Header("Offsets")]
    public float heightOffset = 1.5f;
    public float forwardOffset = 0.5f;
    public Vector2 screenPadding = new Vector2(20f, 20f);

    private RectTransform tooltipUI;
    private TextMeshProUGUI tooltipText;
    private Transform target;
    private Camera mainCamera;

    protected override void Awake()
    {
        base.Awake();

        mainCamera = Camera.main;

        if (tooltipPrefab != null)
        {
            GameObject canvas = GameObject.Find("UICanvas/TooltipManager");
            GameObject tooltipInstance = Instantiate(tooltipPrefab, canvas.transform);

            tooltipUI = tooltipInstance.GetComponent<RectTransform>();
            tooltipText = tooltipInstance.GetComponentInChildren<TextMeshProUGUI>();

            HideTooltip();
        }
        else
        {
            Debug.LogError("TooltipManager: No hay prefab asignado.");
        }
    }

    private void Update()
    {
        if (target != null && tooltipUI != null)
        {
            
            Vector3 worldOffset = Vector3.up * heightOffset
                                //- mainCamera.transform.forward * forwardOffset
                                ;

            Vector3 worldPos = target.position + worldOffset;
            Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPos);

            // si está detrás de la cámara, ocultamos
            if (screenPos.z < 0)
            {
                tooltipUI.gameObject.SetActive(false);
                return;
            }

            // mantener dentro de la pantalla
            float clampedX = Mathf.Clamp(screenPos.x, screenPadding.x, Screen.width - screenPadding.x);
            float clampedY = Mathf.Clamp(screenPos.y, screenPadding.y, Screen.height - screenPadding.y);

            tooltipUI.position = new Vector3(clampedX, clampedY, screenPos.z);

            tooltipUI.gameObject.SetActive(true);
        }
    }

    public void ShowTooltip(Transform worldTarget, string text)
    {
        target = worldTarget;

        if (tooltipText != null)
            tooltipText.text = text;

        if (tooltipUI != null)
            tooltipUI.gameObject.SetActive(true);
    }

    public void HideTooltip()
    {
        target = null;

        if (tooltipUI != null)
            tooltipUI.gameObject.SetActive(false);
    }
}
