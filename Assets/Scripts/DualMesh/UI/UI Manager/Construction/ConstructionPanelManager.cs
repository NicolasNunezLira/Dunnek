using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using ConstructionSystem;
using System.Linq;
using Utils;
using ResourceSystem;
using System;
using TMPro;

public class UIController : Singleton<UIController>
{
    #region - Parameters
    [Header("Main Buttons")]
    [SerializeField] public Button buildButton; // único botón principal

    [Header("Selected Panel")]
    [SerializeField] public GameObject selectedPanel;

    [Header("Options Panels")]
    [SerializeField] public GameObject buildOptionsPanel;

    [Header("Build Category Panels")]
    [SerializeField] public GameObject housingPanel;
    [SerializeField] public GameObject collectorPanel;
    [SerializeField] public GameObject wallPanel;
    [SerializeField] public GameObject consumerPanel;
    [SerializeField] public GameObject bonusPanel;
    [SerializeField] public GameObject actionsPanel;

    [Header("Category Tabs")]
    [SerializeField] public Button housingTabButton;
    [SerializeField] public Button collectorTabButton;
    [SerializeField] public Button wallTabButton;
    [SerializeField] public Button consumerTabButton;
    [SerializeField] public Button bonusTabButton;
    [SerializeField] public Button actionsTabButton;

    [Header("Prefabs")]
    [SerializeField] public GameObject constructionButtonPrefab;

    private Outline buildOutline;

    private Dictionary<string, UIButtonReference> constructionButtons = new();
    private Dictionary<string, UIButtonReference> actionButtons = new();
    private Dictionary<string, GameObject> categoryPanels = new();
    private Dictionary<string, Button> tabButtons = new();

    public enum BuildingModeState
    {
        Idle,
        Building
    }

    public BuildingModeState currentState { get; private set; } = BuildingModeState.Idle;
    public string MainButtonText { get; private set; } = "Build/Action";
    public string currentCategory { get; private set; } = "Housing";
    public string currentBuilding { get; private set; }
    public string currentAction { get; private set; }

    private Color selectedColor = Color.green;
    private Color defaultColor = new Color(0, 0, 0, 0);

    private Dictionary<UIButtonReference, Coroutine> flashRoutines = new();
    private float flashDuration = 1f;
    private Dictionary<UIButtonReference, Color> originalColors = new();
    #endregion

    #region - Awake
    protected override void Awake()
    {
        base.Awake();

        buildButton.onClick.AddListener(OnBuildClicked);

        buildOutline = buildButton.GetComponent<Outline>();

        categoryPanels["Housing"] = housingPanel;
        categoryPanels["Collector"] = collectorPanel;
        categoryPanels["Wall"] = wallPanel;
        categoryPanels["Consumer"] = consumerPanel;
        categoryPanels["BonusProvider"] = bonusPanel;
        categoryPanels["Actions"] = actionsPanel;

        housingTabButton.onClick.AddListener(() => ShowCategory("Housing"));
        collectorTabButton.onClick.AddListener(() => ShowCategory("Collector"));
        wallTabButton.onClick.AddListener(() => ShowCategory("Wall"));
        consumerTabButton.onClick.AddListener(() => ShowCategory("Consumer"));
        bonusTabButton.onClick.AddListener(() => ShowCategory("BonusProvider"));
        actionsTabButton.onClick.AddListener(() => ShowCategory("Actions"));

        tabButtons["Housing"] = housingTabButton;
        tabButtons["Collector"] = collectorTabButton;
        tabButtons["Wall"] = wallTabButton;
        tabButtons["Consumer"] = consumerTabButton;
        tabButtons["BonusProvider"] = bonusTabButton;
        tabButtons["Actions"] = actionsTabButton;

        GenerateConstructionButtons();
        InitializeActionButtons();

        HideAllPanels();
    }
    #endregion

    #region --- Main Button ---
    void OnBuildClicked()
    {
        if (currentState == BuildingModeState.Building)
        {
            CancelBuildMode();
            return;
        }

        ShowCategory(currentCategory);
        DualMesh.Instance.SetMode(DualMesh.PlayingMode.Build);
        UpdateMainButtonVisuals(DualMesh.Instance.inMode);
        BonusVisualizerManager.Instance.ClearVisuals();
        TooltipManager.Instance.HideTooltip();
    }


