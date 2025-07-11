/*using System.Net.Sockets;
using UnityEngine;

namespace DunefieldModel_DualMesh
{
    public partial class ModelDM
    {
        public void InjectDirectionalSand(int dx, int dz, float inflowAmount, int attempts, float pDepositDirect = 0.5f)
        {
            int width = sand.Width;
            int height = sand.Height;

            float total = Mathf.Abs(dx) + Mathf.Abs(dz);
            float px = (total > 0) ? Mathf.Abs(dx) / total : 0f;

            int count = 0;
            for (int i = 0; i < attempts; i++, count++)
            {
                float r = Random.value;
                int x, z;
                if (r < px)
                {
                    // Entrada por borde lateral (x)
                    z = Random.Range(0, height);
                    x = (dx > 0) ? 0 : (width - 1);
                }
                else
                {
                    // Entrada por borde vertical (z)
                    x = Random.Range(0, width);
                    z = (dz > 0) ? 0 : (height - 1);
                }

                // Agregar arena al borde
                if (sand[x, z] <= terrainShadow[x, z] + inflowAmount * 0.5f)
                {
                    sand[x, z] = terrainShadow[x, z] + inflowAmount;
                }
                else
                {
                    sand[x, z] += inflowAmount;
                }

                ActivateCell(x, z);
                UpdateShadow(x, z, dx, dz);

                // Inyectar grano directamente al algoritmo de deposición
                if (Random.value <= pDepositDirect) { AlgorithmDeposit(x, z, dx, dz, inflowAmount); }
                ;
            }
            Debug.Log($"Inyectados {count} granos de arena en dirección ({dx}, {dz}) con cantidad {inflowAmount}.");
        }

    }
}

*/
