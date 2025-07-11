using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using Data;
using Unity.Collections;
using Unity.Mathematics;
using Unity.VisualScripting;
using ue=UnityEngine;

namespace DunefieldModel_DualMesh
{
    [System.Serializable]
    public partial class ModelDM
    {
        #region Variables
        //public float[,] sandElev, terrainElev, realTerrain;
        public NativeGrid sand, terrainShadow, shadow, terrain;
        //public bool[,] isConstruible;
        public int[,] constructionGrid;

        public int xResolution = 0, zResolution = 0;
        public int HopLength = 1;
        public float pSand = 0.6f;
        public float pNoSand = 0.4f;
        public IFindSlope FindSlope;
        protected int xDOF, zDOF; // xDOF = xResolution - 1, zDOF = zResolution - 1
        protected System.Random rnd = new System.Random(42);
        protected bool openEnded = false;
        public float shadowSlope;  //  3 * tan(15 degrees) \approx 0.803847577f

        public float depositeHeight = .1f;
        public float erosionHeight = .1f;

        public float slopeThreshold = .2f; // slope threshold for deposition

        public int grainsPerStep, grainsOutside;

        public float slope, avalancheSlope;


        public int dx, dz;

        private float erosionH, depositeH, aux;

        public Dictionary<int, ConstructionData> constructions;

        public int currentConstructionID;

        public bool verbose, isPaused;

        public float maxCellsPerFrame, conicShapeFactor, avalancheTrasnferRate, minAvalancheAmount, size;

        public FrameVisualChanges sandChanges, terrainShadowChanges;

        #endregion

        #region Init model
        public ModelDM(
            IFindSlope SlopeFinder,
            NativeGrid sand, NativeGrid terrainShadow, ref NativeGrid terrain,
            int[,] constructionGrid,
            float size,
            int xResolution, int zResolution,
            float slope,
            int dx, int dz,
            ref Dictionary<int, ConstructionData> constructions,
            ref int currentConstructionID,
            float depositeHeight, float erosionHeight,
            int hopLength, float shadowSlope, float avalancheSlope,
            float maxCellsPerFrame,
            float conicShapeFactor,
            float avalancheTrasnferRate,
            float minAvalancheAmount,
            FrameVisualChanges sandChanges,
            FrameVisualChanges terrainShadowChanges)
        {
            this.terrain = terrain;
            this.terrainShadow = terrainShadow;
            this.sand = sand;
            FindSlope = SlopeFinder;
            this.constructionGrid = constructionGrid;
            this.constructions = constructions;
            this.currentConstructionID = currentConstructionID;
            this.size = size;
            this.maxCellsPerFrame = maxCellsPerFrame;
            this.avalancheTrasnferRate = avalancheTrasnferRate;
            this.minAvalancheAmount = minAvalancheAmount;
            this.conicShapeFactor = conicShapeFactor;
            this.depositeHeight = depositeHeight;
            this.erosionHeight = erosionHeight;
            this.HopLength = hopLength;
            this.shadowSlope = shadowSlope;
            this.slope = slope;
            this.avalancheSlope = avalancheSlope;
            this.dx = dx;
            this.dz = dz;
            this.xResolution = xResolution;
            this.zResolution = zResolution;
            xDOF = this.xResolution + 1;
            zDOF = this.zResolution + 1;
            this.sandChanges = sandChanges;
            this.terrainShadowChanges = terrainShadowChanges;
            shadow = new NativeGrid(
                sand.Width, sand.Height, sand.VisualWidth, sand.VisualHeight, Allocator.Persistent);
            ShadowInit();
            FindSlope.Init(
                ref sand, ref terrainShadow, this.sand.Width, this.sand.Height, this.slope
            );
        }
        #endregion

        #region Auxiliar methods
        public virtual bool UsesHopLength()
        {
            return true;  // does this model use the user-provided value of hop length?
        }

        public virtual bool UsesSandProbabilities()
        {
            return true;  // does this model use the user-provided values of sand depositing probabilities?
        }

        public void SetOpenEnded(bool NewState)
        {  // 'true' means dunefield is open-ended (no wrapping)
            openEnded = NewState;
            FindSlope.SetOpenEnded(openEnded);
        }
        #endregion

    }
}