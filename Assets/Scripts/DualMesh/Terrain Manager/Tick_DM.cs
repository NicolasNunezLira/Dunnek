using System;
using System.Collections.Generic;
using Data;

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

                    //if (checkX >= 0 && checkX < constructionGrid.GetLength(0) &&
                    //    checkZ >= 0 && checkZ < constructionGrid.GetLength(1))
                    if (constructionGrid.IsValid(checkX, checkZ))
                    {
                        List<int> ids = constructionGrid[checkX, checkZ];
                        //if (id > 0)
                        foreach (int id in ids)
                        {
                            constructions.TryGetValue(id, out ConstructionData currentConstruction);
                            ;
                            int xPrev = checkX - dx;
                            int zPrev = checkZ - dz;

                            float acumulacionBarlovento = terrainShadow[checkX, checkZ] - sand[xPrev, zPrev];

                            if (acumulacionBarlovento <= currentConstruction.buildHeight * 0.2f)
                            {
                                DepositGrain(checkX, checkZ, dx, dz, depositeH);
                                sandChanges.AddChanges(checkX, checkZ);
                                TryToDeleteBuild(checkX, checkZ);
                                return;
                            }
                            else
                            {
                                int stopX = xCurr + (s - 1) * stepX;
                                int stopZ = zCurr + (s - 1) * stepZ;
                                DepositGrain(stopX, stopZ, dx, dz, depositeH);
                                sandChanges.AddChanges(checkX, checkZ);
                                TryToDeleteBuild(checkX, checkZ);
                                return;
                            }
                        }
                    }
                    else
                    {
                        continue;
                    }
                }
                #endregion

                #region Open field behaviour
                xCurr += dx;
                zCurr += dz;

                if (shadow[xCurr, zCurr] > 0 && sand[xCurr, zCurr] > terrainShadow[xCurr, zCurr])
                {
                    DepositGrain(xCurr, zCurr, dx, dz, depositeH);
                    sandChanges.AddChanges(xCurr, zCurr);
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

                            if (Math.Max(terrainShadow[lx, lz], sand[lx, lz]) <
                                Math.Max(terrainShadow[xCurr, zCurr], sand[xCurr, zCurr]) - slopeThreshold)
                            {
                                DepositGrain(lx, lz, dxLateral[j], dzLateral[j], depositeH);
                                sandChanges.AddChanges(lx, lz);
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
                        sandChanges.AddChanges(xCurr, zCurr);
                        return;
                    }
                    i = HopLength;
                }
                #endregion
            }
        }
        #endregion

        #endregion
    
    
    /*
        public virtual void Tick(int grainsPerStep, int dx, int dz, float erosionHeight, float depositeHeight)
        {
            for (int subticks = grainsPerStep; subticks > 0; subticks--)
            {
                int x = rnd.Next(0, sand.Width);
                int z = rnd.Next(0, sand.Height);

                if (shadow[x, z] > 0 || terrainShadow[x, z] >= sand[x, z])
                    continue;

                depositeH = ErodeGrain(x, z, dx, dz, erosionHeight);

                if (depositeH <= 0f)
                    continue;

                if (TryFindDepositLocation(x, z, dx, dz, out int xFinal, out int zFinal, out bool shouldTryDelete))
                {
                    DepositGrain(xFinal, zFinal, dx, dz, depositeH);
                    AddChanges(sandChanges, xFinal, zFinal);
                    if (shouldTryDelete && IsVisual(xFinal, zFinal))
                    {
                        TryToDeleteBuild(xFinal, zFinal); // si es relevante aquí
                    }
                }
            }
        }
    
    public bool TryFindDepositLocation(
        int x, int z, int dx, int dz,
        out int xFinal, out int zFinal, out bool shouldTryDelete)
    {
        xFinal = x;
        zFinal = z;
        shouldTryDelete = false;

        int i = HopLength;
        int xCurr = x;
        int zCurr = z;
        int countTerrain = 0;

        for (int j = 1; j <= i; j++)
        {
            int xAux = xCurr + j * dx;
            int zAux = zCurr + j * dz;

            if (terrainShadow[xAux, zAux] >= sand[xAux, zAux])
                countTerrain++;
        }

        while (true)
        {
            int steps = Math.Max(Math.Abs(dx), Math.Abs(dz));
            int stepX = dx / steps;
            int stepZ = dz / steps;

            for (int s = 1; s <= steps; s++)
            {
                int checkX = xCurr + s * stepX + dx;
                int checkZ = zCurr + s * stepZ + dz;

                if (checkX >= 0 && checkX < constructionGrid.GetLength(0) &&
                    checkZ >= 0 && checkZ < constructionGrid.GetLength(1))
                {
                    int id = constructionGrid[checkX, checkZ];
                    if (id > 0)
                    {
                        constructions.TryGetValue(id, out ConstructionData currentConstruction);

                        int xPrev = checkX - dx;
                        int zPrev = checkZ - dz;

                        shouldTryDelete = true;

                        float acumulacionBarlovento = terrainShadow[checkX, checkZ] - sand[xPrev, zPrev];

                        if (acumulacionBarlovento <= Math.Min(currentConstruction.buildHeight * 0.2f, 0.2f))
                        {
                            xFinal = checkX;
                            zFinal = checkZ;
                            return true;
                        }
                        else
                        {
                            xFinal = xCurr + (s - 1) * stepX;
                            zFinal = zCurr + (s - 1) * stepZ;
                            return true;
                        }
                    }
                }
            }

            // Campo abierto
            xCurr += dx;
            zCurr += dz;

            if (shadow[xCurr, zCurr] > 0 && sand[xCurr, zCurr] > terrainShadow[xCurr, zCurr])
            {
                xFinal = xCurr;
                zFinal = zCurr;
                return true;
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

                        if (Math.Max(terrainShadow[lx, lz], sand[lx, lz]) <
                            Math.Max(terrainShadow[xCurr, zCurr], sand[xCurr, zCurr]) - slopeThreshold)
                        {
                            xFinal = lx;
                            zFinal = lz;
                            return true;
                        }
                    }
                }
            }

            countTerrain -= (terrainShadow[xCurr, zCurr] >= sand[xCurr, zCurr]) ? 1 : 0;

            if (--i <= 0)
            {
                if (rnd.NextDouble() < (sand[xCurr, zCurr] > terrainShadow[xCurr, zCurr] ? pSand : pNoSand))
                {
                    xFinal = xCurr;
                    zFinal = zCurr;
                    return true;
                }
                i = HopLength;
            }
        }
    }
    */


    }
}