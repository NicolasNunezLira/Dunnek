using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using DunefieldModel;
using DunefieldModel_DualMesh;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using ue=UnityEngine;

namespace DunefieldModel_DualMeshJobs
{

    public struct SandChanges
    {
        public int index;
        public float delta;
    }

    public struct ShadowChanges
    {
        public int index;
        public float value;
    }

    [BurstCompile]
    public struct DuneFieldSimulation : IJobParallelFor
    {
        // Cell for simulation process
        public NativeArray<int> randomsX;
        public NativeArray<int> randomsZ;

        // Struct for parallelize
        [WriteOnly] public NativeList<SandChanges>.ParallelWriter sandChanges;


        // Surfaces information
        [ReadOnly] public NativeArray<float> sand;
        [ReadOnly] public NativeArray<float> terrain;
        [ReadOnly] public NativeArray<float> shadow;

        // Resolution of the meshes
        public int xResolution;
        public int zResolution;
        public float size;

        // Max height variation in the deposit and erosion processes
        public float depositeHeight;
        public float erosionHeight;

        // Min slopes for the respective behaviour
        public float slope;
        public float shadowSlope;
        public float avalancheSlope;

        // Threshhold for slope
        public float slopeThreshold;

        // Wind Components
        public int dx;
        public int dz;
        public int HopLength;

        // Probabilities for deposition
        public float pSand;
        public float pNoSand;

        // Iterations for avalanche process
        public int iter;

        // Random Number Generator
        public NativeArray<Unity.Mathematics.Random> rng;        

        // Toroidal behaviour 
        public bool openEnded;

        // Options for debug
        public bool verbose;

        public void Execute(int i)
        {
            int x = randomsX[i];
            int z = randomsZ[i];
            int index = x + (xResolution * z);
            ue.Debug.Log("(" + x + ", " + z + ") = " + index);

            if (Math.Max(sand[index], terrain[index]) <= 0 || // Cell without height
                shadow[index] > 0 ||                          // Cell in shadow
                sand[index] < terrain[index]                  // Cell without sand
            )
            {
                return;
            }

            // Erode process
            float erodeH;
            //NativeArray<float> newShadow;
            erodeH = ModelDMJ.ErodeGrain(
                x, z,
                dx, dz,
                erosionHeight,
                terrain, sand, shadow,
                xResolution, zResolution,
                slope, shadowSlope, openEnded,
                sandChanges
            );

            //foreach (var change in erodeOut) { sandChanges.AddNoResize(new SandChanges { index = change.index, delta = change.delta }); };

            float depositeH = Math.Min(erodeH, depositeHeight);

            // Deposition process
            //List<SandChanges> depositeOut = new List<SandChanges>();
            ModelDMJ.algorithmDeposit(
                x, z,
                dx, dz, HopLength,
                sand, terrain, shadow,
                xResolution, zResolution,
                depositeH,
                slope, shadowSlope, avalancheSlope,
                slopeThreshold,
                pSand, pNoSand,
                rng[i],
                iter,
                openEnded,
                verbose,
                sandChanges
            );
        }
    }

}