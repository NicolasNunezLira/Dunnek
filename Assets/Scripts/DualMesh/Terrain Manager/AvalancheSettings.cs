using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Data;

namespace DunefieldModel_DualMesh
{
    public partial class ModelDM
    {
        #region Variables
        // Cola de celdas activas
        public Queue<Vector2Int> avalancheQueue;
        private HashSet<Vector2Int> inQueue;

        //private int avalancheChecksPerFrame = 500; // o ajustable públicamente
        #endregion

        #region IsValidCell
        private bool IsValidCell(int x, int z)
        {
            return openEnded ? IsInside(x, z) : (x > 0 && x < sand.Width - 1 && z > 0 && z < sand.Height - 1);
        }
        #endregion


        #region Initialize Queues
        /// <summary>
        /// Inicializa la cola reactiva de avalanchas
        /// </summary>
        public void InitAvalancheQueue()
        {
            if (avalancheQueue == null) avalancheQueue = new Queue<Vector2Int>();
            if (inQueue == null) inQueue = new HashSet<Vector2Int>();

            int width = sand.Width;
            int height = sand.Height;

            for (int x = 1; x < width - 1; x++)
            {
                for (int z = 1; z < height - 1; z++)
                {
                    if (CellIsCritical(x, z))
                    {
                        ActivateCell(x, z);
                    }
                }
            }
        }

        public void InitCriticalSlopeCells()
        {
            if (avalancheQueue == null) avalancheQueue = new Queue<Vector2Int>();
            if (inQueue == null) inQueue = new HashSet<Vector2Int>();

            List<(Vector2Int pos, float slope)> critical = new();

            int width = sand.Width;
            int height = sand.Height;

            for (int x = 1; x < width - 1; x++)
            {
                for (int z = 1; z < height - 1; z++)
                {
                    float maxSlope = GetMaxSlopeAt(x, z);
                    if (maxSlope > 2.5 * avalancheSlope)
                    {
                        critical.Add((new Vector2Int(x, z), maxSlope));
                    }
                }
            }

            // Ordenar por pendiente de mayor a menor
            critical.Sort((a, b) => b.slope.CompareTo(a.slope));

            foreach (var (pos, _) in critical)
            {
                if (!inQueue.Contains(pos))
                {
                    avalancheQueue.Enqueue(pos);
                    inQueue.Add(pos);
                }
            }
        }

        private bool CellIsCritical(int x, int z)
        {
            float h = sand[x, z];
            float b = terrainShadow[x, z];

            if (h <= b + minAvalancheAmount) return false;

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    if (dx == 0 && dz == 0) continue;

                    int nx = x + dx;
                    int nz = z + dz;

                    if (!IsValidCell(nx, nz)) continue;

                    float nh = Math.Max(sand[nx, nz], terrainShadow[nx, nz]);
                    float heightDiff = h - nh;
                    float distance = size * Mathf.Sqrt(
                        (float)dx * dx / (xResolution * xResolution) +
                        (float)dz * dz / (zResolution * zResolution));

                    float slope = heightDiff / distance;
                    if (slope > avalancheSlope)
                        return true;
                }
            }

