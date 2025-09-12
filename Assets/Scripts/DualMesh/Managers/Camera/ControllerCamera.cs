using UnityEngine;
using System.Collections;

namespace CameraManager
{
    public class CameraController : MonoBehaviour
    {
        [Header("Camera movement options")]
        public float moveSpeed = 20f;
        public float zoomSpeed = 400f;
        public float rotationSpeed = 5f;
        public float minZoom = -10f;
        public float maxZoom = 10f;

        [Header("Terrain Layouts options")]
        public float minDistanceFromGround = 1f;
        public LayerMask groundMask;

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
            HandleTilt();
            ClampAboveTerrain();
        }

void HandleMovement()
{
    float h = Input.GetAxis("Horizontal");
    float v = Input.GetAxis("Vertical");

    // Calcula un forward y right planos (sin componente Y)
    Vector3 flatForward = transform.forward;
    flatForward.y = 0f;
    flatForward.Normalize();

    Vector3 flatRight = transform.right;
    flatRight.y = 0f;
    flatRight.Normalize();

    Vector3 move = (flatForward * v + flatRight * h).normalized;

    transform.position += move * moveSpeed * Time.deltaTime;
}


        void HandleRotation()
        {
            if (Input.GetMouseButton(1))
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

        void HandleTilt()
        {
            if (Input.GetMouseButton(1)) // botón del medio
            {
                float tiltDelta = -Input.GetAxis("Mouse Y") * rotationSpeed;
                Vector3 angles = transform.eulerAngles;
                float newAngle = angles.x + tiltDelta;

                // Limita la inclinación entre 10 y 80 grados (ajustable)
                //newAngle = Mathf.Clamp(newAngle, 10f, 80f);

                transform.eulerAngles = new Vector3(newAngle, angles.y, angles.z);
                //transform.eulerAngles = new Vector3(Mathf.Lerp(transform.eulerAngles.x, newAngle, Time.deltaTime * 5f), angles.y, angles.z);

            }
        }

        void ClampAboveTerrain()
        {
            Ray ray = new Ray(transform.position + Vector3.up * 100f, Vector3.down);
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, groundMask))
            {
                float groundY = hit.point.y;
                if (transform.position.y < groundY + minDistanceFromGround)
                {
                    Vector3 pos = transform.position;
                    pos.y = groundY + minDistanceFromGround;
                    transform.position = pos;

                    targetHeight = pos.y;
                }
            }

        }
    }
}