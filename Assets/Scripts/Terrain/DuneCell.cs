using UnityEngine;

public class DuneCell
{
    public float height;
    public float shadow;
    public GameObject tileObj;

    public DuneCell(GameObject obj, float h, float s = 0f)
    {
        tileObj = obj;
        height = h;
        shadow = s;
    }

    public void UpdateVisual(float tileSize, bool shadow=false)
    {
        float clampedHeight = Mathf.Max(height, 0.1f);
        tileObj.transform.localScale = new Vector3(tileSize, clampedHeight, tileSize);
        tileObj.transform.position = new Vector3(tileObj.transform.position.x, clampedHeight / 2f, tileObj.transform.position.z);

        // Color según altura y sombra
        float normalized = Mathf.InverseLerp(0f, 10f, height);
        Color baseColor = Color.Lerp(new Color(0.9f, 0.8f, 0.6f), new Color(0.7f, 0.6f, 0.3f), normalized);
        
        tileObj.GetComponent<Renderer>().material.color = baseColor;
    }
}
