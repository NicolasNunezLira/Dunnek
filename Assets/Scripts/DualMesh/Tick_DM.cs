using System;
using TMPro;
using UnityEngine.Rendering;
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

                if (Math.Max(sandElev[x, z], terrainElev[x, z]) <= 0) // Si no hay arena o terreno, saltar
                {
                    if (verbose) { ue.Debug.Log("Grano (" + x + "," + z + ") sin altura."); }
                    ;
                    continue;
                }
                if (Shadow[x, z] > 0 || terrainElev[x, z] >= sandElev[x, z]) // Si el grano está en sombra o no hay arena sobre el terreno, saltar
                {
                    if (verbose) { ue.Debug.Log("Grano (" + x + "," + z + ") en sombra o terreno elevado."); }
                    ;
                    continue;
                }


                // Erosión del grano
                if (verbose) { ue.Debug.Log("Grano a erosionar en (" + x + "," + z + ")."); }
                depositeH = ErodeGrain(x, z, dx, dz, erosionHeight);

                count++;

                algorithmDeposit(x, z, dx, dz, depositeH, verbose);

                /*
                // Deposición del grano
                int i = HopLength;
                int xCurr = x;
                int zCurr = z;

                // Conteo de celdas de terreno en las posibles deposiciones del grano
                int countTerrain = 0;
                for (int j = 1; j <= i; j++)
                {
                    (int xAUx, int zAux) = WrapCoords(xCurr + j * dx, zCurr + j * dz);
                    if (terrainElev[xAUx, zAux] >= sandElev[xAUx, zAux])
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

                    // Si el grano sale del dominio en campo abierto, detener la deposición
                    if (openEnded && (xCurr < 0 || xCurr >= xResolution || zCurr < 0 || zCurr >= zResolution))
                        break;

                    // Si el grano está en sombra, depositar y salir del ciclo
                    if (Shadow[xCurr, zCurr] > 0 && sandElev[xCurr, zCurr] > terrainElev[xCurr, zCurr])
                    {
                        if (verbose) { ue.Debug.Log("Grano a depositar en (" + xCurr + "," + zCurr + ")."); }
                        DepositGrain(xCurr, zCurr, dx, dz, depositeH);
                        break;
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

                        for (int j = 0; j < 2; j++)
                        {
                            int lx = (xCurr + dxLateral[j] + xResolution) % xResolution;
                            int lz = (zCurr + dzLateral[j] + zResolution) % zResolution;

                            if (Math.Max(terrainElev[lx, lz], sandElev[lx, lz]) < Math.Max(terrainElev[xCurr, zCurr], sandElev[xCurr, zCurr]) - slopeThreshold)
                            {
                                DepositGrain(lx, lz, dxLateral[j], dzLateral[j], depositeH);
                                if (verbose) ue.Debug.Log($"Grano redirigido lateralmente a ({lx}, {lz})");
                                break;
                            }
                        }

                        DepositGrain(xCurr, zCurr, dx, dz, depositeH);
                        if (verbose) { ue.Debug.Log("Grano a depositar en (" + xCurr + "," + zCurr + ")."); }
                        break;
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
                            break;
                        }
                        i = HopLength;
                    }

                    // 
                }

                float maxSlopeThreshold = .5f;     // Máxima pendiente frontal admisible
                float lateralThreshold = 0.2f;      // Diferencia mínima para fluir lateralmente

                int i = HopLength;
                int xCurr = x;
                int zCurr = z;

                while (true)
                {
                    // Movimiento en dirección del viento con condiciones toroidales
                    xCurr = (xCurr + dx + xResolution) % xResolution;
                    zCurr = (zCurr + dz + zResolution) % zResolution;

                    if (openEnded && (xCurr < 0 || xCurr >= xResolution || zCurr < 0 || zCurr >= zResolution))
                        break;

                    // Altura total en la celda actual y la próxima
                    float currentHeight = Math.Max(terrainElev[x, z], sandElev[x, z]);
                    float nextHeight = Math.Max(terrainElev[xCurr, zCurr], sandElev[xCurr, zCurr]);
                    float slope = terrainElev[xCurr, zCurr] - currentHeight;

                    // Si el terreno al frente es muy empinado, buscar flujo lateral
                    if (slope > maxSlopeThreshold)
                    {
                        bool deposited = false;

                        // Direcciones laterales (perpendiculares al viento)
                        int[] dxLateral = { -dz, dz };
                        int[] dzLateral = { dx, -dx };

                        for (int j = 0; j < 2; j++)
                        {
                            int lx = (xCurr + dxLateral[j] + xResolution) % xResolution;
                            int lz = (zCurr + dzLateral[j] + zResolution) % zResolution;

                            float lateralHeight = Math.Max(terrainElev[lx, lz], sandElev[lx, lz]);

                            if (lateralHeight < currentHeight - lateralThreshold)
                            {
                                DepositGrain(lx, lz, dxLateral[j], dzLateral[j], depositeH);
                                if (verbose) ue.Debug.Log($"Grano redirigido lateralmente a ({lx}, {lz})");
                                deposited = true;
                                break;
                            }
                        }

                        if (deposited) break;
                        else
                        {
                            // No hay flujo posible, depositar justo antes
                            DepositGrain((xCurr - dx + xResolution) % xResolution, (zCurr - dz + zResolution) % zResolution, dx, dz, depositeH);
                            if (verbose) ue.Debug.Log($"Grano acumulado antes del obstáculo en ({xCurr - dx}, {zCurr - dz})");
                            break;
                        }
                    }

                    // Si hay sombra y arena suficiente, depositar
                    if (Shadow[xCurr, zCurr] > 0 && sandElev[xCurr, zCurr] > terrainElev[xCurr, zCurr])
                    {
                        DepositGrain(xCurr, zCurr, dx, dz, depositeH);
                        if (verbose) ue.Debug.Log($"Grano depositado en sombra en ({xCurr}, {zCurr})");
                        break;
                    }

                    // Depósito aleatorio tras varios saltos
                    if (--i <= 0)
                    {
                        double p = sandElev[xCurr, zCurr] > terrainElev[xCurr, zCurr] ? pSand : pNoSand;
                        if (rnd.NextDouble() < p)
                        {
                            DepositGrain(xCurr, zCurr, dx, dz, depositeH);
                            if (verbose) ue.Debug.Log($"Grano depositado aleatoriamente en ({xCurr}, {zCurr})");
                            break;
                        }
                        i = HopLength;
                    }
                }
                */


                ue.Debug.Log("Granos erosionados en este tick:" + count + "/" + grainsPerStep);

                // Actualizar las sombras después de la deposición
                //ShadowCheck(false, dx, dz);




            }
        }

        public void algorithmDeposit(int x, int z , int dx, int dz, float depositeH, bool verbose = false)
        {
        // Deposición del grano
            int i = HopLength;
            int xCurr = x;
            int zCurr = z;

            // Conteo de celdas de terreno en las posibles deposiciones del grano
            int countTerrain = 0;
            for (int j = 1; j <= i; j++)
            {
                (int xAUx, int zAux) = WrapCoords(xCurr + j * dx, zCurr + j * dz);
                if (terrainElev[xAUx, zAux] >= sandElev[xAUx, zAux])
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

                // Si el grano sale del dominio en campo abierto, detener la deposición
                if (openEnded && (xCurr < 0 || xCurr >= xResolution || zCurr < 0 || zCurr >= zResolution))
                    break;

                // Si el grano está en sombra, depositar y salir del ciclo
                if (Shadow[xCurr, zCurr] > 0 && sandElev[xCurr, zCurr] > terrainElev[xCurr, zCurr])
                {
                    if (verbose) { ue.Debug.Log("Grano a depositar en (" + xCurr + "," + zCurr + ")."); }
                    DepositGrain(xCurr, zCurr, dx, dz, depositeH);
                    break;
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

                    for (int j = 0; j < 2; j++)
                    {
                        int k = 1;
                        while (k <= i)
                        {
                            int lx = (xCurr + dxLateral[j] * k + xResolution) % xResolution;
                            int lz = (zCurr + dzLateral[j] * k + zResolution) % zResolution;

                            if (Math.Max(terrainElev[lx, lz], sandElev[lx, lz]) < Math.Max(terrainElev[xCurr, zCurr], sandElev[xCurr, zCurr]) - slopeThreshold)
                            {
                                DepositGrain(lx, lz, dxLateral[j], dzLateral[j], depositeH);
                                if (verbose) ue.Debug.Log($"Grano redirigido lateralmente a ({lx}, {lz})");
                                break;
                            }
                            k++;
                        }
                    }

                    DepositGrain(xCurr, zCurr, dx, dz, depositeH);
                    if (verbose) { ue.Debug.Log("Grano a depositar en (" + xCurr + "," + zCurr + ")."); }
                    break;
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
                        break;
                    }
                    i = HopLength;
                }

                // 
            }
        }
        
        #endregion
    }
}