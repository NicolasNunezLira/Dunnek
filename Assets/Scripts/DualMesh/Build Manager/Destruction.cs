using UnityEngine;
using DunefieldModel_DualMesh;
using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine.UIElements;
using cakeslice;
//using System.Numerics;

namespace Building
{
    public partial class BuildSystem
    {
        public GameObject toDestroy, currentHoverObject;

        public Dictionary<Renderer, Material[]> originalMaterials = new();
        public void DetectConstructionUnderCursor()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            int layerMask = LayerMask.GetMask("Constructions");

            var construccionesParent = GameObject.Find("Construcciones")?.transform;
            if (construccionesParent == null) { RestoreHoverMaterials(); toDestroy = null; return; }
            

            if (Physics.Raycast(ray, out hit, 100f, layerMask))
            {
                GameObject hitGO = hit.collider.gameObject;
                if (hitGO.transform.IsChildOf(construccionesParent))
                {
                    Transform current = hitGO.transform;
                    while (current.parent != null && current.parent.name != "Construcciones")
                    {
                        current = current.parent;
                    }

                    GameObject target = current.gameObject;

                    if (toDestroy != target)
                    {
                        RestoreHoverMaterials();
                        MakeRed(target);
                        toDestroy = target;
                    }


                    Debug.Log("Construcción seleccionada: " + toDestroy.name);
                }
                else
                {
                    RestoreHoverMaterials();
                    toDestroy = null;
                }
            }
            else
            {
                RestoreHoverMaterials();
                toDestroy = null;
            }
        }

        public bool DestroyConstruction()
        {
            if (toDestroy == null) { return false; }
            ;

            var data = constructionList.Find(c => Vector3.Distance(c.position, toDestroy.transform.position) < 0.1f);
            Debug.Log($"{toDestroy.name}");
            if (data != null)
            {
                foreach (float2 coord in data.support)
                {
                    int x = (int)coord.x;
                    int z = (int)coord.y;

                    isConstruible[x, z] = true;
                    duneModel.terrainElev[x, z] = terrainElev[x, z];
                    duneModel.UpdateShadow(x, z, duneModel.dx, duneModel.dz);
                }

                string name = toDestroy.name;
                constructionList.Remove(data);
                UnityEngine.Object.Destroy(toDestroy);
                Debug.Log($"{name} destruido");

                toDestroy = null; // limpieza
            }
            return true;
        }

        private void MakeRed(GameObject obj)
        {
            currentHoverObject = obj;

            foreach (var rend in obj.GetComponentsInChildren<Renderer>())
            {
                if (!originalMaterials.ContainsKey(rend))
                {
                    originalMaterials[rend] = rend.materials;
                }

                Material newMat = new Material(rend.material); // copiar material original
                Color c = Color.red;
                c.a = 0.3f;
                newMat.color = c;

                newMat.SetFloat("_Mode", 3); // Transparent
                newMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                newMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                newMat.SetInt("_ZWrite", 0);
                newMat.DisableKeyword("_ALPHATEST_ON");
                newMat.EnableKeyword("_ALPHABLEND_ON");
                newMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                newMat.renderQueue = 3000;

                rend.material = newMat;
            }
        }
        
        
        private void RestoreHoverMaterials()
        {
            if (currentHoverObject == null) return;

            foreach (var rend in currentHoverObject.GetComponentsInChildren<Renderer>())
            {
                if (originalMaterials.TryGetValue(rend, out var originalMats))
                {
                    rend.materials = originalMats;
                }
            }

            currentHoverObject = null;
            originalMaterials.Clear();
        }
    }
}