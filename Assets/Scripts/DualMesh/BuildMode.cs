using UnityEngine;
using DunefieldModel_DualMesh;
using System;

namespace Building
{
    public class BuildSystem
    {
        #region Variables
        public GameObject buildPreviewGO, housePrefab;
        public ModelDM duneModel;
        public DualMeshConstructor dualMeshConstructor;
        public float buildHeight = 2f;
        public int buildRadius = 1;
        public int buildSize = 2; // puede ser 2 o 3
        private int previewX, previewZ;
        private Vector3 point;
        public DualMesh.BuildMode currentBuildMode;

        #endregion

        #region Init Build System
        public BuildSystem(ModelDM model, DualMeshConstructor constructor, GameObject previewGO, GameObject housePreviewGO, DualMesh.BuildMode currentBuildMode)
        {
            duneModel = model;
            dualMeshConstructor = constructor;
            buildPreviewGO = previewGO;
            housePrefab = housePreviewGO;
            this.currentBuildMode = currentBuildMode;
        }
        #endregion

        #region Auxiliar function for building


        public void HandleBuildPreview()
        {
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

                buildPreviewGO.transform.position = new Vector3(avgX, maxY + 0.5f, avgZ);

                previewX = x;
                previewZ = z;
            }
        }

        public void ConfirmBuild()
        {
            if (currentBuildMode == DualMesh.BuildMode.PlaceHouse)
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

                GameObject houseInstance = GameObject.Instantiate(housePrefab, centerPos, Quaternion.identity);
                houseInstance.SetActive(true);

                Renderer rend = houseInstance.GetComponentInChildren<Renderer>();
                Bounds bounds = rend.bounds;
                float targetHeight = bounds.max.y - 0.05f;

                // Abarcar los vértices dentro del bounding box del modelo
                int xMin = Mathf.Clamp(Mathf.FloorToInt(bounds.min.x / cellSize), 0, duneModel.xResolution);
                int xMax = Mathf.Clamp(Mathf.CeilToInt(bounds.max.x / cellSize), 0, duneModel.xResolution);
                int zMin = Mathf.Clamp(Mathf.FloorToInt(bounds.min.z / cellSize), 0, duneModel.zResolution);
                int zMax = Mathf.Clamp(Mathf.CeilToInt(bounds.max.z / cellSize), 0, duneModel.zResolution);
                Debug.Log($"xmin={xMin}, xmax={xMax}, zmin={zMin}, zmax={zMax}");

                for (int x = 2 + xMin; x <= xMax - 2; x++)
                {
                    for (int z = 2 + zMin; z <= zMax - 2; z++)
                    {
                        // Posición mundial del vértice
                        float worldX = x * cellSize;
                        float worldZ = z * cellSize;

                        // Verifica si está dentro del área horizontal del modelo
                        if (bounds.Contains(new Vector3(worldX, bounds.center.y, worldZ)))
                        {
                            duneModel.terrainElev[x, z] = targetHeight;
                            //duneModel.sandElev[x, z] = targetHeight;

                            duneModel.ActivateCell(x, z);
                            duneModel.UpdateShadow(x, z, duneModel.dx, duneModel.dz);
                        }
                    }
                }

                housePrefab.SetActive(false);
                buildPreviewGO.SetActive(false);
                return;
            }

            // Modo RAISE / DIG (sin cambios relevantes aquí)
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
                    if (deltaHeight < 0)
                    {
                        duneModel.sandElev[nx, nz] = maxHeight + deltaHeight;
                    }

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
                        case DualMesh.BuildMode.PlaceHouse:
                            renderer.material.color = Color.blue;
                            break;
                    }
                }

                #endregion

            }
        }