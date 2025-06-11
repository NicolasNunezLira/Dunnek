using System;
using Unity.Collections;
using UnityEngine;
using System.Collections.Generic;

namespace DunefieldModel_DualMeshJobs
{
    public partial class ModelDMJ
    {
        #region Tick


        public static void algorithmDeposit(
            int x, int z,
            int dx, int dz, int HopLength,
            NativeArray<float> sand,
            NativeArray<float> terrain,
            NativeArray<float> shadow,
            int xResolution, int zResolution,
            float depositeH,
            float slope,
            float shadowSlope,
            float avalancheSlope,
            float slopeThreshold,
            float pSand, float pNoSand,
            Unity.Mathematics.Random rng,
            int iter,
            bool openEnded,
            bool verbose,
            NativeList<SandChanges>.ParallelWriter sandChanges
        )
        {
            // Deposición del grano
            int hop = HopLength;
            int xCurr = x;
            int zCurr = z;
            int index = xCurr + (xResolution * zCurr);

            int targetX = xCurr, targetZ = zCurr;

            // Conteo de celdas de terreno en las posibles deposiciones del grano
            int countTerrain = 0;
            for (int j = 1; j <= hop; j++)
            {
                (int xAUx, int zAux) = WrapCoords(xCurr + j * dx, zCurr + j * dz, xResolution, zResolution);
                if (xAUx != xCurr || zAux != zCurr)
                {
                    index = xAUx + (xResolution * zAux);
                }
                if (terrain[index] >= sand[index])
                {
                    countTerrain++;
                }
            }

            while (true)
            {

                // Cálculo de la posición actual del grano considerando comportamiento toroidal
                if (openEnded)
                {
                    xCurr += dx;
                    zCurr += dz;
                }
                else
                {
                    xCurr = (xCurr + dx + xResolution) % xResolution;
                    zCurr = (zCurr + dz + zResolution) % zResolution;
                }

                int indexCurr = xCurr + (xResolution * zCurr);

                // Si el grano sale del dominio en campo abierto, detener la deposición
                if (openEnded && IsOutside(xCurr, zCurr, xResolution, zResolution))
                    break;

                // Si el grano está en sombra, depositar y salir del ciclo
                if (shadow[indexCurr] > 0 && sand[indexCurr] > terrain[indexCurr])
                {
                    //if (verbose) { Debug.Log("Grano a depositar en (" + xCurr + "," + zCurr + ")."); }
                    DepositGrain(
                        xCurr, zCurr,
                        dx, dz,
                        depositeH,
                        terrain, sand, shadow,
                        xResolution, zResolution,
                        slope, shadowSlope, avalancheSlope,
                        openEnded, iter, sandChanges);
                    targetX = xCurr;
                    targetZ = zCurr;
                    break;
                }


                if (terrain[indexCurr] >= sand[indexCurr] &&
                    terrain[indexCurr] >= sand[index])
                {
                    countTerrain -= (terrain[indexCurr] >= sand[indexCurr]) ? 1 : 0;
                    continue;
                }


                if (countTerrain >= hop - 1)
                {
                    // Direcciones laterales (perpendiculares al viento)
                    FixedList32Bytes<int> dxLateral = new FixedList32Bytes<int> { -dz, dz };
                    FixedList32Bytes<int> dzLateral = new FixedList32Bytes<int> { dx, -dx };

                    for (int j = 0; j < 2; j++)
                    {
                        int lx = (xCurr + dxLateral[j] + xResolution) % xResolution;
                        int lz = (zCurr + dzLateral[j] + zResolution) % zResolution;
                        int lateralIndex = lx + (xResolution * lz);

                        if (Math.Max(terrain[lateralIndex], sand[lateralIndex]) < Math.Max(terrain[indexCurr], sand[indexCurr]) - slopeThreshold)
                        {
                            DepositGrain(
                                lx, lz,
                                dxLateral[j], dzLateral[j],
                                depositeH,
                                terrain, sand, shadow,
                                xResolution, zResolution,
                                slope, shadowSlope, avalancheSlope,
                                openEnded, iter, sandChanges);
                            targetX = lx;
                            targetZ = lz;
                            if (verbose) Debug.Log($"Grano redirigido lateralmente a ({lx}, {lz})");
                            break;
                        }
                    }

                    DepositGrain(
                        xCurr, zCurr,
                        dx, dz,
                        depositeH,
                        terrain, sand, shadow,
                        xResolution, zResolution,
                        slope, shadowSlope, avalancheSlope,
                        openEnded, iter, sandChanges);
                    targetX = xCurr;
                    targetZ = zCurr;
                    //if (verbose) { Debug.Log("Grano a depositar en (" + xCurr + "," + zCurr + ")."); }
                    break;
                }
                countTerrain -= (terrain[indexCurr] >= sand[indexCurr]) ? 1 : 0;

                // Si el grano no está en sombra, verificar si se debe depositar
                if (--hop <= 0)
                {// Si el terreno es más alto que la arena, reiniciar posición


                    // Verificar si el grano debe depositarse basado en la altura de arena y terreno
                    if (rng.NextDouble() < (sand[indexCurr] > terrain[indexCurr] ? pSand : pNoSand))
                    {
                        DepositGrain(
                            xCurr, zCurr,
                            dx, dz,
                            depositeH,
                            terrain, sand, shadow,
                            xResolution, zResolution,
                            slope, shadowSlope, avalancheSlope,
                            openEnded, iter, sandChanges);
                        targetX = xCurr;
                        targetZ = zCurr;
                        //if (verbose) { Debug.Log("Grano a depositar en (" + xCurr + "," + zCurr + ")."); }
                        break;
                    }
                    hop = HopLength;
                }

                // 
            }
            if (index >= 0 && index <= sand.Length) sandChanges.AddNoResize(new SandChanges { index = targetX + (xResolution * targetZ), delta = depositeH });
            return;
        }
        #endregion

    }
}