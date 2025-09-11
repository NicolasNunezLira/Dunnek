using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using ConstructionSystem;
using System.Linq;

public class UIController : MonoBehaviour
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

    public string currentCategory { get; private set; }

    void Start()
    {
        // Listener principal
        buildButton.onClick.AddListener(OnBuildClicked);

        // Guardar outline
        buildOutline = buildButton.GetComponent<Outline>();

        // Ocultar panel inicial
        housingPanel.SetActive(false);
        wallPanel.SetActive(false);
        consumerPanel.SetActive(false);
        bonusPanel.SetActive(false);
        actionsPanel.SetActive(false);
        buildOptionsPanel.SetActive(false);

        // Mapear categorías
        categoryPanels["Housing"] = housingPanel;
        categoryPanels["Wall"] = wallPanel;
        categoryPanels["Consumer"] = consumerPanel;
        categoryPanels["BonusProvider"] = bonusPanel;
        categoryPanels["Actions"] = actionsPanel;

        // Tabs
        housingTabButton.onClick.AddListener(() => ShowCategory("Housing"));
        wallTabButton.onClick.AddListener(() => ShowCategory("Wall"));
        consumerTabButton.onClick.AddListener(() => ShowCategory("Consumer"));
        bonusTabButton.onClick.AddListener(() => ShowCategory("BonusProvider"));
        actionsTabButton.onClick.AddListener(() => ShowCategory("Actions"));

        // Inicializar botones
        GenerateConstructionButtons();
        InitializeActionButtons();

        // Pestaña inicial
        ShowCategory("Housing");
    }

    #region --- Main Button ---
    void OnBuildClicked()
    {
        ShowCategory(currentCategory ?? "Housing");
        DualMesh.Instance.SetMode(DualMesh.PlayingMode.Build);
        UpdateMainButtonVisuals(DualMesh.PlayingMode.Build);
    }

    public void UpdateMainButtonVisuals(DualMesh.PlayingMode mode)
    {
        Color selectedColor = Color.green;
        Color defaultColor = new Color(0, 0, 0, 0);

        buildOutline.effectColor = (mode == DualMesh.PlayingMode.Build) ? selectedColor : defaultColor;
        buildOptionsPanel.SetActive(mode == DualMesh.PlayingMode.Build);
    }
    #endregion

    #region --- Construcciones ---
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

    void CreateConstructionButton(ConstructionConfig.ConfigData config)
    {
        string category = config.category.ToString();
        if (!categoryPanels.ContainsKey(category))
            return;

        Transform parentPanel = categoryPanels[category].transform;
        GameObject btnGO = Instantiate(constructionButtonPrefab, parentPanel);

        UIButtonReference btnRef = btnGO.GetComponent<UIButtonReference>();
        btnRef.buttonID = config.codeName;

        // si tiene sprite, mostrarlo
        if (config.icon != null && btnRef.iconImage != null)
            btnRef.iconImage.sprite = config.icon;

        // listener
        btnRef.button.onClick.AddListener(() => OnConstructionClicked(config.codeName));

        constructionButtons[config.codeName] = btnRef;
    }

    public void ShowCategory(string category)
    {
        currentCategory = category;

        foreach (var kvp in categoryPanels)
            kvp.Value.SetActive(kvp.Key == category);
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

        // Llamar a tu sistema de construcción
        // ConstructionSystemManager.Instance.Select(config);

        UpdateSelectedVisual(codeName);
    }

    public void UpdateSelectedVisual(string selectedID)
    {
        Color selectedColor = Color.green;
        Color defaultColor = new Color(0, 0, 0, 0);

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

    #region --- Acciones ---
    void InitializeActionButtons()
    {
        foreach (var btnRef in actionsPanel.GetComponentsInChildren<UIButtonReference>())
        {
            string id = btnRef.buttonID;
            actionButtons[id] = btnRef;
            btnRef.button.onClick.AddListener(() => OnActionOptionClicked(id));
        }
    }

    void OnActionOptionClicked(string id)
    {
        if (!actionButtons.ContainsKey(id)) return;

        switch (id)
        {
            case "dig":
                DualMesh.Instance.SetActionType(DualMesh.ActionMode.Dig);
                break;
            case "add":
                DualMesh.Instance.SetActionType(DualMesh.ActionMode.AddSand);
                break;
            case "flat":
                DualMesh.Instance.SetActionType(DualMesh.ActionMode.Flat);
                break;
            case "recycle":
                DualMesh.Instance.SetActionType(DualMesh.ActionMode.Recycle);
                break;
        }

        UpdateActionsButtonVisual(id);
    }

    public void UpdateActionsButtonVisual(string selectedID)
    {
        Color selectedColor = Color.green;
        Color defaultColor = new Color(0, 0, 0, 0);

        foreach (var kvp in actionButtons)
            kvp.Value.outline.effectColor = (kvp.Key == selectedID) ? selectedColor : defaultColor;
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