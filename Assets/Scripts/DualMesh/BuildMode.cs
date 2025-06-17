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
        public DualMeshConstructor dualMeshConstructor;
        public float buildHeight = 2f;
        public int buildRadius = 1;
        private int previewX, previewZ;
        public DualMesh.BuildMode currentBuildMode;

        #endregion

        #region Init Build System
        public BuildSystem(ModelDM model, DualMeshConstructor constructor, GameObject previewGO, DualMesh.BuildMode currentBuildMode)
        {
            duneModel = model;
            dualMeshConstructor = constructor;
            buildPreviewGO = previewGO;
            this.currentBuildMode = currentBuildMode;
        }
        #endregion

        #region Auxiliar function for building
        public void HandleBuildPreview()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, LayerMask.GetMask("Terrain")))
            {
                Vector3 point = hit.point;
                int x = Mathf.FloorToInt(point.x * duneModel.xResolution / duneModel.size);
                int z = Mathf.FloorToInt(point.z * duneModel.zResolution / duneModel.size);

                // Verifica que esté dentro del rango del terreno
                if (x < 0 || x >= duneModel.xResolution + 1 || z < 0 || z >= duneModel.zResolution + 1)
                    return;

                float y = Mathf.Max(duneModel.sandElev[x, z], duneModel.terrainElev[x, z]);
                buildPreviewGO.transform.position = new Vector3(x * duneModel.size / duneModel.xResolution, y + 0.5f, z * duneModel.size / duneModel.zResolution);

                previewX = x;
                previewZ = z;
            }
        }

        public void ConfirmBuild()
        {
            float deltaHeight = currentBuildMode == DualMesh.BuildMode.Raise ? buildHeight : -buildHeight;

            float maxHeight = 0;
            for (int dx = -buildRadius; dx <= buildRadius; dx++)
            {
                for (int dz = -buildRadius; dz <= buildRadius; dz++)
                {
                    int nx = previewX + dx;
                    int nz = previewZ + dz;
                    if (nx < 0 || nx > duneModel.sandElev.GetLength(0)
                        || nz < 0 || nz > duneModel.sandElev.GetLength(1)) continue;

                    float h = Math.Max(duneModel.sandElev[nx, nz], duneModel.terrainElev[nx, nz]);
                    if (h > maxHeight) maxHeight = h;
                }
            }

            for (int dx = -buildRadius; dx <= buildRadius; dx++)
            {
                for (int dz = -buildRadius; dz <= buildRadius; dz++)
                {
                    int nx = previewX + dx;
                    int nz = previewZ + dz;
                    if (nx < 0 || nx > duneModel.sandElev.GetLength(0)
                        || nz < 0 || nz > duneModel.sandElev.GetLength(1)) continue;

                    duneModel.terrainElev[nx, nz] = maxHeight + deltaHeight;
                    if (deltaHeight < 0) { duneModel.sandElev[nx, nz] = maxHeight + deltaHeight; }
                    duneModel.UpdateShadow(nx, nz, duneModel.dx, duneModel.dz);
                    duneModel.ActivateCell(nx, nz);
                }
            }


            buildPreviewGO.SetActive(false);
        }

        public void UpdateBuildPreviewVisual()
        {
            var renderer = buildPreviewGO.GetComponent<MeshRenderer>();
            if (renderer == null) return;

            switch (currentBuildMode)
            {
                case DualMesh.BuildMode.Raise:
                    renderer.material.color = Color.green; // Terreno sube
                    break;
                case DualMesh.BuildMode.Dig:
                    renderer.material.color = Color.red; // Hacer agujero
                    break;
            }
        }

        #endregion

    }
}