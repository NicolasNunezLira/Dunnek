using UnityEngine;

public class LookAtCamera : MonoBehaviour
{
    void Update()
    {
        if (Camera.main != null)
        {
            transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward);
        }
    }
}
