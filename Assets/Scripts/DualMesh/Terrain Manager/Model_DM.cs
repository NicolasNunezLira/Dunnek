using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
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
        public float[,] sandElev, terrainElev;
        public float[,] Shadow;
        public bool[,] isConstruible;

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

        public int grainsPerStep;

        public float slope, avalancheSlope;


        public int dx, dz;

        private float erosionH, depositeH, aux;

        public bool verbose;

        public float maxCellsPerFrame, conicShapeFactor, avalancheTrasnferRate, minAvalancheAmount, size;

        #endregion

        #region Init model
        public ModelDM(IFindSlope SlopeFinder, float[,] sandElev, float[,] terrainElev, bool[,] isConstruible, float size, int xResolution, int zResolution, float slope, int dx, int dz,
            float depositeHeight, float erosionHeight, int hopLength, float shadowSlope, float avalancheSlope, float maxCellsPerFrame,
            float conicShapeFactor, float avalancheTrasnferRate, float minAvalancheAmount, bool verbose = false)
        {
            FindSlope = SlopeFinder;
            this.isConstruible = isConstruible;
            this.size = size;
            this.maxCellsPerFrame = maxCellsPerFrame;
            this.avalancheTrasnferRate = avalancheTrasnferRate;
            this.minAvalancheAmount = minAvalancheAmount;
            this.conicShapeFactor = conicShapeFactor;
            this.sandElev = sandElev;
            this.terrainElev = terrainElev;
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
            this.verbose = verbose;
            xDOF = this.xResolution - 1;
            zDOF = this.zResolution - 1;
            Shadow = new float[xResolution, zResolution];
            Array.Clear(Shadow, 0, zResolution * xResolution);
            ShadowInit();
            FindSlope.Init(ref sandElev, ref terrainElev, this.xResolution, this.zResolution, this.slope);
            //FindSlope.SetOpenEnded(openEnded);
        }
        #endregion

        #region Auxiliar Variables
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