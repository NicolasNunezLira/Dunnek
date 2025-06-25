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
        public GameObject toDestroy;
        public void DetectConstructionUnderCursor()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            int layerMask = LayerMask.GetMask("Constructions");

            var construccionesParent = GameObject.Find("Construcciones")?.transform;
            if (construccionesParent == null) { toDestroy = null; return; }

            if (Physics.Raycast(ray, out hit, 100f, layerMask))
            {
                GameObject hitGO = hit.collider.gameObject;
                if (hitGO.transform.IsChildOf(construccionesParent))
                {
                    //selectedConstruction = hitGO.transform.root.gameObject;
                    // Buscar el GameObject hijo directo de "Construcciones"
                    Transform current = hitGO.transform;
                    while (current.parent != null && current.parent.name != "Construcciones")
                    {
                        current = current.parent;
                    }
                    toDestroy = current.gameObject;
                    
                    Debug.Log("Construcción seleccionada: " + toDestroy.name);
                }
                else
                {
                    toDestroy = null;
                }
            }
            else
            {
                toDestroy = null;
            }
        }

        public bool DestroyConstruction()
        {
            if (toDestroy == null) { return false; };

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
    }
}