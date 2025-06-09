using System;
using System.Linq;
using ue = UnityEngine;

namespace DunefieldModel_DualMeshJobs
{
    public partial class ModelDMJ
    {
        #region Avalanche
        public void AvalancheInit()
        {
            for (int x = 0; x < xResolution; x++)
            {
                for (int z = 0; z < zResolution; z++)
                {
                    Avalanche(x, z);
                }
            }
        }

        public virtual void Avalanche(int x, int z, int iter = 3)
        {
            /// <summary>
            /// Simula la avalancha alrededor de la posición (x, z).
            /// </summary>
            /// <param name="x">Coordenada X de la posición.</param>
            /// <param name="z">Coordenada Z de la posición.</param>
            
            int index = x + (xResolution * z);

            while (iter-- > 0)
            {
                if (terrainElev[index] >= sandElev[index])
                {
                    return;
                }
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

                if (xAvalanche < 0 || zAvalanche < 0)
                {
                    // No hay pendiente de avalancha, no se erosiona
                    break;
                }
                else
                {
                    int indexAvalanche = xAvalanche + (xResolution * zAvalanche);
                    float diff = Math.Abs(Math.Max(sandElev[indexAvalanche], terrainElev[indexAvalanche]) - sandElev[index]) / 2f;
                    sandElev[index] -= diff;
                    sandElev[indexAvalanche] += ((sandElev[indexAvalanche] > terrainElev[indexAvalanche]) ? 0 : terrainElev[indexAvalanche] - sandElev[indexAvalanche]) + diff;
                    x = xAvalanche;
                    z = zAvalanche;
                }
            }
        }
        #endregion
    }
}