using System;
using ue = UnityEngine;

namespace DunefieldModel_DualMeshJobs
{
    public partial class ModelDMJ
    {
        public virtual void DepositGrain(int x, int z, int dx, int dz, float depositeHeight)
        {
            /// <summary>
            /// Deposita un grano de arena en la posición (x, z) considerando viento con dirección (dx, dz).
            /// </summary>
            /// <param name="x">Componente x de la posición donde se intentará depositar el grano.</param>
            /// <param name="z">Componente z de la posición donde se intentará depositar el grano.</param>
            /// <param name="dx">Componente x de la dirección del viento.</param>
            /// <param name="dz">Componente z de la dirección del viento.</param>
            /// <param name="depositeHeight">Altura de deposición del grano.</param>

            // Buscar el punto más bajo en la dirección del viento
            while (FindSlope.Downslope(x, z, dx, dz, out int xLow, out int zLow) >= 1)
            {
                if (openEnded &&
                    ((xLow == xDOF && x == 0) || (xLow == 0 && x == xDOF) ||
                    (zLow == zDOF && z == 0) || (zLow == 0 && z == zDOF)))
                    break;

                x = xLow;
                z = zLow;
            }

            int index = x + (xResolution * z);


            if (terrainElev[index] >= sandElev[index])
            {
                // Si el terreno es más alto que la arena más la altura de deposición, depositar encima del terreno
                sandElev[index] = terrainElev[index] + depositeHeight;
            }
            else
            {
                sandElev[index] += depositeHeight;
            }

            
            Avalanche(x, z, 5);
            UpdateShadow(x, z, dx, dz);
            
            
        }

    }
}
