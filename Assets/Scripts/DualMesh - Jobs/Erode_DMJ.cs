using System;
using System.Collections.Generic;
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
            NativeArray<float> shadow,
            int xResolution,
            int zResolution,
            float slope,
            float shadowSlope,
            bool openEnded,
            NativeList<SandChanges>.ParallelWriter sandChanges
            //out FixedList32Bytes<ShadowChanges> shadowOut
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

            //shadowOut = new FixedList32Bytes<ShadowChanges>();

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
            if (index >= 0 && index <= sand.Length) sandChanges.AddNoResize(new SandChanges { index = index, delta = -erosionH });
            //sand[index] -= erosionH;


            /*
            shadow = UpdateShadow(
                x, z,
                dx, dz,
                sand, terrain, shadow,
                xResolution, zResolution,
                shadowSlope,
                openEnded);
            */
            return erosionH;
        }
        #endregion

    }
}