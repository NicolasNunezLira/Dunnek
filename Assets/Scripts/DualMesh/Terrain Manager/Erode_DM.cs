using System;
using ue = UnityEngine;

namespace DunefieldModel_DualMesh
{
    public partial class ModelDM
    {
        #region Erode Grain
        public virtual float ErodeGrain(int x, int z, int dx, int dz, float erosionHeight)
        {
            /// <summary>
            /// Erosión de un grano de arena en el modelo de dunas.
            /// Este método simula el proceso de erosión de un grano de arena en el modelo de dunas, teniendo en cuenta la pendiente del terreno y la sombra proyectada por otros granos.
            /// /// </summary>
            /// <param name="x">Componennte x de la posición del grano a erosionar.</param>
            /// <param name="z">Componennte z de la posición del grano a erosionar.</param>
            /// <param name="dx">Componente x de la dirección del viento.</param>
            /// <param name="dz">Componente z de la dirección del viento.</param>
            /// <param name="erosionHeight">Máxima cantidad de erosión.</param>
            /// <returns>Altura erosionada.</returns>

            // Busqueda del punto más alto en la vecindad del grano
            while (FindSlope.Upslope(x, z, dx, dz, out int xSteep, out int zSteep) >= 2)
            {
                // Si se sale del dominio en campo abierto
                if (openEnded && IsOutside(xSteep, zSteep))
                    return 0f;

                x = xSteep;
                z = zSteep;
            }

            // Si el grano no tiene altura, no se erosiona
            if (terrainElev[x, z] >= sandElev[x, z]) return 0f;

            // Áltura de erosión
            erosionH = Math.Min(erosionHeight, sandElev[x, z] - terrainElev[x, z]);

            // Erosión
            sandElev[x, z] -= erosionH;
            
            UpdateShadow(x, z, dx, dz);

            ActivateCell(x, z);
            return erosionH;
        }
        #endregion
    }
}