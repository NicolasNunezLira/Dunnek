using System;
using ue = UnityEngine;

namespace DunefieldModel_DualMesh
{
    public partial class ModelDM
    {
        #region Avalanche
        public void AvalancheInit()
        {
            for (int x = 0; x < sandElev.GetLength(0); x++)
            {
                for (int z = 0; z < sandElev.GetLength(1); z++)
                {
                    Avalanche(x, z, erosionHeight);
                }
            }
        }

        public virtual void Avalanche(int x, int z, float avalancheHeight, int iter = 3)
        {
            /// <summary>
            /// Simula la avalancha alrededor de la posición (x, z).
            /// </summary>
            /// <param name="x">Coordenada X de la posición.</param>
            /// <param name="z">Coordenada Z de la posición.</param>

            while (iter-- > 0)
            {
                int xAvalanche = -1;
                int zAvalanche = -1;
                while (FindSlope.AvalancheSlope(x, z, out int xLow, out int zLow, avalancheSlope) >= 2)
                {
                    if (openEnded &&
                        ((xAvalanche == xDOF && x == 0) || (xAvalanche == 0 && x == xDOF) ||
                        (zAvalanche == zDOF && z == 0) || (zAvalanche == 0 && z == zDOF)))
                        break;
                    xAvalanche = xLow;
                    zAvalanche = zLow;
                }

                if (xAvalanche < 0 && zAvalanche < 0)
                {
                    // No hay pendiente de avalancha, no se erosiona
                    break;
                }
                else
                {
                    sandElev[x, z] -= avalancheHeight;
                    sandElev[xAvalanche, zAvalanche] += (sandElev[xAvalanche, zAvalanche] > terrainElev[xAvalanche, zAvalanche]) ? 0 : terrainElev[xAvalanche, zAvalanche] - sandElev[xAvalanche, zAvalanche] + avalancheHeight;
                    x = xAvalanche;
                    z = zAvalanche;
                }
            }
        }
        #endregion
    }
}