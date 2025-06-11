using System;
using Unity.Collections;
using System.Collections.Generic;

namespace DunefieldModel_DualMeshJobs
{
    public partial class ModelDMJ
    {
        #region Avalanche
        /*
        public static NativeArray<float> AvalancheInit(
            NativeArray<float> sand,
            NativeArray<float> terrain,
            int xResolution,
            int zResolution,
            float avalancheSlope,
            bool openEnded = false,
            int iter = 3
        )
        {
            for (int x = 0; x < xResolution; x++)
            {
                for (int z = 0; z < zResolution; z++)
                {
                    sand = Avalanche(
                        x, z,
                        sand, terrain,
                        xResolution, zResolution,
                        avalancheSlope,
                        openEnded, iter);
                }
            }
            return sand;
        }
        */

        public static void Avalanche(
            int x, int z,
            NativeArray<float> sand,
            NativeArray<float> terrain,
            int xResolution,
            int zResolution,
            float avalancheSlope,
            bool openEnded,
            int iter,
            NativeList<SandChanges>.ParallelWriter sandChanges
            )
        {
            /// <summary>
            /// Simula la avalancha alrededor de la posición (x, z).
            /// </summary>
            /// <param name="x">Coordenada X de la posición.</param>
            /// <param name="z">Coordenada Z de la posición.</param>

            int index = x + (xResolution * z);

            while (iter-- > 0)
            {
                if (terrain[index] >= sand[index])
                {
                    return;
                }
                int xAvalanche = -1;
                int zAvalanche = -1;
                while (true)
                {
                    FindSlope.SlopeResult result = FindSlope.AvalancheSlope(x, z, sand, terrain, xResolution, avalancheSlope, openEnded);

                    if (!result.isValid) break;

                    if (openEnded &&
                        ((result.X == xResolution - 1 && x == 0) || (result.X == 0 && x == xResolution - 1) ||
                        (result.Z == zResolution - 1 && z == 0) || (result.Z == 0 && z == zResolution - 1)))
                        break;
                    xAvalanche = result.X;
                    zAvalanche = result.Z;
                }

                if (xAvalanche < 0 || zAvalanche < 0)
                {
                    // No hay pendiente de avalancha, no se erosiona
                    break;
                }
                else
                {
                    int indexAvalanche = xAvalanche + (xResolution * zAvalanche);
                    float diff = Math.Abs(Math.Max(sand[indexAvalanche], terrain[indexAvalanche]) - sand[index]) / 2f;
                    if (index >= 0 && index <= sand.Length)
                    {
                        //sand[index] -= diff;
                        sandChanges.AddNoResize(new SandChanges { index = index, delta = -diff });

                        //sand[indexAvalanche] += ((sand[indexAvalanche] > terrain[indexAvalanche]) ? 0 : terrain[indexAvalanche] - sand[indexAvalanche]) + diff;
                        sandChanges.AddNoResize(new SandChanges
                        {
                            index = indexAvalanche,
                            delta = ((terrain[indexAvalanche] >= sand[indexAvalanche]) ? terrain[indexAvalanche] + diff : sand[indexAvalanche] + diff) - diff
                        });
                    }

                    x = xAvalanche;
                    z = zAvalanche;
                    index = x + (xResolution * z);
                }
            }
            return;
        }
        #endregion
    }
}