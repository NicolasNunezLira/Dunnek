using UnityEngine;

namespace Utils
{
    public static class RecursivelyFunctions
    {
        public static void SetLayerRecursively(GameObject obj, int newLayer)
        {
            if (obj == null) return;

            obj.layer = newLayer;

            foreach (Transform child in obj.transform)
            {
                if (child != null)
                    SetLayerRecursively(child.gameObject, newLayer);
            }
        }
    }
    
}