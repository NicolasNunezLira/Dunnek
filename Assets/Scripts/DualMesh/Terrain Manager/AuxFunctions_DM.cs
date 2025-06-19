using System;
using ue = UnityEngine;

namespace DunefieldModel_DualMesh
{
    public partial class ModelDM
    {
        #region Auxiliar functions


        public virtual int SaltationLength(int x, int z)
        {
            return HopLength;
        }

        public virtual int SpecialField(int x, int z)
        {
            return 0;
        }

        private bool IsInside(int x, int z)
        {
            return x >= 0 && x < xResolution && z >= 0 && z < zResolution;
        }

        private bool IsOutside(int x, int z)
        {
            return x < 0 || x >= xResolution || z < 0 || z >= zResolution;
        }

        private (int, int) WrapCoords(int x, int z)
        {
            x = (x + xResolution) % xResolution;
            z = (z + zResolution) % zResolution;
            return (x, z);
        }
        
        #endregion


    }
}