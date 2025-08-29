using UnityEngine;
using StormSystem;
using TMPro;

public class WindUI : MonoBehaviour
{
    [SerializeField] private RectTransform arrow;
    [SerializeField] private TextMeshProUGUI windLabel;
    [SerializeField] private Camera mainCamera;

    private Vector2 windDir;

    private void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        StormManager.Instance.OnWindChanged += UpdateWindDir;
        UpdateWindDir(StormManager.Instance.currentDirection);
    }

    private void Update()
    {
        if (windLabel != null)
        {
            int turnsRemaining = StormManager.Instance.eventTurnsRemaining;
            windLabel.text = "Remaining turns " + turnsRemaining;
        }

        if (arrow != null && mainCamera != null)
        {
            Vector3 camForward = mainCamera.transform.forward;
            camForward.y = 0;
            camForward.Normalize();

            Vector3 wind3D = new Vector3(windDir.x, 0, windDir.y);

            float angle = Vector3.SignedAngle(camForward, wind3D, Vector3.up);

            arrow.localEulerAngles = new Vector3(0, 0, -angle);
        }
    }

    private void OnDestroy()
    {
        if (StormManager.Instance != null)
        {
            StormManager.Instance.OnWindChanged -= UpdateWindDir;
        }
    }

    private void UpdateWindDir(Vector2 newDir)
    {
        windDir = newDir.normalized;
    }
}