            return false;
        }
        #endregion

        #region ActivateCells
        /// <summary>
        /// Activa una celda para evaluación de avalancha
        /// </summary>
        public void ActivateCell(int x, int z)
        {
            Vector2Int cell = new Vector2Int(x, z);
            if (!inQueue.Contains(cell))
            {
                avalancheQueue.Enqueue(cell);
                inQueue.Add(cell);
            }
        }
        #endregion


        #region Run Avalanche
        public int RunAvalancheBurst(int maxStepsPerCall = 50, bool verbose = false)
        {
            if (avalancheQueue == null || avalancheQueue.Count == 0)
                return 0;

            Stack<Vector2Int> localStack = new Stack<Vector2Int>();

            var root = avalancheQueue.Dequeue();
            inQueue.Remove(root);
            localStack.Push(root);

            int steps = 0;

            while (localStack.Count > 0 && steps < maxStepsPerCall)
            {
                var cell = localStack.Pop();
                steps++;

                int x = cell.x;
                int z = cell.y;

                float h = sand[x, z];
                float b = terrainShadow[x, z];
                float available = h - b;
                if (available <= minAvalancheAmount) continue;

                List<(int dx, int dz, float priority)> neighbors = new();

                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dz = -1; dz <= 1; dz++)
                    {
                        if (dx == 0 && dz == 0) continue;

                        int nx = x + dx;
                        int nz = z + dz;

                        if (!IsValidCell(nx, nz)) continue;

                        float nh = Math.Max(sand[nx, nz], terrainShadow[nx, nz]);
                        float heightDiff = h - nh;
                        float distance = size * Mathf.Sqrt(dx * dx / (xResolution * xResolution) + dz * dz / (zResolution * zResolution));
                        float slope = heightDiff / distance;

                        if (slope > avalancheSlope && heightDiff > minAvalancheAmount)
                        {
                            float priority = slope * heightDiff / Mathf.Pow(distance, conicShapeFactor);
                            neighbors.Add((dx, dz, priority));
                        }
                    }
                }

                if (neighbors.Count == 0) continue;

                float totalWeight = neighbors.Sum(n => n.priority);

                float maxTransfer = available * avalancheTrasnferRate;

                foreach (var (dx, dz, priority) in neighbors)
                {
                    int nx = x + dx;
                    int nz = z + dz;

                    if (!IsValidCell(nx, nz)) continue;

                    float proportion = priority / totalWeight;
                    float transfer = maxTransfer * proportion;

                    float maxDiff = (sand[x, z] - Math.Max(sand[nx, nz], terrainShadow[nx, nz])) * 0.5f;
                    transfer = Mathf.Min(transfer, maxDiff);

                    if (transfer > minAvalancheAmount)
                    {
                        sand[x, z] -= transfer;
                        sandChanges.AddChanges(x, z);
                        sand[nx, nz] = Math.Max(sand[nx, nz], terrainShadow[nx, nz]) + transfer;
                        sandChanges.AddChanges(nx, nz);

                        if (constructionGrid[nx, nz] > 0)
                        {
                            TryToDeleteBuild(nx, nz);
                        }

                        Vector2Int neighbor = new Vector2Int(nx, nz);
                        if (!inQueue.Contains(neighbor))
                        {
                            avalancheQueue.Enqueue(neighbor);
                            inQueue.Add(neighbor);
                        }

                        localStack.Push(neighbor); // 💡 propagación rápida en bloque
                    }
                }

                // Reinsertar el colapsador si sigue inestable
                float slopeNow = GetMaxSlopeAt(x, z);
                if (slopeNow > avalancheSlope)
                    localStack.Push(cell);

                if (steps % 40 == 0)
                    localStack.Reverse();
            }

            return avalancheQueue.Count;
        }
        #endregion

        #region Max Slope
        private float GetMaxSlopeAt(int x, int z)
        {
            float h = sand[x, z];
            float b = terrainShadow[x, z];
            if (h <= b + minAvalancheAmount) return 0f;

            float maxSlope = 0f;
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    if (dx == 0 && dz == 0) continue;

                    int nx = x + dx;
                    int nz = z + dz;

                    if (!IsValidCell(nx, nz)) continue;

                    float nh = Math.Max(sand[nx, nz], terrainShadow[nx, nz]);
                    float heightDiff = h - nh;
                    float distance = size * Mathf.Sqrt(
                        (float)dx * dx / (xResolution * xResolution) +
                        (float)dz * dz / (zResolution * zResolution));

                    float slope = heightDiff / distance;
                    if (slope > maxSlope)
                        maxSlope = slope;
                }
            }
            return maxSlope;
        }
        #endregion
    } 
}
