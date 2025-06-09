using System;
using System.Linq;
using ue = UnityEngine;

namespace DunefieldModel_DualMesh
{
    public partial class ModelDM
    {
        #region Avalanche
        public void AvalancheInit()
        {
            for (int x = 0; x < sandElev.GetLength(0); x++)
            {
                for (int z = 0; z < sandElev.GetLength(1); z++)
                {
                    Avalanche(x, z);
                }
            }
        }

        public virtual void Avalanche(int x, int z, int iter = 3)
        {
            /// <summary>
            /// Simula la avalancha alrededor de la posición (x, z).
            /// </summary>
            /// <param name="x">Coordenada X de la posición.</param>
            /// <param name="z">Coordenada Z de la posición.</param>

            while (iter-- > 0)
            {
                if (terrainElev[x, z] >= sandElev[x, z])
                {
                    return;
                }
                int xAvalanche = -1;
                int zAvalanche = -1;
                while (FindSlope.AvalancheSlope(x, z, out int xLow, out int zLow, avalancheSlope) >= 2)
                {
                    if (openEnded &&
                        ((xAvalanche == xDOF && x == 0) || (xAvalanche == 0 && x == xDOF) ||
                        (zAvalanche == zDOF && z == 0) || (zAvalanche == 0 && z == zDOF)))
                        break;
                    xAvalanche = xLow;
                    zAvalanche = zLow;
                }

                if (xAvalanche < 0 || zAvalanche < 0)
                {
                    // No hay pendiente de avalancha, no se erosiona
                    break;
                }
                else
                {
                    float diff = Math.Abs(Math.Max(sandElev[xAvalanche, zAvalanche], terrainElev[xAvalanche, zAvalanche]) - sandElev[x, z]) / 2f;
                    sandElev[x, z] -= diff;
                    sandElev[xAvalanche, zAvalanche] += (sandElev[xAvalanche, zAvalanche] > terrainElev[xAvalanche, zAvalanche]) ? 0 : terrainElev[xAvalanche, zAvalanche] - sandElev[xAvalanche, zAvalanche] + diff;
                    x = xAvalanche;
                    z = zAvalanche;
                }
            }
        }

        public virtual void AvalancheObjects(float criticalSlopeThreshold)
        {
            var keys = criticalSlopes.Keys.ToList();

            foreach (var key in keys)
            {
                int x = key.Item1;
                int z = key.Item2;
                ue.Vector2Int dir = criticalSlopes[key];

                int xn = x + dir.x;
                int zn = z + dir.y;

                float diff = sandElev[x, z] - sandElev[xn, zn];

                if (diff > slopeThreshold)
                {
                    // Mover una pequeña cantidad de arena
                    float amount = diff * 0.25f;

                    sandElev[x, z] -= amount;
                    sandElev[xn, zn] += amount;

                    // Marcar vecinas como críticas si exceden el umbral ahora
                    MarkNeighborsAsCritical(xn, zn, criticalSlopeThreshold);
                }
                else
                {
                    // Ya no está inestable, remover
                    criticalSlopes.Remove(key);
                }
            }
        }

        void MarkNeighborsAsCritical(int x, int z, float criticalSlopeThreshold)
        {
            ue.Vector2Int[] directions = {
                new(1, 0), new(-1, 0), new(0, 1), new(0, -1), new(1, 1), new(-1, -1), new(1, -1), new(-1, 1)
            };

            foreach (var dir in directions)
            {
                int xn = x + dir.x;
                int zn = z + dir.y;

                if (!IsInside(xn, zn)) continue;

                float slope = sandElev[xn, zn] - sandElev[x, z];
                if (slope > criticalSlopeThreshold)
                {
                    criticalSlopes[(xn, zn)] = -dir;
                }
            }
        }
        #endregion
    }
}