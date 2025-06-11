using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DunefieldModel_DualMesh
{
    public partial class ModelDM
    {
        public void AvalancheInit()
        {
            StopAvalancheProcessing();

            for (int x = 1; x < sandElev.GetLength(0) - 1; x++)
            {
                for (int z = 1; z < sandElev.GetLength(1) - 1; z++)
                {
                    if (NeedAvalancheProcessing(x, z))
                    {
                        EnqueueCellForAvalanche(x, z);
                    }
                }
            }

            if (avalancheCellsQueue.Count > 0)
            {
                avalancheCoroutine = StartCourutine(ProcessAvalancheDistributed());
            }

            if (enableDebugMode)
            {
                Debug.Log($"Avalancha iniciada con {avalancheCellsQueue.Count} celdas activas");
            }
        }


        public virtual void Avalanche(int x, int z, int iter = 3)
        {
            StartCoroutine(ProcessSingleCellAvalanche(x, z, iter));
        }

        private IEnumerator ProcessSingleCellAvalanche(int x, int z, int originalIter)
        {
            int iter = originalIter;

            while (iter-- > 0)
            {
                if (terrainElev[x, z] >= sandElev[x, z])
                {
                    yield break;
                }

                int xAvalanche = -1;
                int zAvalanche = -1;

                while (FindSlope.AvalancheSlope(x, z, out int xLow, out int zLow, avalancheSlope) >= 2)
                {
                    if (openEnded && IsOpenBoundaryConflict(x, z, xLow, zLow))
                        break;

                    xAvalanche = xLow;
                    zAvalanche = zLow;
                }

                if (xAvalanche < 0 || zAvalanche < 0)
                {
                    break;
                }
                else
                {
                    ApplyOriginalAvalancheTransfer(x, z, xAvalanche, zAvalanche);
                    x = xAvalanche;
                    z = zAvalanche;

                    MarkAreaForFutureProcessing(x, z, 2);
                }

                if (iter % 2 == 0)
                    yield return null;
            }
        }

        private IEnumerator ProcessAvalancheDistributed()
        {
            isProcessingAvalanche = true;
            int processedThisFrame = 0;

            while (avalancheCellsQueue.Count > 0)
            {
                Vector2Int currentCell = avalancheCellsQueue.Dequeue();
                activeCells.Remove(currentCell);

                List<Vector2Int> newActiveCells = ProcessConicAvalanche(currentCell.x, currentCell.y);

                foreach (var newCell in newActiveCells)
                {
                    EnqueueCellForAvalanche(newCell.x, newCell.y);
                }

                processedThisFrame++;

                if (processedThisFrame >= maxCellsPerFrame)
                {
                    processedThisFrame = 0;
                    yield return null;
                }
            }

            isProcessingAvalanche = false;
            avalancheCoroutine = null;

            if (enableDebugMode)
            {
                Debug.Log("Procesamiento de avalanchas completado.");
            }
        }

        private List<Vector2Int> ProcessConicAvalanche(int x, int z)
        {
            List<Vector2Int> activatedCells = new List<Vector2Int>();

            if (terrainElev[x, z] >= sandElev[x, z])
                return activatedCells;

            List<AvalancheTarget> targets = FindConicAvalancheTargets(x, z);

            if (targets.Count > 0)
            {
                DistributeSandConically(x, z, targets, activatedCells);
            }

            return activatedCells;
        }

        private List<AvalancheTarget> FindConicAvalancheTargets(int x, int z)
        {
            List<AvalancheTarget> targets = new List<AvalancheTarget>();
            float currentHeight = sandElev[x, z];

            int[] dx = { -1, -1, -1, 0, 0, 1, 1, 1 };
            int[] dz = { -1, 0, 1, -1, 1, -1, 0, 1 };
            float[] distances = { 1.414f, 1f, 1.414f, 1f, 1f, 1.414f, 1f, 1.414f };

            for (int dir = 0; dir < 8; dir++)
            {
                int nx = x + dx[dir];
                int nz = z + dz[dir];

                if (!IsValidCell(nx, nz)) continue;

                if (openEnded && IsOpenBoundaryConflict(x, z, nx, nz)) continue;

                float neighborHeight = Math.Max(sandElev[nx, nz], terrainElev[nx, nz]);
                float heightDiff = currentHeight - neighborHeight;
                float slope = heightDiff / distances[dir];

                if (slope >= avalancheSlope && heightDiff > minAvalancheAmount)
                {
                    targets.Add(new AvalancheTarget
                    {
                        x = nx,
                        z = nz,
                        heightDiff = heightDiff,
                        slope = slope,
                        distance = distances[dir],
                        priority = slope * (heightDiff / distances[dir])
                    });
                }
            }

            targets.Sort((a, b) => b.priority.CompareTo(a.priority));
            return targets;
        }

        private void DistributeSandConically(int sourceX, int sourceZ, List<AvalancheTarget> targets, List<Vector2Int> activatedCells)
        {
            float availableSand = sandElev[sourceX, sourceZ] - terrainElev[sourceX, sourceZ];
            if (availableSand <= minAvalancheAmount) return;

            float totalWeight = 0f;
            foreach (var target in targets)
            {
                float conicWeight = target.priority * (1f + conicShapeFactor / target.distance);
                target.weight = conicWeight;
                totalWeight += conicWeight;
            }

            if (totalWeight <= 0) return;

            float maxTransfer = availableSand * avalancheTrasnferRate;

            foreach (var target in targets)
            {
                float proportion = target.weight / totalWeight;
                float transferAmount = maxTransfer * proportion;

                transferAmount = Math.Min(transferAmount, target.heightDiff * .5f);

                if (transferAmount > minAvalancheAmount)
                {
                    sandElev[sourceX, sourceZ] -= transferAmount;

                    float finalTransfer = transferAmount;
                    if (sandElev[target.x, target.z] <= terrainElev[target.x, target.z])
                    {
                        finalTransfer = Math.Min(finalTransfer,
                            terrainElev[target.x, target.z] - sandElev[target.x, target.z] + transferAmount);

                        sandElev[target.x, target.z] += finalTransfer;
                        activatedCells.Add(new Vector2Int(target.x, target.z));
                    }
                }
            }
        }

        private void ApplyOriginalAvalancheTransfer(int x, int z, int xAvalanche, int zAvalanche)
        {
            float diff = Math.Abs(Math.Max(sandElev[xAvalanche, zAvalanche], terrainElev[xAvalanche, zAvalanche])
                                - sandElev[x, z]) / 2f;

            sandElev[x, z] -= diff;

            float targetTransfer = (sandElev[xAvalanche, zAvalanche] > terrainElev[xAvalanche, zAvalanche]) ?
                0 : terrainElev[xAvalanche, zAvalanche] - sandElev[xAvalanche, zAvalanche] + diff;

            sandElev[xAvalanche, zAvalanche] += targetTransfer;
        }

        #region Métodos de Utilidad

        private bool NeedAvalancheProcessing(int x, int z)
        {
            return sandElev[x, z] > terrainElev[x, z] + minAvalancheAmount;
        }

        private void EnqueueCellForAvalanche(int x, int z)
        {
            Vector2Int cell = new Vector2Int(x, z);
            if (!activeCells.Contains(cell) && IsValidCell(x, z))
            {
                avalancheCellsQueue.Enqueue(cell);
                activeCells.Add(cell);
            }
        }

        private bool IsValidCell(int x, int z)
        {
            return x > 0 && x < sandElev.GetLength(0) - 1 &&
                z > 0 && z < sandElev.GetLength(1) - 1;
        }

        private bool IsOpenBoundaryConflict(int x, int z, int targetX, int targetZ)
        {
            return (targetX == xDOF && x == 0) || (targetX == 0 && x == xDOF) ||
                (targetZ == zDOF && z == 0) || (targetZ == 0 && z == zDOF);
        }

        private void MarkAreaForFutureProcessing(int centerX, int centerZ, int radius)
        {
            for (int x = Math.Max(1, centerX - radius);
                x <= Math.Min(sandElev.GetLength(0) - 2, centerX + radius); x++)
            {
                for (int z = Math.Max(1, centerZ - radius);
                    z <= Math.Min(sandElev.GetLength(1) - 2, centerZ + radius); z++)
                {
                    if (NeedAvalancheProcessing(x, z))
                    {
                        EnqueueCellForAvalanche(x, z);
                    }
                }
            }
        }

        public void StopAvalancheProcessing()
        {
            if (avalancheCoroutine != null)
            {
                StopCoroutine(avalancheCoroutine);
                avalancheCoroutine = null;
            }

            isProcessingAvalanche = false;
            avalancheCellsQueue.Clear();
            activeCells.Clear();
        }

        public AvalancheSystemStatus GetAvalancheStatus()
        {
            return new AvalancheSystemStatus
            {
                isProcessing = isProcessingAvalanche,
                queueSize = avalancheCellsQueue.Count,
                activeCellsCount = activeCells.Count,
                avalancheSlope = avalancheSlope,
                transferRate = avalancheTransferRate,
                conicFactor = conicShapeFactor
            };
        }

        #endregion
    }

    /// <summary>
    /// Estructura para objetivos de avalancha cónica
    /// </summary>
    [System.Serializable]
    public class AvalancheTarget
    {
        public int x, z;
        public float heightDiff;
        public float slope;
        public float distance;
        public float priority;
        public float weight;
    }

    /// <summary>
    /// Estado del sistema de avalanchas
    /// </summary>
    [System.Serializable]
    public struct AvalancheSystemStatus
    {
        public bool isProcessing;
        public int queueSize;
        public int activeCellsCount;
        public float avalancheSlope;
        public float transferRate;
        public float conicFactor;
    }
}
