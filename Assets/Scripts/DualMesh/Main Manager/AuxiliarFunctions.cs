using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public partial class DualMesh : MonoBehaviour
{
    public GameObject previewParentGO;
    #region Auxiliar Functions
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

    void CreatePreviews()
    {
        if (previewParentGO == null)
        {
            previewParentGO = new GameObject("PreviewObjects");
        }

        shovelPreviewGO = Instantiate(shovelPrefabGO, previewParentGO.transform);
        shovelPreviewGO.SetActive(false);

        housePreviewGO = Instantiate(housePrefabGO, previewParentGO.transform);
        housePreviewGO.SetActive(false);
        MakePreviewTransparent(housePreviewGO);

        wallPreviewGO = Instantiate(wallPrefabGO, previewParentGO.transform);
        wallPreviewGO.SetActive(false);
        MakePreviewTransparent(wallPreviewGO);

        towerPreviewGO = Instantiate(towerPrefabGO, previewParentGO.transform);
        towerPreviewGO.SetActive(false);
        MakePreviewTransparent(towerPreviewGO);

        sweeperPreviewGO = Instantiate(sweeperPrefabGO, previewParentGO.transform);
        sweeperPreviewGO.SetActive(false);
        MakePreviewTransparent(sweeperPreviewGO);

        circlePreviewGO = Instantiate(circlePrefabGO, previewParentGO.transform);
        circlePreviewGO.SetActive(false);
    }
    #endregion
}