using System;
using System.Collections.Generic;
using Data;
using Unity.Mathematics;
using ue = UnityEngine;

namespace DunefieldModel_DualMesh
{
    public partial class ModelDM
    {
        #region Tick
        public virtual void Tick(int grainsPerStep, int dx, int dz, float erosionHeight, float depositeHeight, bool verbose = false)
        {
            /// <summary> 
            /// Función que simula un tick del modelo de dunas.
            /// </summary>
            /// <param name="grainsPerStep">Número de granos a erosionar por tick.</param>
            /// <param name="dx">Componente x del viento.</param>
            /// <param name="dz">Componente z del viento</param>
            /// <param name="erosionHeight">Altura máxima de erosión por grano.</param>
            /// <param name="depositeHeight">Altura de deposición por grano.</param>
            /// <param name="verbose">Si es verdadero, imprime información detallada sobre el proceso.</param>
            /// <returns>void</returns>

            // Información para debug 
            if (verbose)
            {
                int count1 = 0;
                int count2 = 0;
                int count3 = 0;
                for (int i = 0; i < sandElev.GetLength(0); i++)
                {
                    for (int j = 0; j < sandElev.GetLength(1); j++)
                    {
                        if ((sandElev[i, j] - terrainElev[i, j]) > 0) count1++;
                        if (Shadow[i, j] <= 0) count2++;
                        if ((sandElev[i, j] - terrainElev[i, j] > 0) && Shadow[i, j] <= 0) count3++;
                    }
                }
                ue.Debug.Log("Cantidad de nodos erosionables:" + count1);
                ue.Debug.Log("Cantidad de nodos sin sombra:" + count2);
                ue.Debug.Log("Cantidad de nodos erosionables sin sombra:" + count3);
            }

            // Ciclo para el movimiento de granos
            int count = 0;
            for (int subticks = grainsPerStep; subticks > 0; subticks--)
            {
                // Elección aleatoria de un grano
                int x = rnd.Next(0, xResolution);
                int z = rnd.Next(0, zResolution);

                if (Shadow[x, z] > 0 || terrainElev[x, z] >= sandElev[x, z]) // Si el grano está en sombra o no hay arena sobre el terreno, saltar
                {
                    if (verbose) { ue.Debug.Log("Grano (" + x + "," + z + ") en sombra o terreno elevado."); }
                    ;
                    continue;
                }

                // Erosión del grano
                if (verbose) { ue.Debug.Log("Grano a erosionar en (" + x + "," + z + ")."); }
                depositeH = ErodeGrain(x, z, dx, dz, erosionHeight);

                if (depositeH <= 0f) continue;

                count++;

                AlgorithmDeposit(x, z, dx, dz, depositeH, verbose);

                if (verbose) { ue.Debug.Log("Granos erosionados en este tick:" + count + "/" + grainsPerStep); }
            }
        }

