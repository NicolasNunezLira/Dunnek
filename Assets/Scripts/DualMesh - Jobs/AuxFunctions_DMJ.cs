using System;
using ue = UnityEngine;

namespace DunefieldModel_DualMeshJobs
{
    public partial class ModelDMJ
    {
        #region Auxiliar functions

        /*
        public virtual int SaltationLength(int x, int z)
        {
            return HopLength;
        }

        public virtual int SpecialField(int x, int z)
        {
            return 0;
        }
        */

        public static bool IsInside(int x, int z, int xResolution, int zResolution)
        {
            return x >= 0 && x < xResolution && z >= 0 && z < zResolution;
        }

        public static bool IsOutside(int x, int z, int xResolution, int zResolution)
        {
            return x < 0 || x >= xResolution || z < 0 || z >= zResolution;
        }

        public static (int, int) WrapCoords(int x, int z, int xResolution, int zResolution)
        {
            x = (x + xResolution) % xResolution;
            z = (z + zResolution) % zResolution;
            return (x, z);
        }
        
        #endregion


    }
}