    public void UpdateMainButtonVisuals(DualMesh.PlayingMode mode)
    {
        buildOutline.effectColor = (mode == DualMesh.PlayingMode.Build) ? selectedColor : defaultColor;
        SetPanelVisible(buildOptionsPanel, mode == DualMesh.PlayingMode.Build);
    }

    void ChangeTextMainButton()
    {
        switch (currentState)
        {
            case BuildingModeState.Idle:
                buildButton.GetComponentInChildren<TextMeshProUGUI>().text = "Build/Action";
                break;
            case BuildingModeState.Building:
                buildButton.GetComponentInChildren<TextMeshProUGUI>().text = "Cancel";
                break;
        }
    }

    public void CancelBuildMode()
    {
        currentState = BuildingModeState.Idle;
        ChangeTextMainButton();

        DualMesh.Instance.SetMode(DualMesh.PlayingMode.Simulation);

        HideAllPanels();
        SetPanelVisible(buildOptionsPanel, true); // vuelves a mostrar categorías
    }
    #endregion

    #region - Selected Panel
    void SetSelectPanel(string selectedID, UIButtonReference btnRef)
    {
        SetPanelVisible(selectedPanel, true);
        
        foreach (Transform child in selectedPanel.transform)
        {
            Destroy(child.gameObject);
        }
        
        GameObject btnCopy = Instantiate(constructionButtonPrefab, selectedPanel.transform);

        RectTransform panelRT = selectedPanel.GetComponent<RectTransform>();
        RectTransform btnRT   = btnCopy.GetComponent<RectTransform>();

        btnRT.anchorMin = new Vector2(0.5f, 0.5f);
        btnRT.anchorMax = new Vector2(0.5f, 0.5f);
        btnRT.pivot     = new Vector2(0.5f, 0.5f);
        btnRT.anchoredPosition = Vector2.zero;

        Vector2 panelSize = panelRT.rect.size;
        Vector2 originalSize = btnRT.sizeDelta;
        float scale = Mathf.Min(panelSize.x / originalSize.x, panelSize.y / originalSize.y) * 0.7f; 
        btnRT.localScale = new Vector3(scale, scale, 1);

        UIButtonReference btnCopyRef = btnCopy.GetComponent<UIButtonReference>();

        btnCopyRef.buttonID = btnRef.buttonID;
        btnCopyRef.label.text = btnRef.label.text;
        btnCopyRef.iconImage.sprite = btnRef.iconImage.sprite;

        var config = ConstructionConfig.Instance.ConstructionConfigs[selectedID];
        RefreshBuildButtonCosts(btnCopyRef, config);

        btnCopyRef.button.onClick.RemoveAllListeners();
        btnCopyRef.button.onClick.AddListener(() =>
        {
            Debug.Log($"Botón seleccionado clickeado de nuevo: {selectedID}");
            
            CancelBuildMode();
        });
    }

    void SetSelectActionPanel(string actionID, UIButtonReference btnRef)
    {
        SetPanelVisible(selectedPanel, true);

        foreach (Transform child in selectedPanel.transform)
            Destroy(child.gameObject);

        GameObject btnCopy = Instantiate(constructionButtonPrefab, selectedPanel.transform);

        RectTransform panelRT = selectedPanel.GetComponent<RectTransform>();
        RectTransform btnRT = btnCopy.GetComponent<RectTransform>();

        // Centrado
        btnRT.anchorMin = new Vector2(0.5f, 0.5f);
        btnRT.anchorMax = new Vector2(0.5f, 0.5f);
        btnRT.pivot = new Vector2(0.5f, 0.5f);
        btnRT.anchoredPosition = Vector2.zero;

        // Escala proporcional al panel
        Vector2 panelSize = panelRT.rect.size;
        Vector2 originalSize = btnRT.sizeDelta;
        float scale = Mathf.Min(panelSize.x / originalSize.x, panelSize.y / originalSize.y) * 0.7f;
        btnRT.localScale = new Vector3(scale, scale, 1);

        UIButtonReference btnCopyRef = btnCopy.GetComponent<UIButtonReference>();

        // Copiar datos visuales
        btnCopyRef.buttonID = btnRef.buttonID;
        btnCopyRef.label.text = btnRef.label.text;
        btnCopyRef.iconImage.sprite = btnRef.iconImage.sprite;

        if (ActionConfig.Instance.actionsConfig.TryGetValue(MapToActionMode(actionID), out var config))
            RefreshActionButtonCosts(btnCopyRef, config);

        // El botón del SelectedPanel solo sirve como preview
        btnCopyRef.button.onClick.RemoveAllListeners();
        btnCopyRef.button.onClick.AddListener(() =>
        {
            Debug.Log($"Botón de acción seleccionado clickeado de nuevo: {actionID}");
            CancelBuildMode();
        });
    }

