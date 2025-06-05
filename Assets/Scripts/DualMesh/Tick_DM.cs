using System;
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


                // Deposición del grano
                int i = HopLength;
                int xCurr = x;
                int zCurr = z;

                while (true)
                {
                    // Cálculo de la posición actual del grano considerando comportamiento toroidal
                    xCurr = (xCurr + dx + xResolution) % xResolution;
                    zCurr = (zCurr + dz + zResolution) % zResolution;

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

                    // Si el grano no está en sombra, verificar si se debe depositar
                    if (--i <= 0)
                    {
                        // Verificar si el grano debe depositarse basado en la altura de arena y terreno
                        if (rnd.NextDouble() < (sandElev[xCurr, zCurr] > terrainElev[xCurr, zCurr] ? pSand : pNoSand))
                        {
                            if (sandElev[xCurr, zCurr] <= terrainElev[xCurr, zCurr] &&
                                terrainElev[xCurr, zCurr] >= sandElev[x, z])
                                continue;

                            
                            DepositGrain(xCurr, zCurr, dx, dz, depositeH);
                            if (verbose) { ue.Debug.Log("Grano a depositar en (" + xCurr + "," + zCurr + ")."); }
                            break;
                        }
                        i = HopLength;
                    }
                }
            }
            ue.Debug.Log("Granos erosionados en este tick:" + count + "/" + grainsPerStep);

            // Actualizar las sombras después de la deposición
            ShadowCheck(false, dx, dz);
        }
        #endregion

    }
}