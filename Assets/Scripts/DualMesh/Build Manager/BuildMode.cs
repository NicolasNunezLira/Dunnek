using UnityEngine;
using DunefieldModel_DualMesh;
using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine.UIElements;
//using System.Numerics;

namespace Building
{
    [System.Serializable]
    public partial class BuildSystem
    {
        #region Variables
        public GameObject shovelPreviewGO, housePreviewGO, wallPreviewGO, activePreview, housePrefab, wallPrefab;
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
        private bool planicie;

        bool canBuild;

        #endregion

        #region Init Build System
        public BuildSystem(
            ModelDM model, DualMeshConstructor constructor,
            GameObject housePrefab, GameObject wallPrefab,
            ref GameObject shovelPreviewGO, ref GameObject housePreviewGO, ref GameObject wallPreviewGO,
            DualMesh.BuildMode currentBuildMode, float[,] terrainElev, ref GameObject activePreview,
            ref bool[,] isConstruible, bool planicie)
        {
            duneModel = model;
            dualMeshConstructor = constructor;
            this.housePrefab = housePrefab;
            this.wallPrefab = wallPrefab;
            this.shovelPreviewGO = shovelPreviewGO;
            this.housePreviewGO = housePreviewGO;
            this.wallPreviewGO = wallPreviewGO;
            this.currentBuildMode = currentBuildMode;
            this.terrainElev = terrainElev;
            this.isConstruible = isConstruible;
            this.planicie = planicie;

            for (int x = 0; x < isConstruible.GetLength(0); x++)
            {
                for (int z = 0; z < isConstruible.GetLength(1); z++)
                {
                    isConstruible[x, z] = true;
                }
            }

            this.activePreview = activePreview;

            constructionList = new List<ConstrucionData>();

            if (planicie)
            {
                previewX = Mathf.FloorToInt((duneModel.size / 2) * duneModel.xResolution / duneModel.size);
                previewZ = Mathf.FloorToInt((duneModel.size / 2) * duneModel.zResolution / duneModel.size);
                GameObjectConstruction(housePrefab, Quaternion.identity);
            }

        }
        #endregion

    }
}