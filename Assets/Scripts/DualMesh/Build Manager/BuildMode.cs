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
        public GameObject shovelPreviewGO, housePreviewGO, wallPreviewGO, towerPreviewGO, sweeperPreviewGO, activePreview, housePrefab, wallPrefab, towerPrefab, circlePreviewGO;
        public ModelDM duneModel;
        public DualMeshConstructor dualMeshConstructor;
        public int buildRadius = 4;
        public int buildSize = 2; 
        public float digDepth = 1f, durationBuild = 5f;
        private int previewX, previewZ;
        private UnityEngine.Vector3 point;
        public DualMesh.BuildMode currentBuildMode;
        public DualMesh.ActionMode currentActionMode;
        public DualMesh.PlayingMode inMode;
        public NativeGrid terrain;
        ConstructionGrid constructionGrid;
        private UnityEngine.Quaternion prefabRotation = UnityEngine.Quaternion.identity;

        private Dictionary<int, ConstructionData> constructions;
        private int currentConstructionID, currentCompositeConstructionID;

        private Coroutine shakeCoroutine;

        bool canBuild;

        public Vector3? wallStartPoint = null;
        private Vector3? wallEndPoint = null;
        private float wallPrefabLength;
        private GameObject wallPreviewParent;

        private ResourceSystem.ResourceManager resourceManager;
        private ConstructionConfig constructionsConfigs;

        #endregion

        #region Init Build System
        public BuildSystem(
            ModelDM model,
            DualMeshConstructor constructor,
            Dictionary<int, ConstructionData> constructions,
            int currentConstructionID,
            int currentCompositeConstructionID,
            float pulledDownTime,
            GameObject housePrefab,
            GameObject wallPrefab,
            GameObject towerPrefab,
            GameObject shovelPreviewGO,
            GameObject housePreviewGO,
            GameObject wallPreviewGO,
            GameObject towerPreviewGO,
            GameObject sweeperPreviewGO,
            GameObject circlePreviewGO,
            DualMesh.PlayingMode inMode,
            DualMesh.BuildMode currentBuildMode,
            DualMesh.ActionMode currentActionMode,
            NativeGrid terrain,
            GameObject activePreview,
            ConstructionGrid constructionGrid,
            bool planicie
        )
        {
            resourceManager = ResourceSystem.ResourceManager.TryGetInstance();
            constructionsConfigs = ConstructionConfig.TryGetInstance();
            
            duneModel = model;
            this.inMode = inMode;
            dualMeshConstructor = constructor;
            this.constructions = constructions;
            this.durationBuild = pulledDownTime;
            this.currentConstructionID = currentConstructionID;
            this.currentCompositeConstructionID = currentCompositeConstructionID;
            this.housePrefab = housePrefab;
            this.wallPrefab = wallPrefab;
            this.towerPrefab = towerPrefab;
            this.shovelPreviewGO = shovelPreviewGO;
            this.housePreviewGO = housePreviewGO;
            this.wallPreviewGO = wallPreviewGO;
            this.towerPreviewGO = towerPreviewGO;
            this.circlePreviewGO = circlePreviewGO;
            this.sweeperPreviewGO = sweeperPreviewGO;
            this.currentBuildMode = currentBuildMode;
            this.terrain = terrain;
            this.constructionGrid = constructionGrid;
            this.activePreview = activePreview;

            wallPrefabLength = CalculateWallPrefabLength(wallPrefab);
            wallPreviewParent = new GameObject();
            wallPreviewParent.name = "Wall Previews";

            if (planicie)
            {
                previewX = duneModel.xResolution / 2;
                previewZ = duneModel.zResolution / 2;
                GameObjectConstruction(ConstructionType.House, previewX, previewZ, Quaternion.identity, ConstructionType.House, verify: false);
                resourceManager.AddResource("Workers", -1);
            }
        }
        #endregion

        private float CalculateWallPrefabLength(GameObject prefab)
        {
            Renderer rend = prefab.GetComponentInChildren<Renderer>();
            if (rend != null)
            {
                return rend.bounds.size.x;
            }
            else
            {
                Debug.LogWarning("Wall prefab does not have a renderer!");
                return 1f; // valor por defecto
            }
        }
    }
}