    DualMesh.ActionMode MapToActionMode(string id)
    {
        return id switch
        {
            "Dig" => DualMesh.ActionMode.Dig,
            "AddSand" => DualMesh.ActionMode.AddSand,
            "Flat" => DualMesh.ActionMode.Flat,
            "Recycle" => DualMesh.ActionMode.Recycle,
            _ => DualMesh.ActionMode.Dig
        };
    }

    #endregion

    #region --- Constructions ---
    void GenerateConstructionButtons()
    {
        foreach (var kvp in ConstructionConfig.Instance.ConstructionConfigs)
        {
            var config = kvp.Value;

            if (!IsUnlocked(config.codeName))
                continue;

            CreateConstructionButton(config);
        }
    }

    public void CreateConstructionButton(ConstructionConfig.ConfigData config)
    {
        string category = config.category.ToString();
        if (!categoryPanels.TryGetValue(category, out GameObject currentPanel))
            return;

        Transform parentPanel = currentPanel.transform;
        GameObject btnGO = Instantiate(constructionButtonPrefab, parentPanel);

        UIButtonReference btnRef = btnGO.GetComponent<UIButtonReference>();
        btnRef.buttonID = config.codeName;

        if (config.icon != null && btnRef.iconImage != null)
            btnRef.iconImage.sprite = config.icon;

        btnRef.label.text = config.displayName;
        btnRef.button.onClick.AddListener(() => OnConstructionClicked(config.codeName, btnRef));

        RefreshBuildButtonCosts(btnRef, config);

        constructionButtons[config.codeName] = btnRef;
        originalColors[btnRef] = btnRef.button.GetComponent<Image>().color;

        RebuilLayoutPanels();
    }

    public void ShowCategory(string category)
    {
        currentCategory = category;
        //Debug.Log($"Current Category {currentCategory}");

        foreach (var kvp in categoryPanels)
        {
            tabButtons[kvp.Key].GetComponent<Outline>().effectColor = (currentCategory == kvp.Key) ? selectedColor : defaultColor;
            SetPanelVisible(kvp.Value, kvp.Key == category);
            LayoutRebuilder.ForceRebuildLayoutImmediate(kvp.Value.GetComponent<RectTransform>());
        }
    }

    bool IsUnlocked(string codeName)
    {
        return ConstructionUnlockerManager.UnlockedConstructions.Contains(codeName);
    }

    void OnConstructionClicked(string codeName, UIButtonReference btnRef)
    {
        if (!constructionButtons.ContainsKey(codeName)) return;

        var config = ConstructionConfig.Instance.ConstructionConfigs[codeName];
        Debug.Log($"Construcción seleccionada: {config.codeName} ({config.category})");

        currentBuilding = codeName;
        currentCategory = config.category.ToString();

        if (DualMesh.Instance.builder.HasEnoughResourcesForBuild(new Dictionary<string, int> { { codeName, 1 } })
            || config.category == ConstructionCategory.Wall)
        {
            DualMesh.Instance.SetBuildType(codeName);

            UpdateSelectedVisual(codeName);
        }
        else
        {
            FlashRed(btnRef);
        }
    }

    public void UpdateSelectedVisual(string selectedID)
    {
        if (selectedID == null || selectedID == "")
        {
            CancelBuildMode();
            return;
        }
        if (!constructionButtons.TryGetValue(selectedID, out var btnRef)) return;

        currentState = BuildingModeState.Building;
        ChangeTextMainButton();
        HideAllPanels();

        SetSelectPanel(selectedID, btnRef);
    }


    public void OnConstructionUnlocked(string codeName)
    {
        if (constructionButtons.ContainsKey(codeName))
            return;

        if (ConstructionConfig.Instance.ConstructionConfigs.TryGetValue(codeName, out var config))
            CreateConstructionButton(config);
    }
    #endregion

    #region --- Actions ---
    void InitializeActionButtons()
    {
        foreach (var (_, config) in ActionConfig.Instance.actionsConfig)
        {
            CreateActionButton(config);
        }
    }

