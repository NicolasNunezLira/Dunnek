using UnityEngine;
using DunefieldModel_DualMesh;
using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine.UIElements;
//using System.Numerics;

namespace Building
{
    public partial class BuildSystem
    {
        public void TriggerInvalidPlacementShake()
        {
            if (shakeCoroutine != null)
                return; // Evita múltiples shakes superpuestos

            shakeCoroutine = activePreview.GetComponent<MonoBehaviour>().StartCoroutine(ShakePreview());
        }

        private System.Collections.IEnumerator ShakePreview()
        {
            Vector3 originalPos = activePreview.transform.position;

            float duration = 0.3f;
            float elapsed = 0f;
            float magnitude = 0.1f;

            while (elapsed < duration)
            {
                float x = UnityEngine.Random.Range(-1f, 1f) * magnitude;
                float z = UnityEngine.Random.Range(-1f, 1f) * magnitude;

                activePreview.transform.position = originalPos + new Vector3(x, 0, z);

                elapsed += Time.deltaTime;
                yield return null;
            }

            activePreview.transform.position = originalPos;
            shakeCoroutine = null;
        }
    }
}