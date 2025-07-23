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

    void Start()
    {
        buildButton.onClick.AddListener(OnBuildClicked);
        destroyButton.onClick.AddListener(OnDestroyClicked);
        actionButton.onClick.AddListener(OnActionClicked);

        buildOutline = buildButton.GetComponent<Outline>();
        destroyOutline = destroyButton.GetComponent<Outline>();
        actionOutline = destroyButton.GetComponent<Outline>();

        buildOptionsPanel.SetActive(false);
        actionOptionsPanel.SetActive(false);

        StartCoroutine(WaitAndInitialize());
    }

    void OnBuildClicked()
    {
        DualMesh.instance.SetMode(PlayingMode.Build);
    }

    void OnDestroyClicked()
    {
        DualMesh.instance.SetMode(PlayingMode.Destroy);
    }

    void OnActionClicked()
    {
        DualMesh.instance.SetMode(PlayingMode.Build);
    }

    IEnumerator WaitAndInitialize()
    {
        yield return new WaitUntil(() => DualMesh.instance != null);
        //DualMesh.instance.uiController = this;
        UpdateButtonVisuals(DualMesh.instance.inMode);
    }

    public void UpdateButtonVisuals(PlayingMode mode)
    {
        Color selectedColor = Color.green;
        Color defaultColor = new Color(0, 0, 0, 0);

        buildOutline.effectColor = (mode == PlayingMode.Build) ? selectedColor : defaultColor;
        destroyOutline.effectColor = (mode == PlayingMode.Destroy) ? selectedColor : defaultColor;
        actionOutline.effectColor = (mode == PlayingMode.Action) ? selectedColor : defaultColor;


        buildOptionsPanel.SetActive(mode == PlayingMode.Build);
    }
}
