using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DunefieldModel_DualMesh
{
    public partial class ModelDM
    {
        private Queue<Vector2Int> criticalCellsQueue;
        private HashSet<Vector2Int> criticalCellsSet;

        // Cola de celdas activas
        public Queue<Vector2Int> avalancheQueue;
        private HashSet<Vector2Int> inQueue;

        //private int avalancheChecksPerFrame = 500; // o ajustable públicamente

        public void RunAvalancheStep()
        {
            int width = sandElev.GetLength(0);
            int height = sandElev.GetLength(1);

            for (int i = 0; i < maxCellsPerFrame; i++)
            {
                int x = rnd.Next(1, width - 1);
                int z = rnd.Next(1, height - 1);

                float currentHeight = sandElev[x, z];
                float baseHeight = terrainElev[x, z];

                if (currentHeight <= baseHeight + minAvalancheAmount)
                    continue;

                List<(int dx, int dz, float priority)> neighbors = new();

                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dz = -1; dz <= 1; dz++)
                    {
                        if (dx == 0 && dz == 0) continue;

                        int nx = x + dx;
                        int nz = z + dz;

                        if (!IsValidCell(nx, nz)) continue;

                        float neighborHeight = Math.Max(sandElev[nx, nz], terrainElev[nx, nz]);
                        float heightDiff = currentHeight - neighborHeight;
                        float distance = Mathf.Sqrt(dx * dx + dz * dz);

                        float slope = heightDiff / distance;

                        if (slope > avalancheSlope && heightDiff > minAvalancheAmount)
                        {
                            //float coneShapeFactor = 1.0f; // puedes exponerlo públicamente
                            float priority = slope * heightDiff / Mathf.Pow(distance, conicShapeFactor);
                            neighbors.Add((dx, dz, priority));
                        }
                    }
                }

                if (neighbors.Count == 0) continue;

                neighbors.Sort((a, b) => b.priority.CompareTo(a.priority));

                float availableSand = currentHeight - baseHeight;
                float totalWeight = neighbors.Sum(n => n.priority);
                float maxTransfer = availableSand * avalancheTrasnferRate;

                foreach (var (dx, dz, priority) in neighbors)
                {
                    int nx = x + dx;
                    int nz = z + dz;

                    float proportion = priority / totalWeight;
                    float transfer = maxTransfer * proportion;

                    float maxDiff = (sandElev[x, z] - Math.Max(sandElev[nx, nz], terrainElev[nx, nz])) * 0.5f;
                    transfer = Mathf.Min(transfer, maxDiff);

                    if (transfer > minAvalancheAmount)
                    {
                        sandElev[x, z] -= transfer;
                        sandElev[nx, nz] += transfer;
                    }
                }
            }
        }

        /*
        public Queue<Vector2Int> avalancheCellsQueue;

        public HashSet<Vector2Int> activeCells;
        public bool isProcessingAvalanche;
        public void AvalancheInit()
        {
            avalancheCellsQueue = new Queue<Vector2Int>();
            activeCells = new HashSet<Vector2Int>();
            isProcessingAvalanche = false;

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

            if (verbose)
            {
                Debug.Log($"Avalancha iniciada con {avalancheCellsQueue.Count} celdas activas");
            }
        }

        public IEnumerator GetAvalancheCoroutine()
        {
            return ProcessAvalancheDistributed();
        }

        public virtual IEnumerator ProcessSingleCellAvalanche(int x, int z, int originalIter = 3)
        {
            int iter = originalIter;

            while (iter-- > 0)
            {
                if (terrainElev[x, z] >= sandElev[x, z])
                    yield break;

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
                    break;

                ApplyOriginalAvalancheTransfer(x, z, xAvalanche, zAvalanche);
                x = xAvalanche;
                z = zAvalanche;

                MarkAreaForFutureProcessing(x, z, 2);

                if (iter % 2 == 0)
                    yield return null;
            }
        }

        private IEnumerator ProcessAvalancheDistributed()
        {
            isProcessingAvalanche = true;
            int processedThisFrame = 0;

            int safetyCounter = 100000;

            while (avalancheCellsQueue.Count > 0 && safetyCounter-- > 0)
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

            if (verbose)
            {
                Debug.Log("Procesamiento de avalanchas completado.");
            }
        }

        // Métodos auxiliares (sin cambios mayores)
        private List<Vector2Int> ProcessConicAvalanche(int x, int z)
        {
            List<Vector2Int> activatedCells = new List<Vector2Int>();

            if (terrainElev[x, z] >= sandElev[x, z])
                return activatedCells;

            List<AvalancheTarget> targets = FindConicAvalancheTargets(x, z);

            if (targets.Count > 0)
                DistributeSandConically(x, z, targets, activatedCells);

            return activatedCells;
        }

        private List<AvalancheTarget> FindConicAvalancheTargets(int x, int z)
        {
            List<AvalancheTarget> targets = new List<AvalancheTarget>();
            float currentHeight = sandElev[x, z];

            int[] dx = { -1, -1, -1, 0, 0, 1, 1, 1 };
            int[] dz = { -1, 0, 1, -1, 1, -1, 0, 1 };
            float diag = (float)Math.Sqrt(Math.Pow(size / xResolution, 2) + Math.Pow(size / zResolution, 2));
            float[] distances = { diag, // (-1, -1)
                size / xResolution,     // (-1,0)
                diag,                   // (-1,1)
                size / zResolution,     // (0, -1)
                size / zResolution,     // (0, 1)
                diag,                   // (1, -1)
                size / xResolution,     // (1, 0)
                diag };                 // (1, 1)

            for (int dir = 0; dir < 8; dir++)
            {
                int nx = x + dx[dir];
                int nz = z + dz[dir];

                if (!IsValidCell(nx, nz)) continue;
                if (openEnded && IsOpenBoundaryConflict(x, z, nx, nz)) continue;

                float neighborHeight = Math.Max(sandElev[nx, nz], terrainElev[nx, nz]);
                float heightDiff = currentHeight - neighborHeight;
                float slope = heightDiff / distances[dir];

                if (slope >= avalancheSlope && heightDiff >  minAvalancheAmount)
                {
                    targets.Add(new AvalancheTarget
                    {
                        x = nx, z = nz, heightDiff = heightDiff,
                        slope = slope, distance = distances[dir],
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
            if (availableSand <= 0) return;

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
                transferAmount = Math.Min(transferAmount, target.heightDiff * 0.5f);

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
        */

        private bool IsValidCell(int x, int z)
        {
            return x > 0 && x < sandElev.GetLength(0) - 1 &&
                   z > 0 && z < sandElev.GetLength(1) - 1;
        }
        /*

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
                        EnqueueCellForAvalanche(x, z);
                }
            }
        }

        public void StopAvalancheProcessing()
        {
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
                transferRate = avalancheTrasnferRate,
                conicFactor = conicShapeFactor
            };
        }
    }
    

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
        */

        public void RunPropagateAvalancheStep()
        {
            int width = sandElev.GetLength(0);
            int height = sandElev.GetLength(1);

            int startX = rnd.Next(1, width - 1);
            int startZ = rnd.Next(1, height - 1);

            //Vector2Int start = SelectMostUnstableCell();
            //int startX = start.x;
            //int startZ = start.y;



            Stack<Vector2Int> toProcess = new Stack<Vector2Int>();
            HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

            toProcess.Push(new Vector2Int(startX, startZ));
            /*
            if (criticalCellsQueue == null || criticalCellsQueue.Count == 0)
                return;

            var current = criticalCellsQueue.Dequeue();
            criticalCellsSet.Remove(current);

            Stack<Vector2Int> toProcess = new Stack<Vector2Int>();
            HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

            toProcess.Push(current);
            */

            while (toProcess.Count > 0)
            {
                var current = toProcess.Pop();
                //current = toProcess.Pop();
                int x = current.x;
                int z = current.y;

                if (visited.Contains(current)) continue;
                visited.Add(current);

                float currentHeight = sandElev[x, z];
                float baseHeight = terrainElev[x, z];
                float availableSand = currentHeight - baseHeight;

                if (availableSand <= minAvalancheAmount)
                    continue;

                List<(int dx, int dz, float priority)> neighbors = new();

                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dz = -1; dz <= 1; dz++)
                    {
                        if (dx == 0 && dz == 0) continue;

                        int nx = x + dx;
                        int nz = z + dz;

                        if (!IsValidCell(nx, nz)) continue;

                        float neighborHeight = Math.Max(sandElev[nx, nz], terrainElev[nx, nz]);
                        float heightDiff = sandElev[x, z] - neighborHeight;
                        float distance = Mathf.Sqrt(dx * dx + dz * dz);

                        float slope = heightDiff / distance;

                        if (slope > avalancheSlope && heightDiff > minAvalancheAmount)
                        {
                            float priority = slope * heightDiff / Mathf.Pow(distance, conicShapeFactor);
                            neighbors.Add((dx, dz, priority));
                        }
                    }
                }

                if (neighbors.Count == 0) continue;

                float totalWeight = 0f;
                foreach (var n in neighbors) totalWeight += n.priority;

                float maxTransfer = availableSand * avalancheTrasnferRate;

                foreach (var (dx, dz, priority) in neighbors)
                {
                    int nx = x + dx;
                    int nz = z + dz;

                    float proportion = priority / totalWeight;
                    float transfer = maxTransfer * proportion;

                    float maxDiff = (sandElev[x, z] - Math.Max(sandElev[nx, nz], terrainElev[nx, nz])) * 0.5f;
                    transfer = Mathf.Min(transfer, maxDiff);

                    if (transfer > minAvalancheAmount)
                    {
                        sandElev[x, z] -= transfer;
                        sandElev[nx, nz] = Math.Max(sandElev[nx, nz], terrainElev[nx, nz]) + transfer;

                        Vector2Int neighborCell = new Vector2Int(nx, nz);

                        if (!visited.Contains(neighborCell))
                            toProcess.Push(neighborCell); // ⚠️ Propagación
                    }
                }
            }
        }

        /*
        private Vector2Int SelectMostUnstableCell(int sampleSize = 50)
        {
            int width = sandElev.GetLength(0);
            int height = sandElev.GetLength(1);

            List<(Vector2Int pos, float maxSlope)> candidates = new();

            for (int x = 1; x < width - 1; x++)
            {
                for (int z = 1; z < height - 1; z++)
                {
                    float h = sandElev[x, z];
                    float b = terrainElev[x, z];

                    if (h <= b + minAvalancheAmount) continue;

                    float maxSlope = 0f;

                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dz = -1; dz <= 1; dz++)
                        {
                            if (dx == 0 && dz == 0) continue;

                            int nx = x + dx;
                            int nz = z + dz;

                            if (!IsValidCell(nx, nz)) continue;

                            float neighborH = Math.Max(sandElev[nx, nz], terrainElev[nx, nz]);
                            float heightDiff = h - neighborH;
                            float distance = Mathf.Sqrt(dx * dx + dz * dz);

                            float slope = heightDiff / distance;
                            if (slope > maxSlope) maxSlope = slope;
                        }
                    }

                    if (maxSlope > avalancheSlope)
                    {
                        candidates.Add((new Vector2Int(x, z), maxSlope));
                    }
                }
            }

            if (candidates.Count == 0)
                return new Vector2Int(rnd.Next(1, width - 1), rnd.Next(1, height - 1)); // fallback

            // Ordenar por mayor pendiente
            candidates.Sort((a, b) => b.maxSlope.CompareTo(a.maxSlope));

            // Tomar al azar uno de los top N más inestables
            int limit = Mathf.Min(sampleSize, candidates.Count);
            int index = rnd.Next(0, limit);
            return candidates[index].pos;
        }
        */


        /// <summary>
        /// Inicializa la cola reactiva de avalanchas
        /// </summary>
        public void InitAvalancheQueue()
        {
            if (avalancheQueue == null) avalancheQueue = new Queue<Vector2Int>();
            if (inQueue == null) inQueue = new HashSet<Vector2Int>();

            int width = sandElev.GetLength(0);
            int height = sandElev.GetLength(1);

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
        public void RunAvalancheStepWithCriticalQueue()
        {
            if (criticalCellsQueue == null || criticalCellsQueue.Count == 0)
                return;

            var current = criticalCellsQueue.Dequeue();
            criticalCellsSet.Remove(current);

            Stack<Vector2Int> toProcess = new Stack<Vector2Int>();
            HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

            toProcess.Push(current);

            while (toProcess.Count > 0)
            {
                var cell = toProcess.Pop();
                int x = cell.x;
                int z = cell.y;

                if (visited.Contains(cell)) continue;
                visited.Add(cell);

                float h = sandElev[x, z];
                float b = terrainElev[x, z];
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

                        float nh = Math.Max(sandElev[nx, nz], terrainElev[nx, nz]);
                        float heightDiff = h - nh;
                        float distance = Mathf.Sqrt(dx * dx + dz * dz);
                        float slope = heightDiff / distance;

                        if (slope > avalancheSlope && heightDiff > minAvalancheAmount)
                        {
                            float priority = slope * heightDiff / Mathf.Pow(distance, conicShapeFactor);
                            neighbors.Add((dx, dz, priority));
                        }
                    }
                }

                if (neighbors.Count == 0) continue;

                float totalWeight = 0f;
                foreach (var n in neighbors) totalWeight += n.priority;

                float maxTransfer = available * avalancheTrasnferRate;

                foreach (var (dx, dz, priority) in neighbors)
                {
                    int nx = x + dx;
                    int nz = z + dz;
                    float proportion = priority / totalWeight;
                    float transfer = maxTransfer * proportion;
                    float maxDiff = (sandElev[x, z] - Math.Max(sandElev[nx, nz], terrainElev[nx, nz])) * 0.5f;
                    transfer = Mathf.Min(transfer, maxDiff);

                    if (transfer > minAvalancheAmount)
                    {
                        sandElev[x, z] -= transfer;
                        sandElev[nx, nz] = Mathf.Max(sandElev[nx, nz], terrainElev[nx, nz]) + transfer;

                        Vector2Int neighbor = new Vector2Int(nx, nz);

                        if (!visited.Contains(neighbor))
                            toProcess.Push(neighbor);

                        // Agregar al sistema si ahora es crítico
                        if (CellIsCritical(nx, nz) && !criticalCellsSet.Contains(neighbor))
                        {
                            criticalCellsQueue.Enqueue(neighbor);
                            criticalCellsSet.Add(neighbor);
                        }
                    }
                }
            }
        }

        public void InitCriticalSlopeCells()
        {
            if (avalancheQueue == null) avalancheQueue = new Queue<Vector2Int>();
            if (inQueue == null) inQueue = new HashSet<Vector2Int>();

            List<(Vector2Int pos, float slope)> critical = new();

            int width = sandElev.GetLength(0);
            int height = sandElev.GetLength(1);

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

            //Debug.Log($"Celdas críticas iniciales activadas: {critical.Count}");
        }
        private bool CellIsCritical(int x, int z)
        {
            float h = sandElev[x, z];
            float b = terrainElev[x, z];

            if (h <= b + minAvalancheAmount) return false;

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    if (dx == 0 && dz == 0) continue;

                    int nx = x + dx;
                    int nz = z + dz;

                    if (!IsValidCell(nx, nz)) continue;

                    float nh = Math.Max(sandElev[nx, nz], terrainElev[nx, nz]);
                    float heightDiff = h - nh;
                    float distance = size * Mathf.Sqrt(dx * dx / (xResolution * xResolution) + dz * dz / (zResolution * zResolution));

                    float slope = heightDiff / distance;
                    if (slope > avalancheSlope)
                        return true;
                }
            }

            return false;
        }

        public void RunAvalancheStepWithQueues()
        {
            if (avalancheQueue == null || avalancheQueue.Count == 0)
                return;

            var cell = avalancheQueue.Dequeue();
            inQueue.Remove(cell);

            int x = cell.x;
            int z = cell.y;

            float h = sandElev[x, z];
            float b = terrainElev[x, z];
            float available = h - b;
            if (available <= minAvalancheAmount) return;

            List<(int dx, int dz, float priority)> neighbors = new();

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    if (dx == 0 && dz == 0) continue;

                    int nx = x + dx;
                    int nz = z + dz;
                    if (!IsValidCell(nx, nz)) continue;

                    float nh = Math.Max(sandElev[nx, nz], terrainElev[nx, nz]);
                    float heightDiff = h - nh;
                    float distance = Mathf.Sqrt(dx * dx + dz * dz);
                    float slope = heightDiff / distance;

                    if (slope > avalancheSlope && heightDiff > minAvalancheAmount)
                    {
                        float priority = slope * heightDiff / Mathf.Pow(distance, conicShapeFactor);
                        neighbors.Add((dx, dz, priority));
                    }
                }
            }

            if (neighbors.Count == 0) return;

            float totalWeight = 0f;
            foreach (var n in neighbors) totalWeight += n.priority;

            float maxTransfer = available * avalancheTrasnferRate;

            foreach (var (dx, dz, priority) in neighbors)
            {
                int nx = x + dx;
                int nz = z + dz;
                float proportion = priority / totalWeight;
                float transfer = maxTransfer * proportion;

                float maxDiff = (sandElev[x, z] - Math.Max(sandElev[nx, nz], terrainElev[nx, nz])) * 0.5f;
                transfer = Mathf.Min(transfer, maxDiff);

                if (transfer > minAvalancheAmount)
                {
                    sandElev[x, z] -= transfer;
                    sandElev[nx, nz] = Math.Max(sandElev[nx, nz], terrainElev[nx, nz]) + transfer;
                    ActivateCell(nx, nz); // Propagación local
                }

            }

            // Revisar vecinos del colapsador (x, z) después del colapso
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    //if (dx == 0 && dz == 0) continue;

                    int nx = x + dx;
                    int nz = z + dz;

                    if (!IsValidCell(nx, nz)) continue;

                    float slope = GetMaxSlopeAt(nx, nz);
                    if (slope > avalancheSlope)
                    {
                        ActivateCell(nx, nz); // Propagación explícita a vecinos
                    }
                }
            }
        }


        public int RunAvalancheBurst(int maxStepsPerCall = 50, bool verbose = false)
        {

            if (verbose) Debug.Log($"Granos avalanchados por paso: {avalancheQueue.Count}/{maxStepsPerCall}");
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

                float h = sandElev[x, z];
                float b = terrainElev[x, z];
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

                        float nh = Math.Max(sandElev[nx, nz], terrainElev[nx, nz]);
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

                float totalWeight = 0f;
                foreach (var n in neighbors) totalWeight += n.priority;

                float maxTransfer = available * avalancheTrasnferRate;

                foreach (var (dx, dz, priority) in neighbors)
                {
                    int nx = x + dx;
                    int nz = z + dz;
                    float proportion = priority / totalWeight;
                    float transfer = maxTransfer * proportion;

                    float maxDiff = (sandElev[x, z] - Math.Max(sandElev[nx, nz], terrainElev[nx, nz])) * 0.5f;
                    transfer = Mathf.Min(transfer, maxDiff);

                    if (transfer > minAvalancheAmount)
                    {
                        sandElev[x, z] -= transfer;
                        sandElev[nx, nz] = Math.Max(sandElev[nx, nz], terrainElev[nx, nz]) + transfer;

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

                if  (steps % 40 == 0)
                    localStack.Reverse();
            }

            return avalancheQueue.Count;
        }

        private float GetMaxSlopeAt(int x, int z)
        {
            float h = sandElev[x, z];
            float b = terrainElev[x, z];
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

                    float nh = Math.Max(sandElev[nx, nz], terrainElev[nx, nz]);
                    float heightDiff = h - nh;
                    float distance = size * Mathf.Sqrt(dx * dx / (xResolution * xResolution) + dz * dz / (zResolution * zResolution));
                    float slope = heightDiff / distance;

                    if (slope > maxSlope)
                        maxSlope = slope;
                }
            }
            return maxSlope;
        }




    }

    
}
