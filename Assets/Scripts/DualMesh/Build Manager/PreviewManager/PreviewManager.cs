using System.Collections.Generic;
using UnityEngine;
using Utils;
using Data;
public class PreviewManager : Singleton<PreviewManager>
{
    public GameObject BuildsPreviewParent, ActionsPreviewParent;
    public Dictionary<ConstructionType, GameObject> buildPreviews = new();

    public Dictionary<DualMesh.ActionMode, GameObject> actionPreviews = new();

    protected override void Awake()
    {
        base.Awake();
        InitializeBuildPreviews();
        InitializeActionPreviews();
    }

    private void InitializeBuildPreviews()
    {
        if (BuildsPreviewParent == null)
        {
            BuildsPreviewParent = new GameObject("Builds Previews");
        }

        var builds = ConstructionConfig.Instance.constructionConfig;

        foreach (var (key, item) in builds)
        {
            GameObject preview = Instantiate(item.loadedPrefab, BuildsPreviewParent.transform);
            MakePreviewTransparent(preview);
            preview.SetActive(false);

            buildPreviews[key] = preview;
        }
    }

    private void InitializeActionPreviews()
    {
        if (BuildsPreviewParent == null)
        {
            ActionsPreviewParent = new GameObject("Actions Previews");
        }

        var actions = ActionConfig.Instance.actionsConfig;

        foreach (var (key, item) in actions)
        {
            GameObject preview = Instantiate(item.loadedPrefab, BuildsPreviewParent.transform);
            MakePreviewTransparent(preview);
            preview.SetActive(false);

            actionPreviews[key] = preview;
        }
    }
    
    void MakePreviewTransparent(GameObject obj)
    {
        foreach (var rend in obj.GetComponentsInChildren<Renderer>())
        {
            Material mat = rend.material; // Esto instancia una copia
            Color c = Color.green;
            c.a = 0.3f;
            mat.color = c;
            mat.SetFloat("_Mode", 3); // Transparent
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
        }

        foreach (var col in obj.GetComponentsInChildren<Collider>())
        {
            col.enabled = false;
        }
    }
}