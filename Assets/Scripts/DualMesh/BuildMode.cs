using UnityEngine;
using DunefieldModel_DualMesh;
using System;

namespace Building
{
    public class BuildSystem
    {
        #region Variables
        public GameObject buildPreviewGO;
        public ModelDM duneModel;
        public float buildHeight = 2f;
        public int buildRadius = 1;
        #endregion

        #region Init Build System
        public BuildSystem(ModelDM model, GameObject previewGO)
        {
            duneModel = model;
            buildPreviewGO = previewGO;
        }
        #endregion

        #region Auxiliar function for building
        public void HandleBuildPreview()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, LayerMask.GetMask("Terrain")))
            {
                Vector3 point = hit.point;
                int x = Mathf.RoundToInt(point.x);
                int z = Mathf.RoundToInt(point.z);

                float y = Mathf.Max(duneModel.sandElev[x, z], duneModel.terrainElev[x, z]);

                buildPreviewGO.transform.position = new Vector3(x, y + 0.5f, z); 
            }
        }

        public void ConfirmBuild()
        {
            Vector3 pos = buildPreviewGO.transform.position;
            int x = Mathf.RoundToInt(pos.x);
            int z = Mathf.RoundToInt(pos.z);

            for (int dx = -buildRadius; dx <= buildRadius; dx++)
            {
                for (int dz = -buildRadius; dz <= buildRadius; dz++)
                {
                    int nx = x + dx;
                    int nz = z + dz;
                    if (nx < 0 || nx > duneModel.sandElev.GetLength(0)
                        || nz < 0 || nz > duneModel.sandElev.GetLength(1)) continue;

                    duneModel.terrainElev[nx, nz] = Math.Max(duneModel.sandElev[nx, nz], duneModel.terrainElev[nx, nz]) + buildHeight;
                    duneModel.ActivateCell(nx, nz);
                }
            }


            buildPreviewGO.SetActive(false);
        }

        #endregion

    }
}