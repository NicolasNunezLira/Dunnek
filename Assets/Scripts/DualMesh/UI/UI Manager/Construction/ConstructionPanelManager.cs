using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using ConstructionSystem;
using System.Linq;
using Utils;
using ResourceSystem;
using System;

public class UIController : Singleton<UIController>
{
    [Header("Main Buttons")]
    [SerializeField] public Button buildButton; // único botón principal

    [Header("Options Panels")]
    [SerializeField] public GameObject buildOptionsPanel;

    [Header("Build Category Panels")]
    [SerializeField] public GameObject housingPanel;
    [SerializeField] public GameObject wallPanel;
    [SerializeField] public GameObject consumerPanel;
    [SerializeField] public GameObject bonusPanel;
    [SerializeField] public GameObject actionsPanel; // nueva pestaña para acciones

    [Header("Category Tabs")]
    [SerializeField] public Button housingTabButton;
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

    public string currentCategory { get; private set; } = "Housing";
    public string currentBuilding { get; private set; }
    public string currentAction { get; private set; }

    private Color selectedColor = Color.green;
    private Color defaultColor = new Color(0, 0, 0, 0);

    protected override void Awake()
    {
        base.Awake();

        buildButton.onClick.AddListener(OnBuildClicked);

        buildOutline = buildButton.GetComponent<Outline>();

        categoryPanels["Housing"] = housingPanel;
        categoryPanels["Wall"] = wallPanel;
        categoryPanels["Consumer"] = consumerPanel;
        categoryPanels["BonusProvider"] = bonusPanel;
        categoryPanels["Actions"] = actionsPanel;

        housingTabButton.onClick.AddListener(() => ShowCategory("Housing"));
        wallTabButton.onClick.AddListener(() => ShowCategory("Wall"));
        consumerTabButton.onClick.AddListener(() => ShowCategory("Consumer"));
        bonusTabButton.onClick.AddListener(() => ShowCategory("BonusProvider"));
        actionsTabButton.onClick.AddListener(() => ShowCategory("Actions"));

        tabButtons["Housing"] = housingTabButton;
        tabButtons["Wall"] = wallTabButton;
        tabButtons["Consumer"] = consumerTabButton;
        tabButtons["BonusProvider"] = bonusTabButton;
        tabButtons["Actions"] = actionsTabButton;

        GenerateConstructionButtons();
        InitializeActionButtons();

        HideAllPanels();
    }

    #region --- Main Button ---
    void OnBuildClicked()
    {
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
        btnRef.button.onClick.AddListener(() => OnConstructionClicked(config.codeName));

        RefreshButtonCosts(btnRef, config);

        constructionButtons[config.codeName] = btnRef;

        StartCoroutine(RebuildNextFrame(currentPanel));
    }

    public void ShowCategory(string category)
    {
        currentCategory = category;
        //Debug.Log($"Current Category {currentCategory}");

        foreach (var kvp in categoryPanels)
        {
            tabButtons[kvp.Key].GetComponent<Outline>().effectColor = (currentCategory == kvp.Key) ? selectedColor : defaultColor;
            SetPanelVisible(kvp.Value, kvp.Key == category);
            //Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(kvp.Value.GetComponent<RectTransform>());
        }
    }

    bool IsUnlocked(string codeName)
    {
        return ConstructionUnlockerManager.UnlockedConstructions.Contains(codeName);
    }

    void OnConstructionClicked(string codeName)
    {
        if (!constructionButtons.ContainsKey(codeName)) return;

        var config = ConstructionConfig.Instance.ConstructionConfigs[codeName];
        Debug.Log($"Construcción seleccionada: {config.codeName} ({config.category})");

        currentBuilding = codeName;
        currentCategory = config.category.ToString();

        DualMesh.Instance.SetBuildType(codeName);

        UpdateSelectedVisual(codeName);
    }

    public void UpdateSelectedVisual(string selectedID)
    {
        currentBuilding = selectedID;

        foreach (var kvp in constructionButtons)
            kvp.Value.outline.effectColor = (kvp.Key == selectedID) ? selectedColor : defaultColor;
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

        btnRef.button.onClick.AddListener(() => OnActionOptionClicked(config.type));

        actionButtons[config.type] = btnRef;

        StartCoroutine(RebuildNextFrame(parentPanel.gameObject));
    }

    void OnActionOptionClicked(string id)
    {
        currentAction = id;

        if (!actionButtons.ContainsKey(id)) return;

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

        UpdateActionsButtonVisual(id);
    }

    public void UpdateActionsButtonVisual(string selectedID)
    {
        currentAction = selectedID;

        foreach (var kvp in actionButtons)
            kvp.Value.outline.effectColor = (kvp.Key == selectedID) ? selectedColor : defaultColor;
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
                RefreshButtonCosts(btnRef, config);
                LayoutRebuilder.ForceRebuildLayoutImmediate(btnRef.GetComponent<RectTransform>());
            }
        }

        foreach (var panel in categoryPanels.Values)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(panel.GetComponent<RectTransform>());
        }
    }
    #endregion

    #region - Helper
    private void RefreshButtonCosts(UIButtonReference btnRef, ConstructionConfig.ConfigData config)
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

        StartCoroutine(RebuildNextFrame(btnRef.costPanel.gameObject));
    }

    private System.Collections.IEnumerator RebuildNextFrame(GameObject panel)
    {
        yield return null;
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(panel.GetComponent<RectTransform>());
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
        SetPanelVisible(buildOptionsPanel, false);
        foreach (GameObject panel in categoryPanels.Values)
        {
            SetPanelVisible(panel, false);
        }
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