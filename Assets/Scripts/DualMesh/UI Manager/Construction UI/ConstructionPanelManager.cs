using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using static DualMesh;

public class UIController : MonoBehaviour
{
    [SerializeField]
    public GameObject buildOptionsPanel;

    [SerializeField]
    public GameObject actionOptionsPanel;
    public Button buildButton;
    public Button destroyButton;
    public Button actionButton;

    private Outline buildOutline;
    private Outline destroyOutline;
    private Outline actionOutline;

    private Button selectedBuildButton;
    private Button selectedActionButton;

    void Start()
    {
        buildButton.onClick.AddListener(OnBuildClicked);
        destroyButton.onClick.AddListener(OnDestroyClicked);
        actionButton.onClick.AddListener(OnActionClicked);

        buildOutline = buildButton.GetComponent<Outline>();
        destroyOutline = destroyButton.GetComponent<Outline>();
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
        DualMesh.Instance.SetMode(PlayingMode.Destroy);
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
        destroyOutline.effectColor = (mode == PlayingMode.Destroy) ? selectedColor : defaultColor;
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
                // Agrega más según tus botones
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
