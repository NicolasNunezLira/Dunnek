using UnityEngine;
using DunefieldModel_DualMesh;
using System;

namespace Building
{
    public class BuildSystem
    {
        #region Variables
        public GameObject boxPreviewGO, housePreviewGO, wallPreviewGO, activePreview, housePrefab, wallPrefab;
        public ModelDM duneModel;
        public DualMeshConstructor dualMeshConstructor;
        public float buildHeight = 2f;
        public int buildRadius = 1;
        public int buildSize = 2; // puede ser 2 o 3
        private int previewX, previewZ;
        private Vector3 point;
        public DualMesh.BuildMode currentBuildMode;
        public float[,] terrainElev;
        private Quaternion prefabRotation = Quaternion.identity;

        #endregion

        #region Init Build System
        public BuildSystem(
            ModelDM model, DualMeshConstructor constructor,
            GameObject housePrefab, GameObject wallPrefab,
            ref GameObject boxPreviewGO, ref GameObject housePreviewGO, ref GameObject wallPreviewGO,
            DualMesh.BuildMode currentBuildMode, float[,] terrainElev, ref GameObject activePreview)
        {
            duneModel = model;
            dualMeshConstructor = constructor;
            this.housePrefab = housePrefab;
            this.wallPrefab = wallPrefab;
            this.boxPreviewGO = boxPreviewGO;
            this.housePreviewGO = housePreviewGO;
            this.wallPreviewGO = wallPreviewGO;
            this.currentBuildMode = currentBuildMode;
            this.terrainElev = terrainElev;

            this.activePreview = activePreview;

        }
        #endregion

        #region Auxiliar function for building