        #region Deposit
        public void AlgorithmDeposit(int x, int z, int dx, int dz, float depositeH, bool verbose = false)
        {
            int i = HopLength;
            int xCurr = x;
            int zCurr = z;

            // Conteo de celdas de terreno en las posibles deposiciones del grano
            int countTerrain = 0;
            for (int j = 1; j <= i; j++)
            {
                (int xAUx, int zAux) = openEnded ? (xCurr + j * dx, zCurr + j * dz) : WrapCoords(xCurr + j * dx, zCurr + j * dz);

                if (openEnded && IsOutside(xAUx, zAux)) break;

                if (terrainElev[xAUx, zAux] >= sandElev[xAUx, zAux])
                {
                    countTerrain++;
                }
            }

            while (true)
            {
                #region Barlovento behaviour with structures
                int steps = Math.Max(Math.Abs(dx), Math.Abs(dz));
                int stepX = dx / steps;  // dirección normalizada
                int stepZ = dz / steps;

                for (int s = 1; s <= steps; s++)
                {
                    int checkX = openEnded ? xCurr + s * stepX + dx : (xCurr + s * stepX + dx + xResolution) % xResolution;
                    int checkZ = openEnded ? zCurr + s * stepZ + dz : (zCurr + s * stepZ + dz + zResolution) % zResolution;

                    if (openEnded && IsOutside(checkX, checkZ)) continue;

                    if (constructionGrid[checkX, checkZ] > 0)
                    {
                        // Celda inmediatamente anterior (en barlovento)
                        int xPrev = openEnded ? checkX - dx : (checkX - dx + xResolution) % xResolution;
                        int zPrev = openEnded ? checkX - dx : (checkZ - dz + zResolution) % zResolution;

                        if (openEnded && IsOutside(xPrev, zPrev)) break;

                        // Verificar acumulación de arena frente a la construcción
                        float acumulacionBarlovento = terrainElev[checkX, checkZ] - sandElev[xPrev, zPrev];

                        if (acumulacionBarlovento <= 0.2f) // umbralBarlovento = 0.2f
                        {
                            // Se permite depósito sobre construcción por acumulación barlovento
                            DepositGrain(checkX, checkZ, dx, dz, depositeH);
                            if (verbose) ue.Debug.Log($"Acumulación barlovento permite depósito en construcción ({checkX}, {checkZ})");
                            TryToDeleteBuild(checkX, checkZ);
                            return;
                        }
                        else
                        {
                            // Depositar justo antes de la construcción
                            int stopX = xCurr + (s - 1) * stepX;
                            int stopZ = zCurr + (s - 1) * stepZ;
                            DepositGrain(stopX, stopZ, dx, dz, depositeH);
                            if (verbose) ue.Debug.Log($"Construcción bloquea paso en ({checkX}, {checkZ}), sin acumulación barlovento. Deposita en ({stopX}, {stopZ})");
                            TryToDeleteBuild(checkX, checkZ);
                            return;
                        }
                    }
                }
                #endregion

                #region Open field behaviour
                // Cálculo de la posición actual del grano considerando comportamiento toroidal
                xCurr = openEnded ? xCurr + dx : (xCurr + dx + xResolution) % xResolution;
                zCurr = openEnded ? zCurr + dz : (zCurr + dx + zResolution) % zResolution;

                // Si el grano sale del dominio en campo abierto, detener la deposición
                if (openEnded && !IsInside(xCurr, zCurr))
                    return;

                // Si el grano está en sombra, depositar y salir del ciclo
                if (Shadow[xCurr, zCurr] > 0 && sandElev[xCurr, zCurr] > terrainElev[xCurr, zCurr])
                {
                    if (verbose) { ue.Debug.Log("Grano a depositar en (" + xCurr + "," + zCurr + ")."); }
                    DepositGrain(xCurr, zCurr, dx, dz, depositeH);
                    return;
                }


                if (terrainElev[xCurr, zCurr] >= sandElev[xCurr, zCurr] &&
                    terrainElev[xCurr, zCurr] >= sandElev[x, z])
                {
                    countTerrain -= (terrainElev[xCurr, zCurr] >= sandElev[xCurr, zCurr]) ? 1 : 0;
                    continue;
                }


                if (countTerrain >= i - 1)
                {
                    // Direcciones laterales (perpendiculares al viento)
                    int[] dxLateral = { -dz, dz };
                    int[] dzLateral = { dx, -dx };

                    bool deposited = false;

                    for (int j = 0; j < 2 && !deposited; j++)
                    {
                        int k = 1;
                        while (k <= i)
                        {
                            int lx = openEnded ? xCurr + dxLateral[j] : (xCurr + dxLateral[j] * k + xResolution) % xResolution;
                            int lz = openEnded ? zCurr + dzLateral[j] : (zCurr + dzLateral[j] * k + zResolution) % zResolution;

                            if (Math.Max(terrainElev[lx, lz], sandElev[lx, lz]) < Math.Max(terrainElev[xCurr, zCurr], sandElev[xCurr, zCurr]) - slopeThreshold)
                            {
                                DepositGrain(lx, lz, dxLateral[j], dzLateral[j], depositeH);
                                if (verbose) ue.Debug.Log($"Grano redirigido lateralmente a ({lx}, {lz})");
                                return;
                            }
                            k++;
                        }
                    }
                    //break;
                }
                countTerrain -= (terrainElev[xCurr, zCurr] >= sandElev[xCurr, zCurr]) ? 1 : 0;

                // Si el grano no está en sombra, verificar si se debe depositar
                if (--i <= 0)
                {// Si el terreno es más alto que la arena, reiniciar posición


                    // Verificar si el grano debe depositarse basado en la altura de arena y terreno
                    if (rnd.NextDouble() < (sandElev[xCurr, zCurr] > terrainElev[xCurr, zCurr] ? pSand : pNoSand))
                    {
                        DepositGrain(xCurr, zCurr, dx, dz, depositeH);
                        if (verbose) { ue.Debug.Log("Grano a depositar en (" + xCurr + "," + zCurr + ")."); }
                        return;
                    }
                    i = HopLength;
                }

            }
            #endregion

        }
        #endregion

        #endregion

        #region Total sand amount

        public float TotalSand()
        {
            float total = 0f;
            for (int i = 0; i < xResolution; i++)
                for (int j = 0; j < zResolution; j++)
                    total += sandElev[i, j] - terrainElev[i, j];
            return total;
        }

        #endregion

        #region Destroy buried builds

        public void TryToDeleteBuild(int checkX, int checkZ)
        {
            int id = constructionGrid[checkX, checkZ];
            if (!constructions.TryGetValue(id, out ConstructionData currentConstruction))
            {
                ue.Debug.LogWarning($"ID {id} no encontrado en constructions.");
                return;
            }
            (bool isBuried, string toDestroyName, int idToDestroy, List<int2> needActivate) = currentConstruction.IsBuried(sandElev, terrainElev, realTerrain, constructionGrid);
            if (isBuried) { ue.Debug.Log($"Construcción {toDestroyName} enterrada. No utilizable."); }

            foreach (var cell in needActivate)
            {
                ActivateCell(cell.x, cell.y);
            }

            DeleteBuild(idToDestroy);
        }

        public void DeleteBuild(int id)
        {
            if (!constructions.TryGetValue(id, out ConstructionData data))
            {
                ue.Debug.LogWarning($"ID {id} no encontrado al intentar eliminar construcción.");
                return;
            }

            if (!data.isBuried) return;

            if (data.obj != null)
            {
                UnityEngine.Object.Destroy(data.obj);
            }

            constructions.Remove(id);
        }
        #endregion
    }
}