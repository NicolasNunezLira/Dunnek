using UnityEngine;
using System.Collections.Generic;
using Unity.Mathematics;
using System.Text.RegularExpressions;
using Data;
using System;

namespace Building
{
    public partial class BuildSystem
    {
        public GameObject toDestroy, currentHoverObject;

        public int idToDestroy = -1;

        public Dictionary<Renderer, Material[]> originalMaterials = new();
        public void DetectConstructionUnderCursor(Color color)
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
                        ChangeColor(target, color);
                        toDestroy = target;
                    }

                    Debug.Log("Construcción seleccionada: " + toDestroy.name);

                    idToDestroy = int.Parse(Regex.Match(toDestroy.name, @"\d+$").Value);
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
            if (toDestroy == null) return false;

            ConstructionData data = constructions[idToDestroy];

            resourceManager.AddResource(ResourceSystem.ResourceName.Work, -constructionsConfigs.constructionConfig[data.type].rate.Work);
            resourceManager.AddResource(ResourceSystem.ResourceName.Sand, Mathf.Floor(constructionsConfigs.constructionConfig[data.type].cost.Sand / 2));

            // Liberar celdas ocupadas
            foreach (int2 coord in data.support)
            {
                int cx = coord.x;
                int cz = coord.y;

                if (!constructionGrid.IsValid(cx, cz)) continue;

                if (duneModel.sand[cx, cz] >= data.buildHeight + data.floorHeight)
                {
                    duneModel.sand[cx, cz] -= data.buildHeight;
                }

                duneModel.terrainShadow[cx, cz] = duneModel.terrain[cx, cz];
                duneModel.ActivateCell(cx, cz);
                duneModel.UpdateShadow(cx, cz, duneModel.dx, duneModel.dz);
            }
            foreach (int2 coord in data.boundarySupport)
            {
                int cx = coord.x;
                int cz = coord.y;

                if (!constructionGrid.IsValid(cx, cz)) continue;

                duneModel.terrainShadow[cx, cz] = duneModel.terrain[cx, cz]; // restaura altura original
                duneModel.ActivateCell(cx, cz);
                duneModel.UpdateShadow(cx, cz, duneModel.dx, duneModel.dz);
            }

            constructionGrid.RemoveConstructionById(idToDestroy);

            constructions.Remove(idToDestroy);
            string name = toDestroy.name;
            UnityEngine.Object.Destroy(toDestroy);
            Debug.Log($"{name} destruido");

            toDestroy = null;
            idToDestroy = -1;
            return true;
        }

        private void ChangeColor(GameObject obj, Color color)
        {
            currentHoverObject = obj;

            foreach (var rend in obj.GetComponentsInChildren<Renderer>())
            {
                if (!originalMaterials.ContainsKey(rend))
                {
                    originalMaterials[rend] = rend.materials;
                }

                Material newMat = new Material(rend.material); // copiar material original
                Color c = color;
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
        
        
        public void RestoreHoverMaterials()
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