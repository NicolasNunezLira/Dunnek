using UnityEngine;
using DunefieldModel_DualMesh;
using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine.UIElements;
//using System.Numerics;

namespace Building
{
    public class BuildSystem
    {
        #region Variables
        public GameObject boxPreviewGO, housePreviewGO, wallPreviewGO, activePreview, housePrefab, wallPrefab;
        public ModelDM duneModel;
        public DualMeshConstructor dualMeshConstructor;
        public int buildRadius = 4;
        public int buildSize = 2; // puede ser 2 o 3d
        public float digDepth = 1f;
        private int previewX, previewZ;
        private UnityEngine.Vector3 point;
        public DualMesh.BuildMode currentBuildMode;
        public float[,] terrainElev;
        public bool[,] isConstruible;
        private UnityEngine.Quaternion prefabRotation = UnityEngine.Quaternion.identity;

        private List<ConstrucionData> constructionList;

        private Coroutine shakeCoroutine;

        bool canBuild;

        #endregion

        #region Init Build System
        public BuildSystem(
            ModelDM model, DualMeshConstructor constructor,
            GameObject housePrefab, GameObject wallPrefab,
            ref GameObject boxPreviewGO, ref GameObject housePreviewGO, ref GameObject wallPreviewGO,
            DualMesh.BuildMode currentBuildMode, float[,] terrainElev, ref GameObject activePreview,
            ref bool[,] isConstruible)
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
            this.isConstruible = isConstruible;

            for (int x = 0; x < isConstruible.GetLength(0); x++)
            {
                for (int z = 0; z < isConstruible.GetLength(1); z++)
                {
                    isConstruible[x, z] = true;
                }
            }

            this.activePreview = activePreview;

            constructionList = new List<ConstrucionData>();

        }
        #endregion

        #region Building functions


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

                Renderer rend = activePreview.GetComponentInChildren<Renderer>();
                Bounds bounds = rend.bounds;

                float cellSize = duneModel.size / duneModel.xResolution;
                // Convertimos los límites del modelo 3D a coordenadas en la grilla del terreno
                int xMin = Mathf.Clamp(Mathf.FloorToInt(bounds.min.x / cellSize), 0, duneModel.xResolution - 1);
                int xMax = Mathf.Clamp(Mathf.CeilToInt(bounds.max.x / cellSize), 0, duneModel.xResolution - 1);
                int zMin = Mathf.Clamp(Mathf.FloorToInt(bounds.min.z / cellSize), 0, duneModel.zResolution - 1);
                int zMax = Mathf.Clamp(Mathf.CeilToInt(bounds.max.z / cellSize), 0, duneModel.zResolution - 1);

                canBuild = true;
                float maxY = float.MinValue;

                for (int xi = xMin; xi <= xMax; xi++)
                {
                    for (int zj = zMin; zj <= zMax; zj++)
                    {
                        if (!isConstruible[xi, zj]) // ← asegúrate que `true` signifique libre
                            canBuild = false;

                        float y = Mathf.Max(duneModel.sandElev[xi, zj], duneModel.terrainElev[xi, zj]);
                        if (y > maxY) maxY = y;
                    }
                }

                float avgX = (x + (buildSize - 1) / 2f) * duneModel.size / duneModel.xResolution;
                float avgZ = (z + (buildSize - 1) / 2f) * duneModel.size / duneModel.zResolution;

                activePreview.transform.position = new UnityEngine.Vector3(avgX, maxY + 0.5f, avgZ);

                // Aplicar color según disponibilidad
                Color color = canBuild ? new Color(0f, 1f, 0f, 0.3f) : new Color(1f, 0f, 0f, 0.3f);
                foreach (var rend_ in activePreview.GetComponentsInChildren<Renderer>())
                {
                    if (rend_.material.HasProperty("_Color"))
                        rend_.material.color = color;
                }

