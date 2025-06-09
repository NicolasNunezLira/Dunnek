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

namespace DunefieldModel_DualMeshJobs
{
    public partial class ModelDMJ
    {
        #region Variables
        public NativeArray<float> sandElev, terrainElev, shadow;

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
        

        #endregion

        #region Init model
        public ModelDMJ(IFindSlope SlopeFinder, NativeArray<float> sandElev, NativeArray<float> terrainElev, int xResolution, int zResolution, float slope, int dx, int dz,
            float depositeHeight, float erosionHeight, int hopLength, float shadowSlope, float avalancheSlope)
        {
            FindSlope = SlopeFinder;
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
            xDOF = this.xResolution - 1;
            zDOF = this.zResolution - 1;
            shadow = new NativeArray<float>(zResolution * xResolution, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            for (int i = 0; i < shadow.Length; i++)
            {
                shadow[i] = 0f;
            }
            ShadowInit();
            FindSlope.Init(ref sandElev, ref terrainElev, this.xResolution, this.zResolution, this.slope);
            FindSlope.SetOpenEnded(openEnded);
        }
        #endregion

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

    }
}