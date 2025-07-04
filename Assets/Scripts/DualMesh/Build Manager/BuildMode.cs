using UnityEngine;
using DunefieldModel_DualMesh;
using System.Collections.Generic;
using Data;

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
        public float digDepth = 1f, durationBuild = 5f;
        private int previewX, previewZ;
        private UnityEngine.Vector3 point;
        public DualMesh.BuildMode currentBuildMode;
        public float[,] terrainElev;
        //public bool[,] isConstruible;
        public int[,] constructionGrid;
        private UnityEngine.Quaternion prefabRotation = UnityEngine.Quaternion.identity;

        private Dictionary<int, ConstructionData> constructions;
        private int currentConstructionID;

        private Coroutine shakeCoroutine;

        bool canBuild;

        #endregion

        #region Init Build System
        public BuildSystem(
            ModelDM model,
            DualMeshConstructor constructor,
            Dictionary<int, ConstructionData> constructions,
            int currentConstructionID,
            float pulledDownTime,
            GameObject housePrefab,
            GameObject wallPrefab,
            GameObject shovelPreviewGO,
            GameObject housePreviewGO,
            GameObject wallPreviewGO,
            GameObject sweeperPreviewGO,
            GameObject circlePreviewGO,
            DualMesh.BuildMode currentBuildMode,
            float[,] terrainElev,
            GameObject activePreview,
            int[,] constructionGrid,
            bool planicie
        )
        {
            duneModel = model;
            dualMeshConstructor = constructor;
            this.constructions = constructions;
            this.durationBuild = pulledDownTime;
            this.currentConstructionID = currentConstructionID;
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
            this.activePreview = activePreview;

            if (planicie)
            {
                previewX = duneModel.xResolution / 2;
                previewZ = duneModel.zResolution / 2;
                GameObjectConstruction(housePrefab, Quaternion.identity, "House");
            }
        }
        #endregion

    }
}