using System;
using ue = UnityEngine;

namespace DunefieldModel_DualMesh
{
    public partial class ModelDM
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


            if (terrainElev[x, z] >= sandElev[x, z])
            {
                // Si el terreno es más alto que la arena más la altura de deposición, depositar encima del terreno
                sandElev[x, z] = terrainElev[x, z] + depositeHeight;
            }
            else
            {
                sandElev[x, z] += depositeHeight;
            }

            /*
            float h = Math.Max(sandElev[x, z], terrainElev[x, z]);
            float hs;

            if (openEnded && (x == 0 || z == 0))
                hs = h;
            else
            {
                int xs = (x - dx + xResolution) % xResolution;
                int zs = (z - dz + zResolution) % zResolution;
                hs = Math.Max(h, Math.Max(Math.Max(terrainElev[xs, zs], sandElev[xs, zs]), Shadow[xs, zs]) - shadowSlope);
            }

            while (hs >= (h = Math.Max(sandElev[x, z], terrainElev[x, z])))
            {
                Shadow[x, z] = (hs == h) ? 0 : hs;
                hs -= shadowSlope;
                
                x = (x + dx + xResolution) % xResolution;
                z = (z + dz + zResolution) % zResolution;

                if (openEnded && (x == 0 || z == 0))
                    return;
            }
            */
            
            UpdateShadow(x, z, dx, dz);
            
            
        }

    }
}
