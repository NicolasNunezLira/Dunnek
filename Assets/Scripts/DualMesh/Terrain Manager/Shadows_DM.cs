using System;

namespace DunefieldModel_DualMesh
{
    public partial class ModelDM
    {
        #region Shadow Init
        public void ShadowInit()
        {
            /// <summary>
            /// Inicializa la sombra basado en el terreno y la arena inicial.
            ///</summary>
            ShadowCheck(false, dx, dz);
        }
        #endregion

        #region Shadow Check
        protected int ShadowCheck(bool ReportErrors, int dx, int dz)
        {
            /// <summary>
            /// Verifica y actualiza la sombra del modelo de dunas.
            /// </summary>

            int height = sandElev.GetLength(0);
            int width = sandElev.GetLength(1);
            float[,] newShadow = new float[height, width];
            Array.Clear(newShadow, 0, newShadow.Length);

            int errors = 0;

            for (int x = 0; x < height; x++)
            {
                for (int z = 0; z < width; z++)
                {
                    float h = Math.Max(sandElev[x, z], terrainElev[x, z]);
                    if (h <= 0) continue;

                    int xNext = x + dx;
                    int zNext = z + dz;

                    float hs = h;
                    float randomSlope = shadowSlope *
                        ((terrainElev[x, z] >= sandElev[x, z]) ? 1f : (1f + UnityEngine.Random.Range(-0.1f, 0.1f)));

                    while (true)
                    {
                        // Verificación de bordes en modo abierto
                        if (openEnded && IsOutside(xNext, zNext)) break;

                        // Envolver si es toroidal
                        if (!openEnded)
                            (xNext, zNext) = WrapCoords(xNext, zNext);

                        float hNext = Math.Max(sandElev[xNext, zNext], terrainElev[xNext, zNext]);

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

        #endregion

        #region Update Shadow
        public void UpdateShadow(int x, int z, int dx, int dz)
        {
            /// <summary>
            /// Actualiza la sombra desde la posición (x,z) en dirección del viento (dx,dz).
            /// </summary>

            float h = Math.Max(sandElev[x, z], terrainElev[x, z]);
            float hs;

            int xPrev = x - dx;
            int zPrev = z - dz;

            if (openEnded && IsOutside(xPrev, zPrev))
            {
                hs = h;
            }
            else
            {
                if (!openEnded)
                    (xPrev, zPrev) = WrapCoords(xPrev, zPrev);

                if (IsOutside(xPrev, zPrev)) return; // Prevención por seguridad

                hs = Math.Max(
                    h,
                    Math.Max(
                        Math.Max(sandElev[xPrev, zPrev], terrainElev[xPrev, zPrev]),
                        Shadow[xPrev, zPrev]
                    ) - shadowSlope
                );
            }

            int xNext = x;
            int zNext = z;

            while (true)
            {
                if (openEnded && IsOutside(xNext, zNext)) return;
                if (!openEnded)
                    (xNext, zNext) = WrapCoords(xNext, zNext);

                float currentHeight = Math.Max(sandElev[xNext, zNext], terrainElev[xNext, zNext]);
                if (hs < currentHeight) break;

                Shadow[xNext, zNext] = (hs == currentHeight) ? 0 : hs;
                hs -= shadowSlope;

                xNext += dx;
                zNext += dz;
            }

            // Borrar sombra si ya no se proyecta
            while (true)
            {
                if (openEnded && IsOutside(xNext, zNext)) return;
                if (!openEnded)
                    (xNext, zNext) = WrapCoords(xNext, zNext);

                if (Shadow[xNext, zNext] <= 0)
                    break;

                Shadow[xNext, zNext] = 0;

                xNext += dx;
                zNext += dz;
            }

            // Ver si hay que volver a proyectar sombra después de limpiar
            hs = h - shadowSlope;

            while (true)
            {
                if (openEnded && IsOutside(xNext, zNext)) return;
                
                if (!openEnded)
                    (xNext, zNext) = WrapCoords(xNext, zNext);

                float currentHeight = Math.Max(sandElev[xNext, zNext], terrainElev[xNext, zNext]);
                if (hs < currentHeight) break;

                Shadow[xNext, zNext] = (hs == currentHeight) ? 0 : hs;
                hs -= shadowSlope;

                xNext += dx;
                zNext += dz;
            }
}

        #endregion
    }
}