                previewX = x;
                previewZ = z;
            }
        }

        public bool ConfirmBuild()
        {
            if (!canBuild) return false;

            activePreview.SetActive(false);


            switch (currentBuildMode)
            {
                case DualMesh.BuildMode.PlaceHouse:
                    GameObjectConstruction(housePrefab, prefabRotation);
                    return true;
                case DualMesh.BuildMode.Raise:
                    GameObjectConstruction(wallPrefab, prefabRotation);
                    return true;
                case DualMesh.BuildMode.Dig:
                    DigAction(previewX, previewZ, buildRadius, digDepth);
                    return true;
            }
            return false;
        }

        public void GameObjectConstruction(GameObject prefab, UnityEngine.Quaternion rotation)
        {
            float cellSize = duneModel.size / duneModel.xResolution;

            float y = Mathf.Max(
                duneModel.sandElev[previewX, previewZ],
                duneModel.terrainElev[previewX, previewZ]
            );

            UnityEngine.Vector3 centerPos = new UnityEngine.Vector3(
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

            List<float2> support = new List<float2>();
            for (int x = xMin; x <= xMax; x++)
            {
                for (int z = zMin; z <= zMax; z++)
                {
                    // Posición mundial del vértice
                    float worldX = x * cellSize;
                    float worldZ = z * cellSize;

                    // Verifica si está dentro del área horizontal del modelo
                    if (bounds.Contains(new UnityEngine.Vector3(worldX, bounds.center.y, worldZ)))
                    {
                        duneModel.terrainElev[x, z] = targetHeight;
                        terrainElev[x, z] = floorHeight;
                        isConstruible[x, z] = false;
                        duneModel.ActivateCell(x, z);
                        duneModel.UpdateShadow(x, z, duneModel.dx, duneModel.dz);
                    }
                }
            }
            AddConstructionToList(centerPos, prefabRotation, currentBuildMode);
            return;
        }

        public void DigAction(int centerX, int centerZ, int radius, float digDepth)
        {
            if (terrainElev[centerX, centerZ] >= duneModel.sandElev[centerX, centerZ]) return;

            float[,] sandElev = duneModel.sandElev;

            int width = sandElev.GetLength(0);
            int height = sandElev.GetLength(1);

            float maxHeight = float.MinValue;

            // 1. Encontrar altura máxima
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dz = -radius; dz <= radius; dz++)
                {
                    int nx = centerX + dx;
                    int nz = centerZ + dz;
                    if (nx < 0 || nx >= width || nz < 0 || nz >= height) continue;

                    float h = Mathf.Max(sandElev[nx, nz], terrainElev[nx, nz]);
                    if (h > maxHeight) maxHeight = h;
                }
            }

            // 2. Cavar y acumular arena removida
            float totalRemoved = 0f;

            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dz = -radius; dz <= radius; dz++)
                {
                    int nx = centerX + dx;
                    int nz = centerZ + dz;
                    if (nx < 0 || nx >= width || nz < 0 || nz >= height) continue;

                    float dist = Mathf.Sqrt(dx * dx + dz * dz);

                    if (dist <= radius - 0.5f)
                    {
                        if (terrainElev[nx, nz] >= sandElev[nx, nz] || !isConstruible[nx, nz]) continue;
                        float original = sandElev[nx, nz];
                        float newHeight = original - digDepth;
                        newHeight = newHeight > terrainElev[nx, nz] ? newHeight : terrainElev[nx, nz];
                        float removed = original - newHeight;
                        totalRemoved += removed;

                        //terrainElev[nx, nz] = newHeight;
                        sandElev[nx, nz] = newHeight;
                    }
                }
            }

            // 3. Recolectar celdas del anillo expandido con peso
            List<(int x, int z, float weight)> ringCells = new();
            float weightSum = 0f;

            int extraSpreadRadius = CalculateExtraSpreadRadius(totalRemoved, radius);

            for (int dx = -(radius + extraSpreadRadius); dx <= (radius + extraSpreadRadius); dx++)
            {
                for (int dz = -(radius + extraSpreadRadius); dz <= (radius + extraSpreadRadius); dz++)
                {
                    int nx = centerX + dx;
                    int nz = centerZ + dz;
                    if (nx < 0 || nx >= width || nz < 0 || nz >= height) continue;

                    float dist = Mathf.Sqrt(dx * dx + dz * dz);
                    if (dist > radius && dist <= radius + extraSpreadRadius)
                    {
                        if (!isConstruible[nx, nz]) continue;
                        // Peso inverso a la distancia (más cerca → más arena)
                        float weight = 1f / (dist + 0.01f);
                        ringCells.Add((nx, nz, weight));
                        weightSum += weight;
                    }
                }
            }

            // 4. Distribuir la arena suavemente
            foreach (var (x, z, weight) in ringCells)
            {
                float amount = totalRemoved * (weight / weightSum);
                sandElev[x, z] += amount;

                duneModel.ActivateCell(x, z);
                duneModel.UpdateShadow(x, z, duneModel.dx, duneModel.dz);
            }
        }


        int CalculateExtraSpreadRadius(float totalRemoved, float radius, float maxRimHeight = 0.5f)
        {
            float baseArea = Mathf.PI * radius * radius;
            float requiredArea = totalRemoved / maxRimHeight;

            float requiredTotalRadius = Mathf.Sqrt((requiredArea + baseArea) / Mathf.PI);
            float extraRadius = requiredTotalRadius - radius;

            return Mathf.CeilToInt(extraRadius);
        }


        #endregion

        #region Preview functions

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

            prefabRotation *= UnityEngine.Quaternion.Euler(0, 90f, 0);
            activePreview.transform.rotation = prefabRotation;
        }

        #endregion


        #region Save constructions
        [Serializable]
        public class ConstrucionData
        {
            public UnityEngine.Vector3 position;
            public UnityEngine.Quaternion rotation;
            public DualMesh.BuildMode type;
        }

        public void AddConstructionToList(
            UnityEngine.Vector3 position,
            UnityEngine.Quaternion rotation,
            DualMesh.BuildMode currentType
        )
        {
            constructionList.Add(
                new ConstrucionData
                {
                    position = position,
                    rotation = rotation,
                    type = currentType
                }
            );
        }

        public GameObject GetPrefabForType(DualMesh.BuildMode mode)
        {
            switch (mode)
            {
                case DualMesh.BuildMode.Raise: return wallPrefab;
                case DualMesh.BuildMode.Dig: return boxPreviewGO;
                case DualMesh.BuildMode.PlaceHouse: return housePrefab;
                default: return null;
            }
        }
        #endregion

        #region Shake Routine

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
        #endregion

    }
}