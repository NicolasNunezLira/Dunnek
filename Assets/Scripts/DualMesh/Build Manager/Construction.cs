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
        
        public void GameObjectConstruction(GameObject prefab, UnityEngine.Quaternion rotation, string name)
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

            GameObject parentGO = GameObject.Find("Construcciones");
            if (parentGO == null)
            {
                parentGO = new GameObject("Construcciones");
                SetLayerRecursively(parentGO, LayerMask.NameToLayer("Constructions"));
            }

            // Instanciar el prefab con el objeto padre
            GameObject prefabInstance = GameObject.Instantiate(prefab, centerPos, rotation, parentGO.transform);
            SetLayerRecursively(prefabInstance, LayerMask.NameToLayer("Constructions"));
            prefabInstance.name = name + System.DateTime.Now.ToString("HHmmss");
            
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
                        duneModel.sandElev[x, z] = floorHeight;
                        isConstruible[x, z] = false;
                        support.Add(new float2(x, z));
                        duneModel.ActivateCell(x, z);
                        duneModel.UpdateShadow(x, z, duneModel.dx, duneModel.dz);
                    }
                }
            }
            AddConstructionToList(centerPos, prefabRotation, currentBuildMode, support);
            return;
        }

        // Helper method to set layer recursively
        private void SetLayerRecursively(GameObject obj, int newLayer)
        {
            if (obj == null) return;
            obj.layer = newLayer;
            foreach (Transform child in obj.transform)
            {
                if (child == null) continue;
                SetLayerRecursively(child.gameObject, newLayer);
            }
        }

        #region Save constructions
        public class ConstrucionData
        {
            public UnityEngine.Vector3 position;
            public UnityEngine.Quaternion rotation;
            public DualMesh.BuildMode type;
            public List<float2> support;
        }

        public void AddConstructionToList(
            UnityEngine.Vector3 position,
            UnityEngine.Quaternion rotation,
            DualMesh.BuildMode currentType,
            List<float2> support
        )
        {
            constructionList.Add(
                new ConstrucionData
                {
                    position = position,
                    rotation = rotation,
                    type = currentType,
                    support = support
                }
            );
        }

        public GameObject GetPrefabForType(DualMesh.BuildMode mode)
        {
            switch (mode)
            {
                case DualMesh.BuildMode.Raise: return wallPrefab;
                case DualMesh.BuildMode.Dig: return shovelPreviewGO;
                case DualMesh.BuildMode.PlaceHouse: return housePrefab;
                default: return null;
            }
        }
        #endregion
    }
}