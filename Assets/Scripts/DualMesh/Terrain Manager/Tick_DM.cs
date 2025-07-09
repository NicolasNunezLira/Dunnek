using System;
using Unity.VisualScripting;
using UnityEngine;

namespace DunefieldModel_DualMesh
{
    public partial class ModelDM
    {
        #region Tick
        public virtual void Tick(int grainsPerStep, int dx, int dz, float erosionHeight, float depositeHeight)
        {
            /// <summary> 
            /// Función que simula un tick del modelo de dunas.
            /// </summary>
            /// <param name="grainsPerStep">Número de granos a erosionar por tick.</param>
            /// <param name="dx">Componente x del viento.</param>
            /// <param name="dz">Componente z del viento</param>
            /// <param name="erosionHeight">Altura máxima de erosión por grano.</param>
            /// <param name="depositeHeight">Altura de deposición por grano.</param>
            /// <returns>void</returns>

            // Ciclo para el movimiento de granos
            for (int subticks = grainsPerStep; subticks > 0; subticks--)
            {
                int x = rnd.Next(0, sand.Width);
                int z = rnd.Next(0, sand.Height);

                if (shadow[x, z] > 0 || terrainShadow[x, z] >= sand[x, z]) // Si el grano está en sombra o no hay arena sobre el terreno, saltar
                {
                    continue;
                }

                depositeH = ErodeGrain(x, z, dx, dz, erosionHeight);

                if (depositeH <= 0f) continue;

                AlgorithmDeposit(x, z, dx, dz, depositeH);
            }
        }

        #region Deposit
        public void AlgorithmDeposit(int x, int z, int dx, int dz, float depositeH)
        {
            int i = HopLength;
            int xCurr = x;
            int zCurr = z;

            // Conteo de celdas de terreno en el camino
            int countTerrain = 0;
            for (int j = 1; j <= i; j++)
            {
                int xAux = xCurr + j * dx;
                int zAux = zCurr + j * dz;

                if (!openEnded)
                    (xAux, zAux) = WrapCoords(xAux, zAux);
                else if (!IsInside(xAux, zAux))
                    break;

                if (terrainShadow[xAux, zAux] >= sand[xAux, zAux])
                    countTerrain++;
            }

            while (true)
            {
                #region Barlovento behaviour with structures
                int steps = Math.Max(Math.Abs(dx), Math.Abs(dz));
                int stepX = dx / steps;
                int stepZ = dz / steps;

                for (int s = 1; s <= steps; s++)
                {
                    int checkX = xCurr + s * stepX + dx;
                    int checkZ = zCurr + s * stepZ + dz;

                    if (openEnded && !IsInside(checkX, checkZ)) return;
                    if (!openEnded)
                        (checkX, checkZ) = WrapCoords(checkX, checkZ);

                    if (constructionGrid[checkX, checkZ] > 0)
                    {
                        int xPrev = checkX - dx;
                        int zPrev = checkZ - dz;
                        if (!openEnded)
                            (xPrev, zPrev) = WrapCoords(xPrev, zPrev);
                        else if (!IsInside(xPrev, zPrev)) return;

                        float acumulacionBarlovento = terrainShadow[checkX, checkZ] - sand[xPrev, zPrev];

                        if (acumulacionBarlovento <= 0.2f)
                        {
                            DepositGrain(checkX, checkZ, dx, dz, depositeH);
                            TryToDeleteBuild(checkX, checkZ);
                            return;
                        }
                        else
                        {
                            int stopX = xCurr + (s - 1) * stepX;
                            int stopZ = zCurr + (s - 1) * stepZ;
                            DepositGrain(stopX, stopZ, dx, dz, depositeH);
                            TryToDeleteBuild(checkX, checkZ);
                            return;
                        }
                    }
                }
                #endregion

                #region Open field behaviour
                xCurr += dx;
                zCurr += dz;

                if (openEnded && !IsInside(xCurr, zCurr)) return;
                if (!openEnded)
                    (xCurr, zCurr) = WrapCoords(xCurr, zCurr);

                if (shadow[xCurr, zCurr] > 0 && sand[xCurr, zCurr] > terrainShadow[xCurr, zCurr])
                {
                    DepositGrain(xCurr, zCurr, dx, dz, depositeH);
                    return;
                }

                if (terrainShadow[xCurr, zCurr] >= sand[xCurr, zCurr] &&
                    terrainShadow[xCurr, zCurr] >= sand[x, z])
                {
                    countTerrain -= (terrainShadow[xCurr, zCurr] >= sand[xCurr, zCurr]) ? 1 : 0;
                    continue;
                }

                if (countTerrain >= i - 1)
                {
                    int[] dxLateral = { -dz, dz };
                    int[] dzLateral = { dx, -dx };

                    for (int j = 0; j < 2; j++)
                    {
                        for (int k = 1; k <= i; k++)
                        {
                            int lx = xCurr + dxLateral[j] * k;
                            int lz = zCurr + dzLateral[j] * k;

                            if (openEnded && !IsInside(lx, lz)) break;
                            if (!openEnded)
                                (lx, lz) = WrapCoords(lx, lz);

                            if (Math.Max(terrainShadow[lx, lz], sand[lx, lz]) <
                                Math.Max(terrainShadow[xCurr, zCurr], sand[xCurr, zCurr]) - slopeThreshold)
                            {
                                DepositGrain(lx, lz, dxLateral[j], dzLateral[j], depositeH);
                                return;
                            }
                        }
                    }
                }

                countTerrain -= (terrainShadow[xCurr, zCurr] >= sand[xCurr, zCurr]) ? 1 : 0;

                if (--i <= 0)
                {
                    if (rnd.NextDouble() < (sand[xCurr, zCurr] > terrainShadow[xCurr, zCurr] ? pSand : pNoSand))
                    {
                        DepositGrain(xCurr, zCurr, dx, dz, depositeH);
                        return;
                    }
                    i = HopLength;
                }
                #endregion
            }
        }
        #endregion

        #endregion
    }
}