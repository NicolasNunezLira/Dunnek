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
        public void TryDestroyConstructionUnderCursor()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 100f))
            {
                GameObject hitGO = hit.collider.gameObject;

                // Asegúrate de que es parte de "Construcciones"
               var construccionesParent = GameObject.Find("Construcciones")?.transform;
                if (construccionesParent == null) return;

                if (hitGO.transform.IsChildOf(construccionesParent))
                {
                    GameObject toDestroy = hitGO.transform.root.gameObject;

                    // Buscar la construcción correspondiente en la lista
                    var data = constructionList.Find(c => Vector3.Distance(c.position, toDestroy.transform.position) < 0.1f);

                    if (data != null)
                    {
                        // 🔍 Aquí accedes al soporte
                        foreach (float2 coord in data.support)
                        {
                            int x = (int)coord.x;
                            int z = (int)coord.y;

                            // Restaurar estado si es necesario
                            isConstruible[x, z] = true;
                            duneModel.terrainElev[x, z] = terrainElev[x, z];
                            duneModel.UpdateShadow(x, z, duneModel.dx, duneModel.dz); // si quieres actualizar sombras
                        }

                        constructionList.Remove(data);
                        UnityEngine.Object.Destroy(toDestroy);
                    }
                }
            }
        }
    }
}