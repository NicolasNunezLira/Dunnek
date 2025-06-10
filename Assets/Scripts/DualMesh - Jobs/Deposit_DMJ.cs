using System;
using Unity.Collections;


namespace DunefieldModel_DualMeshJobs
{
    public partial class ModelDMJ
    {
        public static void DepositGrain(
            int x, int z,
            int dx, int dz,
            float depositeHeight,
            NativeArray<float> terrain,
            NativeArray<float> sand,
            NativeArray<float> shadow,
            int xResolution,
            int zResolution,
            float slope,
            float shadowSlope,
            float avalancheSlope,
            bool openEnded,
            int iter,
            ref FixedList32Bytes<SandChanges> sandOut
        )
        {
            /// <summary>
            /// Deposita un grano de arena en la posición (x, z) considerando viento con dirección (dx, dz).
            /// </summary>
            /// <param name="x">Componente x de la posición donde se intentará depositar el grano.</param>
            /// <param name="z">Componente z de la posición donde se intentará depositar el grano.</param>
            /// <param name="dx">Componente x de la dirección del viento.</param>
            /// <param name="dz">Componente z de la dirección del viento.</param>
            /// <param name="depositeHeight">Altura de deposición del grano.</param>

            // Buscar el punto más bajo en la dirección del viento
            while (true)
            {
                FindSlope.SlopeResult result = FindSlope.Downslope(
                    x, z, dx, dz, sand, terrain, xResolution, slope, openEnded);

                if (!result.isValid) break;

                if (openEnded &&
                    ((result.X == xResolution - 1 && result.X == 0) ||
                    (result.X == 0 && result.X == xResolution - 1) ||
                    (result.Z == zResolution - 1 && result.Z == 0) ||
                    (result.Z == 0 && result.Z == zResolution - 1)))
                    break;

                x = result.X;
                z = result.Z;
            }

            int index = x + (xResolution * z);


            if (terrain[index] >= sand[index])
            {
                // Si el terreno es más alto que la arena más la altura de deposición, depositar encima del terreno
                //sand[index] = terrain[index] + depositeHeight;
                sandOut.Add(new SandChanges { index = index, delta = terrain[index] - sand[index] + depositeHeight });
            }
            else
            {
                //sand[index] += depositeHeight;
                sandOut.Add(new SandChanges { index = index, delta = depositeHeight });
            }


            Avalanche(
                x, z,
                sand, terrain,
                xResolution, zResolution,
                avalancheSlope,
                openEnded, iter,
                ref sandOut);

            /*
            shadow = UpdateShadow(
                x, z,
                dx, dz,
                sand, terrain, shadow,
                xResolution, zResolution,
                shadowSlope,
                openEnded);
            */

            return;
        }

    }
}