        public void HandleBuildPreview()
        {
            activePreview.SetActive(true);
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, LayerMask.GetMask("Terrain")))
            {
                point = hit.point;
                int x = Mathf.FloorToInt(point.x * duneModel.xResolution / duneModel.size);
                int z = Mathf.FloorToInt(point.z * duneModel.zResolution / duneModel.size);

                if (x < 0 || z < 0 || x + buildSize > duneModel.xResolution + 1 || z + buildSize > duneModel.zResolution + 1)
                    return;

                float maxY = float.MinValue;
                for (int i = 0; i < buildSize; i++)
                {
                    for (int j = 0; j < buildSize; j++)
                    {
                        int xi = x + i;
                        int zj = z + j;
                        float y = Mathf.Max(duneModel.sandElev[xi, zj], duneModel.terrainElev[xi, zj]);
                        if (y > maxY) maxY = y;
                    }
                }

                float avgX = (x + (buildSize - 1) / 2f) * duneModel.size / duneModel.xResolution;
                float avgZ = (z + (buildSize - 1) / 2f) * duneModel.size / duneModel.zResolution;

                activePreview.transform.position = new Vector3(avgX, maxY + 0.5f, avgZ);

                previewX = x;
                previewZ = z;
            }
        }

        public void ConfirmBuild()
        {
            activePreview.SetActive(false);
            if (currentBuildMode == DualMesh.BuildMode.PlaceHouse)
            {
                GameObjectConstruction(housePrefab, prefabRotation);
                return;
            }

            if (currentBuildMode == DualMesh.BuildMode.Raise)
            {
                GameObjectConstruction(wallPrefab, prefabRotation);
                return;
            }

            // Modo DIG
            float deltaHeight = currentBuildMode == DualMesh.BuildMode.Raise ? buildHeight : -buildHeight;
            float maxHeight = 0;

            for (int dx = -buildRadius; dx <= buildRadius; dx++)
            {
                for (int dz = -buildRadius; dz <= buildRadius; dz++)
                {
                    int nx = previewX + dx;
                    int nz = previewZ + dz;
                    if (nx < 0 || nx > duneModel.sandElev.GetLength(0) || nz < 0 || nz > duneModel.sandElev.GetLength(1)) continue;

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
                    if (nx < 0 || nx > duneModel.sandElev.GetLength(0) || nz < 0 || nz > duneModel.sandElev.GetLength(1)) continue;

                    duneModel.terrainElev[nx, nz] = maxHeight + deltaHeight;
                    terrainElev[nx, nz] = maxHeight + deltaHeight;
                    if (deltaHeight < 0)
                    {
                        duneModel.sandElev[nx, nz] = maxHeight + deltaHeight;
                    }

                    duneModel.UpdateShadow(nx, nz, duneModel.dx, duneModel.dz);
                    duneModel.ActivateCell(nx, nz);
                }
            }
        }

        public void GameObjectConstruction(GameObject prefab, Quaternion rotation)
        {
            float cellSize = duneModel.size / duneModel.xResolution;

            float y = Mathf.Max(
                duneModel.sandElev[previewX, previewZ],
                duneModel.terrainElev[previewX, previewZ]
            );

            Vector3 centerPos = new Vector3(
                (previewX + 0.5f) * cellSize,
                y,
                (previewZ + 0.5f) * cellSize
            );

            GameObject prefabInstance = GameObject.Instantiate(prefab, centerPos, rotation);
            activePreview.SetActive(false);
            prefabInstance.SetActive(true);

            Renderer rend = prefabInstance.GetComponentInChildren<Renderer>();
            Bounds bounds = rend.bounds;
            float targetHeight = bounds.max.y - 0.05f;
            float floorHeight = bounds.min.y;

            // Abarcar los vértices dentro del bounding box del modelo
            int xMin = Mathf.Clamp(Mathf.FloorToInt(bounds.min.x / cellSize), 0, duneModel.xResolution);
            int xMax = Mathf.Clamp(Mathf.CeilToInt(bounds.max.x / cellSize), 0, duneModel.xResolution);
            int zMin = Mathf.Clamp(Mathf.FloorToInt(bounds.min.z / cellSize), 0, duneModel.zResolution);
            int zMax = Mathf.Clamp(Mathf.CeilToInt(bounds.max.z / cellSize), 0, duneModel.zResolution);
            //Debug.Log($"xmin={xMin}, xmax={xMax}, zmin={zMin}, zmax={zMax}");

            for (int x = xMin; x <= xMax; x++)
            {
                for (int z = zMin; z <= zMax; z++)
                {
                    // Posición mundial del vértice
                    float worldX = x * cellSize;
                    float worldZ = z * cellSize;

                    // Verifica si está dentro del área horizontal del modelo
                    if (bounds.Contains(new Vector3(worldX, bounds.center.y, worldZ)))
                    {
                        duneModel.terrainElev[x, z] = targetHeight;
                        terrainElev[x, z] = floorHeight;

                        duneModel.ActivateCell(x, z);
                        duneModel.UpdateShadow(x, z, duneModel.dx, duneModel.dz);
                    }
                }
            }
            return;
        }

        public void UpdateBuildPreviewVisual()
        {
            boxPreviewGO.SetActive(false);
            wallPreviewGO.SetActive(false);
            housePreviewGO.SetActive(false);

            switch (currentBuildMode)
            {
                case DualMesh.BuildMode.Raise:
                    activePreview = wallPreviewGO;
                    break;

                case DualMesh.BuildMode.Dig:
                    activePreview = boxPreviewGO;
                    break;

                case DualMesh.BuildMode.PlaceHouse:
                    activePreview = housePreviewGO;
                    break;
            }
            activePreview.SetActive(true);
        }

        public void HideAllPreviews()
        {
            boxPreviewGO?.SetActive(false);
            wallPreviewGO?.SetActive(false);
            housePreviewGO?.SetActive(false);
        }
        
        public void RotateWallPreview()
        {
            if (currentBuildMode == DualMesh.BuildMode.Dig) return;

            prefabRotation *= Quaternion.Euler(0, 45f, 0);
            activePreview.transform.rotation = prefabRotation;
        }

        #endregion

    }
}