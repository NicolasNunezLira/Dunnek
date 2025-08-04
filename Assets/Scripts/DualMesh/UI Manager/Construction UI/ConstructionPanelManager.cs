using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using static DualMesh;

public class UIController : MonoBehaviour
{
    [Header("Main Buttons")]
    [SerializeField]
    [Tooltip("Recycle Button")]
    public Button recycleButton;
    [SerializeField]
    [Tooltip("Build Button")]
    public Button buildButton;
    [SerializeField]
    [Tooltip("Action Button")]
    public Button actionButton;

    [Header("Options subpanels")]
    [SerializeField]
    [Tooltip("Builds options panel")]
    public GameObject buildOptionsPanel;
    [SerializeField]
    [Tooltip("Action options panel")]
    public GameObject actionOptionsPanel;

    private Outline buildOutline;
    private Outline recycleOutline;
    private Outline actionOutline;

    private Button selectedBuildButton;
    private Button selectedActionButton;

    void Start()
    {
        buildButton.onClick.AddListener(OnBuildClicked);
        recycleButton.onClick.AddListener(OnDestroyClicked);
        actionButton.onClick.AddListener(OnActionClicked);

        buildOutline = buildButton.GetComponent<Outline>();
        recycleOutline = recycleButton.GetComponent<Outline>();
        actionOutline = actionButton.GetComponent<Outline>();

        buildOptionsPanel.SetActive(false);
        actionOptionsPanel.SetActive(false);

        StartCoroutine(WaitAndInitialize());

        InitializeActionButtons();
        InitializeBuildButtons();
    }

    void OnBuildClicked()
    {
        DualMesh.Instance.SetMode(PlayingMode.Build);
    }

    void OnDestroyClicked()
    {
        DualMesh.Instance.SetMode(PlayingMode.Recycle);
    }

    void OnActionClicked()
    {
        DualMesh.Instance.SetMode(PlayingMode.Action);
    }

    IEnumerator WaitAndInitialize()
    {
        yield return new WaitUntil(() => DualMesh.Instance != null);
        UpdateButtonVisuals(DualMesh.Instance.inMode);
    }

    public void UpdateButtonVisuals(PlayingMode mode)
    {
        Color selectedColor = Color.green;
        Color defaultColor = new Color(0, 0, 0, 0);

        buildOutline.effectColor = (mode == PlayingMode.Build) ? selectedColor : defaultColor;
        recycleOutline.effectColor = (mode == PlayingMode.Recycle) ? selectedColor : defaultColor;
        actionOutline.effectColor = (mode == PlayingMode.Action) ? selectedColor : defaultColor;


        buildOptionsPanel.SetActive(mode == PlayingMode.Build);
        actionOptionsPanel.SetActive(mode == PlayingMode.Action);
    }

    public void UpdateBuildsButtonVisual(BuildMode mode)
    {
        Color selectedColor = Color.green;
        Color defaultColor = new Color(0, 0, 0, 0);

        Button[] buildButtons = buildOptionsPanel.GetComponentsInChildren<Button>();

        foreach (Button btn in buildButtons)
        {
            Outline outline = btn.GetComponent<Outline>();
            if (outline == null) continue;

            // Match button name to BuildMode
            bool isSelected = false;
            switch (mode)
            {
                case BuildMode.PlaceHouse:
                    isSelected = btn.name == "HouseButton";
                    break;
                case BuildMode.PlaceWallBetweenPoints:
                    isSelected = btn.name == "WallButton";
                    break;
                case BuildMode.PlaceCantera:
                    isSelected = btn.name == "CanteraButton";
                    break;
            }

            outline.effectColor = isSelected ? selectedColor : defaultColor;

            if (isSelected) selectedBuildButton = btn;
        }
    }

    public void UpdateActionsButtonVisual(ActionMode mode)
    {
        Color selectedColor = Color.green;
        Color defaultColor = new Color(0, 0, 0, 0);

        Button[] actionButtons = actionOptionsPanel.GetComponentsInChildren<Button>();

        foreach (Button btn in actionButtons)
        {
            Outline outline = btn.GetComponent<Outline>();
            if (outline == null) continue;

            bool isSelected = false;
            switch (mode)
            {
                case ActionMode.Dig:
                    isSelected = btn.name == "DigButton";
                    break;
                case ActionMode.AddSand:
                    isSelected = btn.name == "AddButton";
                    break;
                case ActionMode.Flat:
                    isSelected = btn.name == "FlattenButton";
                    break;
            }

            outline.effectColor = isSelected ? selectedColor : defaultColor;

            if (isSelected) selectedActionButton = btn;
        }
    }


    void InitializeBuildButtons()
    {
        Button[] buildButtons = buildOptionsPanel.GetComponentsInChildren<Button>();

        foreach (Button btn in buildButtons)
        {
            string name = btn.gameObject.name;
            btn.onClick.AddListener(() => OnBuildOptionClicked(name));
        }
    }

    void InitializeActionButtons()
    {
        Button[] actionButtons = actionOptionsPanel.GetComponentsInChildren<Button>();

        foreach (Button btn in actionButtons)
        {
            string name = btn.gameObject.name;
            btn.onClick.AddListener(() => OnActionOptionClicked(name));
        }
    }

    void OnBuildOptionClicked(string buttonName)
    {
        switch (buttonName)
        {
            case "HouseButton":
                DualMesh.Instance.SetBuildType(BuildMode.PlaceHouse);
                UpdateBuildsButtonVisual(BuildMode.PlaceHouse);
                break;
            case "WallButton":
                DualMesh.Instance.SetBuildType(BuildMode.PlaceWallBetweenPoints);
                UpdateBuildsButtonVisual(BuildMode.PlaceWallBetweenPoints);
                break;
            case "CanteraButton":
                DualMesh.Instance.SetBuildType(BuildMode.PlaceCantera);
                UpdateBuildsButtonVisual(BuildMode.PlaceCantera);
                break;
        }
    }

    void OnActionOptionClicked(string buttonName)
    {
        switch (buttonName)
        {
            case "DigButton":
                DualMesh.Instance.SetActionType(ActionMode.Dig);
                break;
            case "AddButton":
                DualMesh.Instance.SetActionType(ActionMode.AddSand);
                break;
            case "FlattenButton":
                DualMesh.Instance.SetActionType(ActionMode.Flat);
                break;
        }
    }
}
