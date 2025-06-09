using System;
using Unity.Collections;

namespace DunefieldModel_DualMeshJobs
{
    public partial class ModelDMJ
    {
        #region Erode Grain
        public static float ErodeGrain(
            int x, int z,
            int dx, int dz,
            float erosionHeight,
            NativeArray<float> terrain,
            NativeArray<float> sand,
            int xResolution,
            int zResolution,
            float slope,
            bool openEnded
        )
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
            while (true)
            {
                FindSlope.SlopeResult result = FindSlope.Upslope(x, z, dx, dz, sand, terrain, xResolution, zResolution, slope, false);
                // Si se sale del dominio en campo abierto
                if (!result.isValid) break;
                
                if (openEnded && (result.X < 0 || result.X >= xResolution || result.Z < 0 || result.Z >= zResolution))
                    return 0f;

                x = result.X;
                z = result.Z;
            }

            int index = x + (xResolution * z);

            // Si el grano no tiene altura, no se erosiona
            if (terrain[index] >= sand[index]) return 0f;

            // Áltura de erosión
            float erosionH = Math.Min(erosionHeight, sand[index] - terrain[index]);

            // Erosión
            sand[index] -= erosionH;
            
            //UpdateShadow(x, z, dx, dz);
            return erosionH;
        }
        #endregion

    }
}