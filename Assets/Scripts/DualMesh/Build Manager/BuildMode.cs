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
        public GameObject shovelPreviewGO, housePreviewGO, wallPreviewGO, sweeperPreviewGO, activePreview, housePrefab, wallPrefab, circlePreviewGO;
        public ModelDM duneModel;
        public DualMeshConstructor dualMeshConstructor;
        public int buildRadius = 4;
        public int buildSize = 2; // puede ser 2 o 3d
        public float digDepth = 1f;
        private int previewX, previewZ;
        private UnityEngine.Vector3 point;
        public DualMesh.BuildMode currentBuildMode;
        public float[,] terrainElev;
        //public bool[,] isConstruible;
        public int[,] constructionGrid;
        private UnityEngine.Quaternion prefabRotation = UnityEngine.Quaternion.identity;

        private Dictionary<int, ConstrucionData> constructions;
        private int currentConstructionID = 1;

        private Coroutine shakeCoroutine;
        private bool planicie;

        bool canBuild;

        #endregion

        #region Init Build System
        public BuildSystem(
            ModelDM model, DualMeshConstructor constructor,
            GameObject housePrefab, GameObject wallPrefab,
            ref GameObject shovelPreviewGO, ref GameObject housePreviewGO, ref GameObject wallPreviewGO, ref GameObject sweeperPreviewGO, ref GameObject circlePreviewGO,
            DualMesh.BuildMode currentBuildMode, float[,] terrainElev, ref GameObject activePreview,
            ref int[,] constructionGrid, bool planicie)
        {
            duneModel = model;
            dualMeshConstructor = constructor;
            this.housePrefab = housePrefab;
            this.wallPrefab = wallPrefab;
            this.shovelPreviewGO = shovelPreviewGO;
            this.housePreviewGO = housePreviewGO;
            this.wallPreviewGO = wallPreviewGO;
            this.circlePreviewGO = circlePreviewGO;
            this.sweeperPreviewGO = sweeperPreviewGO;
            this.currentBuildMode = currentBuildMode;
            this.terrainElev = terrainElev;
            this.constructionGrid = constructionGrid;
            this.planicie = planicie;

            this.activePreview = activePreview;

            constructions = new Dictionary<int, ConstrucionData>();

            if (planicie)
            {
                previewX = Mathf.FloorToInt((duneModel.size / 2) * duneModel.xResolution / duneModel.size);
                previewZ = Mathf.FloorToInt((duneModel.size / 2) * duneModel.zResolution / duneModel.size);
                GameObjectConstruction(housePrefab, Quaternion.identity, "House");
            }

        }
        #endregion

    }
}