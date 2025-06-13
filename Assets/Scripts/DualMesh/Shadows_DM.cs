using System;
using UnityEngine;
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

                    float randomSlope = shadowSlope *
                        ((terrainElev[x, z] >= sandElev[x, z]) ? 1 : (1f + (float)UnityEngine.Random.Range(-.1f, .1f))); // Añadir un poco de aleatoriedad a la pendiente

                    while (IsInside(xNext, zNext) && hs >= Math.Max(sandElev[xNext, zNext], terrainElev[xNext, zNext]))
                    {
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


        /*
        protected int ShadowCheck(bool ReportErrors, int dx, int dz)
        {
            /// <summary>
            /// Verifica y actualiza la sombra del modelo de dunas con proyección cónica.
            /// </summary>
            /// <param name="ReportErrors">Indica si se deben reportar errores.</param>
            /// <param name="dx">Componente x del viento.</param>
            /// <param name="dz">Componente z del viento.</param>
            /// <returns>El número de errores encontrados.</returns>

            int height = sandElev.GetLength(0);
            int width = sandElev.GetLength(1);
            int errors = 0;

            float[,] newShadow = new float[height, width];
            Array.Clear(newShadow, 0, newShadow.Length);

            int maxSteps = 20; // Largo máximo de sombra
            float baseRadius = 3f; // Ancho inicial de la sombra
            float endRadius = 0.5f; // Ancho final (punta)
            float baseSlope = shadowSlope; // Slope base de pérdida de altura

            UnityEngine.Vector2 windDir = new UnityEngine.Vector2(dx, dz).normalized;
            UnityEngine.Vector2 perp = new UnityEngine.Vector2(-windDir.y, windDir.x); // perpendicular al viento

            for (int x = 0; x < height; x++)
            {
                for (int z = 0; z < width; z++)
                {
                    float h = Math.Max(sandElev[x, z], terrainElev[x, z]);
                    if (h <= 0) continue;

                    float hs = h;

                    // Slope con aleatoriedad
                    float randomSlope = baseSlope * (
                        (terrainElev[x, z] >= sandElev[x, z]) ?
                        1f :
                        1f + UnityEngine.Random.Range(-0.1f, 0.1f));

                    for (int step = 1; step < maxSteps && hs > 0; step++)
                    {
                        UnityEngine.Vector2 basePos = new UnityEngine.Vector2(x, z) + windDir * step;

                        // Radio lateral que disminuye con la distancia (forma cónica)
                        float t = step / (float)maxSteps;
                        int radius = Mathf.CeilToInt(Mathf.Lerp(baseRadius, endRadius, t));

                        for (int i = -radius; i <= radius; i++)
                        {
                            UnityEngine.Vector2 offset = basePos + perp * i;
                            int sx = Mathf.RoundToInt(offset.x);
                            int sz = Mathf.RoundToInt(offset.y);

                            if (!IsInside(sx, sz)) continue;

                            float cellElev = Math.Max(sandElev[sx, sz], terrainElev[sx, sz]);
                            if (hs >= cellElev)
                            {
                                newShadow[sx, sz] = Math.Max(newShadow[sx, sz], hs);
                            }
                        }

                        hs -= randomSlope;
                    }
                }
            }

            // Eliminar sombra que no sobrepasa altura
            for (int x = 0; x < height; x++)
            {
                for (int z = 0; z < width; z++)
                {
                    if (newShadow[x, z] <= Math.Max(sandElev[x, z], terrainElev[x, z]))
                        newShadow[x, z] = 0f;
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

            // Si hubo cambios, copiar nueva sombra
            if (errors > 0)
            {
                if (ReportErrors)
                    Console.WriteLine("shadowCheck error count: " + errors);
                Array.Copy(newShadow, Shadow, Shadow.Length);
            }

            return errors;
        }
*/

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

            /*
            public void UpdateShadow(int x, int z, int dx, int dz)
            {
                /// <summary>
                /// Proyecta sombra cónica desde (x, z) en dirección del viento (dx, dz), usando altura local.
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
                    (xPrev, zPrev) = WrapCoords(xPrev, zPrev);
                    hs = Math.Max(h, Math.Max(Math.Max(sandElev[xPrev, zPrev], terrainElev[xPrev, zPrev]), Shadow[xPrev, zPrev]) - shadowSlope);
                }

                Vector2 windDir = new Vector2(dx, dz).normalized;
                Vector2 perp = new Vector2(-windDir.y, windDir.x);
                int maxSteps = 20;
                float baseRadius = 3f;
                float endRadius = 0.5f;
                float currentHs = hs;

                for (int step = 0; step < maxSteps && currentHs > 0; step++)
                {
                    Vector2 basePos = new Vector2(x, z) + windDir * step;

                    float t = step / (float)maxSteps;
                    int radius = Mathf.CeilToInt(Mathf.Lerp(baseRadius, endRadius, t));

                    for (int i = -radius; i <= radius; i++)
                    {
                        Vector2 offset = basePos + perp * i;
                        int xi = Mathf.RoundToInt(offset.x);
                        int zi = Mathf.RoundToInt(offset.y);

                        if (openEnded && IsOutside(xi, zi)) continue;
                        (xi, zi) = WrapCoords(xi, zi);

                        float localH = Math.Max(sandElev[xi, zi], terrainElev[xi, zi]);
                        if (currentHs < localH) continue;

                        Shadow[xi, zi] = (currentHs == localH) ? 0 : Math.Max(Shadow[xi, zi], currentHs);
                    }

                    currentHs -= shadowSlope;
                }
            }
    */


        }
        #endregion
    }
}

