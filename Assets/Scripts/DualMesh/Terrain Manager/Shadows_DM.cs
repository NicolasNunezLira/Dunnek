using System;
using Unity.Collections;
using UnityEngine.UI;

namespace DunefieldModel_DualMesh
{
    public partial class ModelDM
    {
        #region shadow Init
        public void ShadowInit()
        {
            /// <summary>
            /// Inicializa la sombra basado en el terreno y la arena inicial.
            ///</summary>
            ShadowCheck(false, dx, dz);
        }
        #endregion

        #region shadow Check
        protected int ShadowCheck(bool ReportErrors, int dx, int dz)
        {
            /// <summary>
            /// Verifica y actualiza la sombra del modelo de dunas.
            /// </summary>

            int height = sand.Height;
            int width = sand.Width;
            NativeGrid newShadow = new NativeGrid(width, height, sand.VisualWidth, sand.VisualHeight, Allocator.Persistent);

            int errors = 0;

            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < height; z++)
                {
                    float h = Math.Max(sand[x, z], terrainShadow[x, z]);
                    if (h <= 0) continue;

                    int xNext = x + dx;
                    int zNext = z + dz;

                    float hs = h;
                    float randomSlope = shadowSlope *
                        ((terrainShadow[x, z] >= sand[x, z]) ? 1f : (1f + UnityEngine.Random.Range(-0.1f, 0.1f)));

                    while (true)
                    {
                        // Verificación de bordes en modo abierto
                        if (openEnded && IsOutside(xNext, zNext)) break;

                        // Envolver si es toroidal
                        if (!openEnded)
                            (xNext, zNext) = WrapCoords(xNext, zNext);

                        float hNext = Math.Max(sand[xNext, zNext], terrainShadow[xNext, zNext]);

                        if (hs < hNext) break;

                        newShadow[xNext, zNext] = hs;
                        hs -= randomSlope;

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
                    if (newShadow[x, z] <= Math.Max(sand[x, z], terrainShadow[x, z]))
                        newShadow[x, z] = 0;
                }
            }

            // Comparar con sombra anterior
            for (int x = 0; x < height; x++)
            {
                for (int z = 0; z < width; z++)
                {
                    if (newShadow[x, z] != shadow[x, z])
                        errors++;
                }
            }

            if (errors > 0)
            {
                if (ReportErrors)
                    Console.WriteLine("shadowCheck error count: " + errors);
                shadow.CopyFrom(newShadow);
            }
            newShadow.Dispose();
            return errors;
        }

        #endregion

        #region Update shadow
        public void UpdateShadow(int x, int z, int dx, int dz)
        {
            /// <summary>
            /// Actualiza la sombra desde la posición (x,z) en dirección del viento (dx,dz).
            /// </summary>

            float h = Math.Max(sand[x, z], terrainShadow[x, z]);
            float hs;

            int xPrev = x - dx;
            int zPrev = z - dz;

            hs = Math.Max(
                    h,
                    Math.Max(
                        Math.Max(sand[xPrev, zPrev], terrainShadow[xPrev, zPrev]),
                        shadow[xPrev, zPrev]
                    ) - shadowSlope
                );

            int xNext = x;
            int zNext = z;

            while (true)
            {
                float currentHeight = Math.Max(sand[xNext, zNext], terrainShadow[xNext, zNext]);
                if (hs < currentHeight) break;

                shadow[xNext, zNext] = (hs == currentHeight) ? 0 : hs;
                hs -= shadowSlope;

                xNext += dx;
                zNext += dz;
            }

            // Borrar sombra si ya no se proyecta
            while (true)
            {
                if (shadow[xNext, zNext] <= 0)
                    break;

                shadow[xNext, zNext] = 0;

                xNext += dx;
                zNext += dz;
            }

            // Ver si hay que volver a proyectar sombra después de limpiar
            hs = h - shadowSlope;

            while (true)
            {
                float currentHeight = Math.Max(sand[xNext, zNext], terrainShadow[xNext, zNext]);
                if (hs < currentHeight) break;

                shadow[xNext, zNext] = (hs == currentHeight) ? 0 : hs;
                hs -= shadowSlope;

                xNext += dx;
                zNext += dz;
            }
}

        #endregion
    }
}

