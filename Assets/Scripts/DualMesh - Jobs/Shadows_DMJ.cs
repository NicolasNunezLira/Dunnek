using System;
using Unity.Collections;
using UnityEngine.UI;
using UnityEngine.UIElements;
using ue = UnityEngine;
using System.Collections.Generic;

namespace DunefieldModel_DualMeshJobs
{
    public partial class ModelDMJ
    {
        #region Shadows
        public static void ShadowInit(
            int dx, int dz,
            NativeArray<float> sand,
            NativeArray<float> terrain,
            int xResolution,
            int zResolution,
            NativeArray<float> shadow,
            float shadowSlope,
            ref List<ShadowChanges> shadowOut
        )
        {
            /// <summary>
            /// Inicializa la sombra basado en el terreno y la arena inicial.
            ///</summary>
            ShadowCheck(false, dx, dz, sand, terrain, xResolution, zResolution, shadow, shadowSlope, ref shadowOut);
        }
        protected static float ShadowCheck(
            bool ReportErrors,
            int dx, int dz,
            NativeArray<float> sand,
            NativeArray<float> terrain,
            int xResolution,
            int zResolution,
            NativeArray<float> shadow,
            float shadowSlope,
            ref List<ShadowChanges> shadowOut
        )
        {
            /// <summary>
            /// Verifica y actualiza la sombra del modelo de dunas.
            /// </summary>
            /// <param name="ReportErrors">Indica si se deben reportar errores.</param>
            /// <param name="dx">Componente x del viento.</param>
            /// <param name="dz">Componente z del viento.</param>
            /// <returns>El número de errores encontrados.</returns>

            // Inicializar nueva sombra
            NativeArray<float> newShadow = new NativeArray<float>(shadow.Length, Allocator.Temp);

            int errors = 0;

            // Iterar sobre cada celda del terreno
            for (int x = 0; x < xResolution; x++)
            {
                for (int z = 0; z < zResolution; z++)
                {
                    int index = x + (xResolution * z);
                    float h = Math.Max(sand[index], terrain[index]);
                    if (h <= 0) continue;

                    int xNext = x + dx;
                    int zNext = z + dz;
                    int indexNext = xNext + (xResolution * zNext);

                    //float hs = h - shadowSlope;
                    float hs = h;

                    float randomSlope = shadowSlope *
                        ((terrain[index] >= sand[index]) ? 1 : (1f + (float)UnityEngine.Random.Range(-.1f, .1f))); // Añadir un poco de aleatoriedad a la pendiente

                    while (xNext >= 0 && xNext <= xResolution - 1 && zNext >= 0 && zNext <= zResolution - 1 && hs >= Math.Max(sand[indexNext], terrain[indexNext]))
                    {
                        newShadow[indexNext] = hs;
                        hs -= randomSlope;
                        xNext += dx;
                        zNext += dz;

                        indexNext = xNext + (xResolution * zNext);
                    }
                }
            }

            // Ajustar sombra si iguala altura
            for (int x = 0; x < xResolution; x++)
            {
                for (int z = 0; z < zResolution; z++)
                {
                    int index = x + (xResolution * z);
                    if (newShadow[index] <= Math.Max(sand[index], terrain[index]))
                        newShadow[index] = 0;
                }
            }

            // Comparar con sombra anterior
            for (int x = 0; x < xResolution; x++)
            {
                for (int z = 0; z < zResolution; z++)
                {
                    int index = x + (xResolution * z);
                    if (newShadow[index] != shadow[index])
                        shadowOut.Add(new ShadowChanges { index = index, value = newShadow[index] });
                        errors++;
                }
            }

            if (errors > 0)
            {
                if (ReportErrors)
                    Console.WriteLine("shadowCheck error count: " + errors);
                //NativeArray<float>.Copy(newShadow, shadow);

            }

            newShadow.Dispose();

            return errors;
        }

        /*
        public static NativeArray<float> UpdateShadow(
            int x, int z,
            int dx, int dz,
            NativeArray<float> sand,
            NativeArray<float> terrain,
            NativeArray<float> shadow,
            int xResolution,
            int zResolution,
            float shadowSlope,
            bool openEnded
            )
        {
            /// <summary>
            /// Actualiza la sombra desde la posición (x,z) en dirección del viento (dx,dz).
            /// </summary>
            /// <param name="x">Componente x de la posición inicial.</param>
            /// <param name="z">Componente z de la posición inicial.</param>
            /// <param name="dx">Componente x del viento.</param>
            /// <param name="dz">Componente z del viento.</param>
            /// <returns>void</returns>

            int index = x + (xResolution * z);
            // Actualizar la sombra en la posición inicial
            float h = Math.Max(sand[index], terrain[index]);
            float hs;

            int xPrev = x - dx;
            int zPrev = z - dz;

            if (openEnded && IsOutside(xPrev, zPrev, xResolution, zResolution))
                hs = h;
            else
            {
                (xPrev, zPrev) = WrapCoords(xPrev, zPrev, xResolution, zResolution);
                int indexPrev = xPrev + (xResolution * zPrev);
                hs = Math.Max(h, Math.Max(Math.Max(sand[indexPrev], terrain[indexPrev]), shadow[indexPrev]) - shadowSlope);
            }

            int xNext = x;
            int zNext = z;
            int indexNext = index;

            while (true)
            {
                h = Math.Max(sand[indexNext], terrain[indexNext]);
                if (hs < h) break;

                shadow[indexNext] = (hs == h) ? 0 : hs;
                hs -= shadowSlope;

                xNext += dx;
                zNext += dz;

                if (openEnded && IsOutside(xNext, zNext, xResolution, zResolution)) return shadow;

                (xNext, zNext) = WrapCoords(xNext, zNext, xResolution, zResolution);
                indexNext = xNext + (xResolution * zNext);
            }

            while (shadow[indexNext] > 0)
            {
                shadow[indexNext] = 0;

                xNext += dx;
                zNext += dz;

                if (openEnded && IsOutside(xNext, zNext, xResolution, zResolution)) return shadow;

                (xNext, zNext) = WrapCoords(xNext, zNext, xResolution, zResolution);
                indexNext = xNext + (xResolution * zNext);

                hs = h - shadowSlope;
                if (shadow[indexNext] > hs)
                {
                    while (true)
                    {
                        h = Math.Max(sand[indexNext], terrain[indexNext]);
                        if (hs < h) break;

                        shadow[indexNext] = (hs == h) ? 0 : hs;
                        hs -= shadowSlope;

                        xNext += dx;
                        zNext += dz;

                        if (openEnded && IsOutside(xNext, zNext, xResolution, zResolution)) return shadow;

                        (xNext, zNext) = WrapCoords(xNext, zNext, xResolution, zResolution);
                        indexNext = xNext + (xResolution * zNext);
                    }
                }
            }

            return shadow;


        }
        */
        #endregion
    }
}