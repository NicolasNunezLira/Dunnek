using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public partial class DualMesh : MonoBehaviour
{
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

    void AddCollidersToPrefabs()
    {
        /*
        housePrefabGO.AddComponent<MeshCollider>().convex = true;
        wallPrefabGO.AddComponent<MeshCollider>().convex = true;
        */
        /*
        AddFittedBoxCollider(housePrefabGO);
        AddFittedBoxCollider(wallPrefabGO);
        */
    }

    /*
    void AddFittedBoxCollider(GameObject go)
    {
        var coll = go.AddComponent<BoxCollider>();

        Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        Bounds combinedBounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            combinedBounds.Encapsulate(renderers[i].bounds);
        }

        coll.center = go.transform.InverseTransformPoint(combinedBounds.center);
        coll.size = combinedBounds.size;
    }
    */

    void CreatePreviews()
    {
        shovelPreviewGO = Instantiate(shovelPrefabGO);
        shovelPreviewGO.SetActive(false);
        //MakePreviewTransparent(shovelPreviewGO);

        housePreviewGO = Instantiate(housePrefabGO);
        housePreviewGO.SetActive(false);
        MakePreviewTransparent(housePreviewGO);

        wallPreviewGO = Instantiate(wallPrefabGO);
        wallPreviewGO.SetActive(false);
        MakePreviewTransparent(wallPreviewGO);

        towerPreviewGO = Instantiate(towerPrefabGO);
        towerPreviewGO.SetActive(false);
        MakePreviewTransparent(towerPreviewGO);

        sweeperPreviewGO = Instantiate(sweeperPrefabGO);
        sweeperPreviewGO.SetActive(false);
        MakePreviewTransparent(sweeperPreviewGO);

        circlePreviewGO = Instantiate(circlePrefabGO);
        circlePreviewGO.SetActive(false);
        //MakePreviewTransparent(circlePreviewGO);
    }
    #endregion
}