    void CreateActionButton(ActionConfig.ConfigData config)
    {
        Transform parentPanel = categoryPanels["Actions"].transform;
        GameObject btnGO = Instantiate(constructionButtonPrefab, parentPanel);

        UIButtonReference btnRef = btnGO.GetComponent<UIButtonReference>();
        btnRef.buttonID = config.type;

        if (config.icon != null && btnRef.iconImage != null)
            btnRef.iconImage.sprite = config.icon;

        btnRef.label.text = config.type;

        btnRef.button.onClick.AddListener(() => OnActionOptionClicked(config.type, btnRef));

        RefreshActionButtonCosts(btnRef, config);

        actionButtons[config.type] = btnRef;

        RebuilLayoutPanels();
    }

    void OnActionOptionClicked(string id, UIButtonReference btnRef)
    {
        currentAction = id;

        if (!actionButtons.ContainsKey(id)) return;
        if (!MapAction(id, out var parsed)) return;

        if (id == "Recycle" || DualMesh.Instance.builder.HasEnoughtResourcesForAction(parsed.Value))
        {
            SetActionType(id);
            UpdateActionsButtonVisual(id);
        }
        else
        {
            FlashRed(btnRef);
        }
    }

    void SetActionType(string id)
    {
        switch (id)
        {
            case "Dig":
                DualMesh.Instance.SetActionType(DualMesh.ActionMode.Dig);
                break;
            case "AddSand":
                DualMesh.Instance.SetActionType(DualMesh.ActionMode.AddSand);
                break;
            case "Flat":
                DualMesh.Instance.SetActionType(DualMesh.ActionMode.Flat);
                break;
            case "Recycle":
                DualMesh.Instance.SetActionType(DualMesh.ActionMode.Recycle);
                break;
        }
    }

    bool MapAction(string id, out DualMesh.ActionMode? parsed)
    {
        switch (id)
        {
            case "Dig":
                parsed = DualMesh.ActionMode.Dig;
                return true;
            case "AddSand":
                parsed = DualMesh.ActionMode.AddSand;
                return true;
            case "Flat":
                parsed = DualMesh.ActionMode.Flat;
                return true;
            case "Recycle":
                parsed = DualMesh.ActionMode.Dig;
                return true;
        }
        parsed = null;
        return false;
    }

    public void UpdateActionsButtonVisual(string selectedID)
    {
        if (selectedID == null || selectedID == "")
        {
            CancelBuildMode();
            return;
        }
        if (!actionButtons.TryGetValue(selectedID, out var btnRef)) return;

        currentState = BuildingModeState.Building;
        ChangeTextMainButton();
        HideAllPanels();

        SetSelectActionPanel(selectedID, btnRef);
    }
    #endregion

    #region - External Updates
    public void UpdateBuildCost()
    {
        foreach (var kvp in constructionButtons)
        {
            string codeName = kvp.Key;
            UIButtonReference btnRef = kvp.Value;

            if (ConstructionConfig.Instance.ConstructionConfigs.TryGetValue(codeName, out var config))
            {
                RefreshBuildButtonCosts(btnRef, config);
                LayoutRebuilder.ForceRebuildLayoutImmediate(btnRef.GetComponent<RectTransform>());
            }
        }

        RebuilLayoutPanels();
    }

    public void UpdateActionCost()
    {
        foreach (var kvp in actionButtons)
        {
            string codeName = kvp.Key;
            UIButtonReference btnRef = kvp.Value;

            if (!MapAction(codeName, out var parsed)) continue;

            if (ActionConfig.Instance.actionsConfig.TryGetValue(parsed.Value, out var config))
            {
                RefreshActionButtonCosts(btnRef, config);
                LayoutRebuilder.ForceRebuildLayoutImmediate(btnRef.GetComponent<RectTransform>());
            }
        }

        RebuilLayoutPanels();
    }

    public void UpdateAllCost()
    {
        UpdateActionCost();
        UpdateBuildCost();
    }
    #endregion

    #region - Helper
    void RebuilLayoutPanels()
    {
        foreach (var panel in categoryPanels.Values)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(panel.GetComponent<RectTransform>());
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(buildOptionsPanel.GetComponent<RectTransform>());
    }

