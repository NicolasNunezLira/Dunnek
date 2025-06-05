using System;
using UnityEngine.UIElements;
using ue = UnityEngine;

namespace DunefieldModel_DualMesh
{
    public partial class ModelDM
    {
        #region Shadows
        public void ShadowInit()
        {
            /// <summary>
            /// Inicializa la sombra basado en el terreno y la arena inicial.
            ///</summary>
            ShadowCheck(false, dx, dz);
        }
        protected int ShadowCheck(bool ReportErrors, int dx, int dz)
        {
            /// <summary>
            /// Verifica y actualiza la sombra del modelo de dunas.
            /// </summary>
            /// <param name="ReportErrors">Indica si se deben reportar errores.</param>
            /// <param name="dx">Componente x del viento.</param>
            /// <param name="dz">Componente z del viento.</param>
            /// <returns>El número de errores encontrados.</returns>

            // Inicializar nueva sombra
            float[,] newShadow = new float[Shadow.GetLength(0), Shadow.GetLength(1)];
            Array.Clear(newShadow, 0, newShadow.Length);

            int height = sandElev.GetLength(0);
            int width = sandElev.GetLength(1);
            int errors = 0;

            // Iterar sobre cada celda del terreno
            for (int x = 0; x < height; x++)
            {
                for (int z = 0; z < width; z++)
                {
                    float h = Math.Max(sandElev[x, z], terrainElev[x, z]);
                    if (h <= 0) continue;

                    int xNext = x + dx;
                    int zNext = z + dz;

                    //float hs = h - shadowSlope;
                    float hs = h;

                    while (IsInside(xNext, zNext) && hs >= Math.Max(sandElev[xNext, zNext], terrainElev[xNext, zNext]))
                    {
                        newShadow[xNext, zNext] = hs;
                        hs -= shadowSlope;
                        xNext += dx;
                        zNext += dz;
                    }
                }
            }

            // Ajustar sombra si iguala altura
            for (int x = 0; x < height; x++)
            {
                for (int z = 0; z < width; z++)
                {
                    if (newShadow[x, z] <= Math.Max(sandElev[x, z], terrainElev[x, z]))
                        newShadow[x, z] = 0;
                }
            }

            // Comparar con sombra anterior
            for (int x = 0; x < height; x++)
            {
                for (int z = 0; z < width; z++)
                {
                    if (newShadow[x, z] != Shadow[x, z])
                        errors++;
                }
            }

            if (errors > 0)
            {
                if (ReportErrors)
                    Console.WriteLine("shadowCheck error count: " + errors);
                Array.Copy(newShadow, Shadow, Shadow.Length);
            }

            return errors;
        }

        public void UpdateShadow(int x, int z, int dx, int dz)
        {
            /// <summary>
            /// Actualiza la sombra desde la posición (x,z) en dirección del viento (dx,dz).
            /// </summary>
            /// <param name="x">Componente x de la posición inicial.</param>
            /// <param name="z">Componente z de la posición inicial.</param>
            /// <param name="dx">Componente x del viento.</param>
            /// <param name="dz">Componente z del viento.</param>
            /// <returns>void</returns>
            
            // Actualizar la sombra en la posición inicial
            float h = Math.Max(sandElev[x, z], terrainElev[x, z]);
            float hs;
            
            int xPrev = x - dx;
            int zPrev = z - dz;

            if (openEnded && IsOutside(xPrev, zPrev))
                hs = h;
            else
            {
                (xPrev, zPrev) = WrapCoords(xPrev, zPrev);
                hs = Math.Max(h, Math.Max(Math.Max(sandElev[xPrev, zPrev], terrainElev[xPrev, zPrev]), Shadow[xPrev, zPrev]) - shadowSlope);
            }

            int xNext = x;
            int zNext = z;

            while (true)
            {
                h = Math.Max(sandElev[xNext, zNext], terrainElev[xNext, zNext]);
                if (hs < h) break;

                Shadow[xNext, zNext] = (hs == h) ? 0 : hs;
                hs -= shadowSlope;

                xNext += dx;
                zNext += dz;

                if (openEnded && IsOutside(xNext, zNext)) return;

                (xNext, zNext) = WrapCoords(xNext, zNext);
            }

            while (Shadow[xNext, zNext] > 0)
            {
                Shadow[xNext, zNext] = 0;
                
                xNext += dx;
                zNext += dz;

                if (openEnded && IsOutside(xNext, zNext)) return;

                (xNext, zNext) = WrapCoords(xNext, zNext);

                hs = h - shadowSlope;
                if (Shadow[xNext, zNext] > hs)
                {
                    while (true)
                    {
                        h = Math.Max(sandElev[xNext, zNext], terrainElev[xNext, zNext]);
                        if (hs < h) break;

                        Shadow[xNext, zNext] = (hs == h) ? 0 : hs;
                        hs -= shadowSlope;

                        xNext += dx;
                        zNext += dz;

                        if (openEnded && IsOutside(xNext, zNext)) return;

                        (xNext, zNext) = WrapCoords(xNext, zNext);
                    }
                }
            }

            
        }
        #endregion
    }
}