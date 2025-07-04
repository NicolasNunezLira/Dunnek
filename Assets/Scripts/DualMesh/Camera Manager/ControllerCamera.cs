using UnityEngine;
using System.Collections;

namespace CameraManager
{
    public class CameraController : MonoBehaviour
    {
        public float moveSpeed = 20f;
        public float zoomSpeed = 100f;
        public float rotationSpeed = 5f;
        public float minZoom = 10f;
        public float maxZoom = 80f;

        private float targetHeight;
        private float currentZoom;

        public bool isControllable = true;

        public static CameraController Instance;

        void Awake()
        {
            Instance = this;   
        }

        void Start()
        {
            currentZoom = Camera.main.transform.eulerAngles.x;
            targetHeight = Camera.main.transform.position.y;
        }

        void Update()
        {
            if (!isControllable)
                return;

            HandleMovement();
            HandleRotation();
            HandleZoom();
        }

        void HandleMovement()
        {
            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");

            Vector3 move = (transform.forward * v + transform.right * h).normalized;
            transform.position += move * moveSpeed * Time.deltaTime;
        }

        void HandleRotation()
        {
            if (Input.GetMouseButton(1)) // clic derecho
            {
                float rotX = Input.GetAxis("Mouse X") * rotationSpeed;
                transform.Rotate(0f, rotX, 0f, Space.World);
            }
        }

        void HandleZoom()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                targetHeight -= scroll * zoomSpeed * Time.deltaTime;
                targetHeight = Mathf.Clamp(targetHeight, minZoom, maxZoom);
            }

            Vector3 pos = transform.position;
            pos.y = Mathf.Lerp(pos.y, targetHeight, Time.deltaTime * 5f);
            transform.position = pos;
        }


        public IEnumerator MoveCameraTo(Vector3 targetWorldPos, float duration = 1f)
        {
            isControllable = false;

            Vector3 startPos = transform.position;
            Quaternion startRot = transform.rotation;

            // Posición destino: ajusta el offset si quieres ángulo superior
            Vector3 offset = new Vector3(-6, 3f, -6f); // ajustable
            Vector3 endPos = targetWorldPos + offset;
            Quaternion endRot = Quaternion.LookRotation(targetWorldPos - endPos);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                float t = elapsed / duration;
                transform.position = Vector3.Lerp(startPos, endPos, t);
                transform.rotation = Quaternion.Slerp(startRot, endRot, t);
                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.position = endPos;
            transform.rotation = endRot;
        }

    }
}