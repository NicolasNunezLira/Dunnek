using System;
using ue = UnityEngine;

namespace DunefieldModel_DualMeshJobs
{   
    public partial class ModelDMJ
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
                for (int x = 0; x < xDOF + 1; x++)
                {
                    for (int z = 0; z < zDOF + 1; z++)
                    {
                        int i = x + (xDOF + 1) * z;
                        if ((sandElev[i] - terrainElev[i]) > 0) count1++;
                        if (shadow[i] <= 0) count2++;
                        if ((sandElev[i] - terrainElev[i] > 0) && shadow[i] <= 0) count3++;
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
                int index = x + (xResolution * z);

                if (Math.Max(sandElev[index], terrainElev[index]) <= 0) // Si no hay arena o terreno, saltar
                {
                    if (verbose) { ue.Debug.Log("Grano (" + x + "," + z + ") sin altura."); }
                    ;
                    continue;
                }
                if (shadow[index] > 0 || terrainElev[index] >= sandElev[index]) // Si el grano está en sombra o no hay arena sobre el terreno, saltar
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
            int index = xCurr + (xResolution * zCurr);

            // Conteo de celdas de terreno en las posibles deposiciones del grano
            int countTerrain = 0;
            for (int j = 1; j <= i; j++)
            {
                (int xAUx, int zAux) = WrapCoords(xCurr + j * dx, zCurr + j * dz);
                if (xAUx != xCurr || zAux != zCurr)
                {
                    index = xAUx + (xResolution * zAux);
                }
                if (terrainElev[index] >= sandElev[index])
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
                if (openEnded && (xCurr < 0 || xCurr >= xResolution || zCurr < 0 || zCurr >= zResolution))
                    break;

                // Si el grano está en sombra, depositar y salir del ciclo
                if (shadow[indexCurr] > 0 && sandElev[indexCurr] > terrainElev[indexCurr])
                {
                    if (verbose) { ue.Debug.Log("Grano a depositar en (" + xCurr + "," + zCurr + ")."); }
                    DepositGrain(xCurr, zCurr, dx, dz, depositeH);
                    break;
                }


                if (terrainElev[indexCurr] >= sandElev[indexCurr] &&
                    terrainElev[indexCurr] >= sandElev[index])
                {
                    countTerrain -= (terrainElev[indexCurr] >= sandElev[indexCurr]) ? 1 : 0;
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
                        int lateralIndex = lx + (xResolution * lz);

                        if (Math.Max(terrainElev[lateralIndex], sandElev[lateralIndex]) < Math.Max(terrainElev[indexCurr], sandElev[indexCurr]) - slopeThreshold)
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
                countTerrain -= (terrainElev[indexCurr] >= sandElev[indexCurr]) ? 1 : 0;

                // Si el grano no está en sombra, verificar si se debe depositar
                if (--i <= 0)
                {// Si el terreno es más alto que la arena, reiniciar posición


                    // Verificar si el grano debe depositarse basado en la altura de arena y terreno
                    if (rnd.NextDouble() < (sandElev[indexCurr] > terrainElev[indexCurr] ? pSand : pNoSand))
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