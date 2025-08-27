using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utils;
using ResourceSystem;
using UnityEngine.EventSystems;

public class TooltipManager : Singleton<TooltipManager>
{
    [Header("Prefab del Tooltip")]
    [SerializeField] private GameObject tooltipPrefab;

    [Header("Offsets")]
    public float heightOffset = 1.5f;
    public float forwardOffset = 0.5f;
    public Vector2 screenPadding = new Vector2(20f, 20f);

    private RectTransform tooltipUI;
    private ScrollRect scrollRect;
    private TextMeshProUGUI tooltipTitle, tooltipInfo;
    private Button toggleButton;   
    private Transform target;
    private Camera mainCamera;

    private int currentConsumerId = -1;
    private bool isForceToStop, justOpened = false;   

    protected override void Awake()
    {
        base.Awake();

        mainCamera = Camera.main;

        if (tooltipPrefab != null)
        {
            GameObject canvas = GameObject.Find("UICanvas/TooltipManager");
            GameObject tooltipInstance = Instantiate(tooltipPrefab, canvas.transform);

            tooltipUI = tooltipInstance.GetComponent<RectTransform>();
            var texts = tooltipInstance.GetComponentsInChildren<TextMeshProUGUI>();
            if (texts.Length >= 2)
            {
                tooltipTitle = texts[0];
                tooltipInfo = texts[1];
            }
            // Busca el botón dentro del prefab
            toggleButton = tooltipInstance.GetComponentInChildren<Button>(true);
            if (toggleButton != null)
                toggleButton.onClick.AddListener(ToggleProduction);

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
            Vector3 worldOffset = Vector3.up * heightOffset;
            Vector3 worldPos = target.position + worldOffset;
            Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPos);

            if (screenPos.z < 0)
            {
                tooltipUI.gameObject.SetActive(false);
                return;
            }

            float clampedX = Mathf.Clamp(screenPos.x, screenPadding.x, Screen.width - screenPadding.x);
            float clampedY = Mathf.Clamp(screenPos.y, screenPadding.y, Screen.height - screenPadding.y);

            tooltipUI.position = new Vector3(clampedX, clampedY, screenPos.z);

            tooltipUI.gameObject.SetActive(true);
        }

        if (target != null && Input.GetMouseButtonDown(0))
        {
            if (justOpened)
            {
                justOpened = false;
                return;
            }

            if (EventSystem.current.IsPointerOverGameObject())
                return;

            if (!RectTransformUtility.RectangleContainsScreenPoint(tooltipUI, Input.mousePosition, mainCamera))
            {
                HideTooltip();
            }
        }
    }

    public void ShowTooltip(Transform worldTarget, string title, string info, int? consumerId = null)
    {
        target = worldTarget;

        if (tooltipTitle != null)
            tooltipTitle.text = title;

        if (tooltipInfo != null)
            tooltipInfo.text = info;

        if (tooltipUI != null)
            tooltipUI.gameObject.SetActive(true);

        if (consumerId.HasValue)
        {
            currentConsumerId = consumerId.Value;
            var consumer = ResourceManager.GetAllConsumers()[currentConsumerId];
            isForceToStop = consumer.isForceToStop;
            UpdateButtonText();
            toggleButton.gameObject.SetActive(true);
        }
        else
        {
            currentConsumerId = -1;
            if (toggleButton != null)
                toggleButton.gameObject.SetActive(false);
        }

        justOpened = true;
        
        LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipUI);
    }

    public void HideTooltip()
    {
        target = null;

        if (tooltipUI != null)
            tooltipUI.gameObject.SetActive(false);

        currentConsumerId = -1;
    }

    private void ToggleProduction()
    {
        if (currentConsumerId < 0) return;

        var consumers = ResourceManager.GetAllConsumers();
        if (!consumers.ContainsKey(currentConsumerId)) return;

        var consumer = consumers[currentConsumerId];
        bool newState = !consumer.isForceToStop;

        ResourceManager.SetConsumerActive(currentConsumerId, newState);
        UpdateButtonText();
    }

    private void UpdateButtonText()
    {
        if (toggleButton == null) return;

        var consumers = ResourceManager.GetAllConsumers();
        if (!consumers.ContainsKey(currentConsumerId)) return;

        var consumer = consumers[currentConsumerId];
        var textComp = toggleButton.GetComponentInChildren<TextMeshProUGUI>();
        if (textComp != null)
            textComp.text = consumer.isForceToStop ? "Activar producción" : "Forzar detención";
    }
}
