using UnityEngine;

namespace DunefieldModel_DualMesh
{
    public partial class ModelDM
    {
        public void InjectDirectionalSand(int dx, int dz, float inflowAmount, int attempts, float pDepositDirect = 0.5f)
        {
            int width = sandElev.GetLength(0);
            int height = sandElev.GetLength(1);

            if (!openEnded) return;

            float total = Mathf.Abs(dx) + Mathf.Abs(dz);
            float px = (total > 0) ? Mathf.Abs(dx) / total : 0f;

            for (int i = 0; i < attempts; i++)
            {
                float r = Random.value;
                int x = 0, z = 0;

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
                if (sandElev[x, z] <= terrainElev[x, z] + inflowAmount * 0.5f)
                {
                    sandElev[x, z] = terrainElev[x, z] + inflowAmount;
                }
                else
                {
                    sandElev[x, z] += inflowAmount;
                }

                ActivateCell(x, z);
                UpdateShadow(x, z, dx, dz);

                // Inyectar grano directamente al algoritmo de deposición
                if (Random.value <= pDepositDirect) { AlgorithmDeposit(x, z, dx, dz, inflowAmount, verbose: false); };
            }
        }

    }
}