    private void RefreshBuildButtonCosts(UIButtonReference btnRef, ConstructionConfig.ConfigData config)
    {
        foreach (Transform child in btnRef.costPanel)
        {
            Destroy(child.gameObject);
        }

        foreach ((Resource resource, float amount) in config.cost)
        {
            GameObject slotGO = Instantiate(btnRef.resourceSlotPrefab, btnRef.costPanel);
            slotGO.name = "slot" + resource.ToString();

            var slotImage = slotGO.GetComponentInChildren<UnityEngine.UI.Image>();
            var slotText = slotGO.GetComponentInChildren<TMPro.TextMeshProUGUI>();

            slotText.text = Math.Abs(amount).ToString();
            slotImage.sprite = ResourceSystem.ResoureIconManager.Instance.GetIcon(resource);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(btnRef.costPanel.GetComponent<RectTransform>());

        RebuilLayoutPanels();
    }

    private void RefreshActionButtonCosts(UIButtonReference btnRef, ActionConfig.ConfigData config)
    {
        foreach (Transform child in btnRef.costPanel)
        {
            Destroy(child.gameObject);
        }

        foreach ((Resource resource, float amount) in config.cost)
        {
            GameObject slotGO = Instantiate(btnRef.resourceSlotPrefab, btnRef.costPanel);
            slotGO.name = "slot" + resource.ToString();

            var slotImage = slotGO.GetComponentInChildren<UnityEngine.UI.Image>();
            var slotText = slotGO.GetComponentInChildren<TMPro.TextMeshProUGUI>();

            slotText.text = Math.Abs(amount).ToString();
            slotImage.sprite = ResourceSystem.ResoureIconManager.Instance.GetIcon(resource);
            //LayoutRebuilder.ForceRebuildLayoutImmediate(slotGO.GetComponent<RectTransform>());
        }

        //Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(btnRef.costPanel.GetComponent<RectTransform>());

        UpdateBuildCost();
    }

    void SetPanelVisible(GameObject panel, bool visible)
    {
        var cg = panel.GetComponent<CanvasGroup>();
        if (cg == null) cg = panel.AddComponent<CanvasGroup>();

        cg.alpha = visible ? 1 : 0;
        cg.interactable = visible;
        cg.blocksRaycasts = visible;
    }

    public void HideAllPanels()
    {
        SetPanelVisible(selectedPanel, false);
        SetPanelVisible(buildOptionsPanel, false);
        foreach (GameObject panel in categoryPanels.Values)
        {
            SetPanelVisible(panel, false);
        }
    }
    #endregion

    #region - Flash red routine
    public void FlashRed(UIButtonReference btnRef)
    {
        if (flashRoutines.TryGetValue(btnRef, out Coroutine running))
            StopCoroutine(running);

        flashRoutines[btnRef] = StartCoroutine(FlashRoutine(btnRef));
    }


    private IEnumerator FlashRoutine(UIButtonReference btnRef)
    {
        Button button = btnRef.button;
        Image buttonImage = button.GetComponent<Image>();
        buttonImage.color = Color.red;

        float t = 0f;
        while (t < flashDuration)
        {
            t += Time.deltaTime;
            buttonImage.color = Color.Lerp(Color.red, originalColors[btnRef], t / flashDuration);
            yield return null;
        }

        buttonImage.color = originalColors[btnRef];
        flashRoutines.Remove(btnRef);
    }
    #endregion
}


/*
Canvas
 └── UIController (GameObject con script UIController)
     ├── MainButtonsPanel
     │    └── BuildButton (único botón principal, con Outline)
     │
     └── BuildOptionsPanel (panel principal de pestañas, desactivado al inicio)
          ├── TabButtonsPanel (fila horizontal de pestañas)
          │    ├── HousingTabButton
          │    ├── WallTabButton
          │    ├── ConsumerTabButton
          │    ├── BonusTabButton
          │    └── ActionsTabButton
          │
          ├── HousingPanel   (GridLayoutGroup o VerticalLayoutGroup)
          │    └── (botones generados dinámicamente, ej: HouseSand)
          │
          ├── WallPanel      (GridLayoutGroup)
          │    └── (botones generados dinámicamente, ej: WallSand)
          │
          ├── ConsumerPanel  (GridLayoutGroup)
          │    └── (botones generados dinámicamente, ej: Cantera)
          │
          ├── BonusPanel     (GridLayoutGroup)
          │    └── (botones generados dinámicamente, ej: InitialTemple)
          │
          └── ActionsPanel   (GridLayoutGroup)
               ├── DigButton        (UIButtonReference, id = "dig")
               ├── AddButton        (UIButtonReference, id = "add")
               ├── FlattenButton    (UIButtonReference, id = "flat")
               └── RecycleButton    (UIButtonReference, id = "recycle")

*/