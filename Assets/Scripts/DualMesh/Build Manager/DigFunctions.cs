using UnityEngine;
using DunefieldModel_DualMesh;
using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine.UIElements;
//using System.Numerics;

namespace Building
{
    public partial class BuildSystem
    {

        public void DigAction(int centerX, int centerZ, int radius, float digDepth, bool acumular = false)
        {
            if (terrain[centerX, centerZ] >= duneModel.sand[centerX, centerZ]) return;

            NativeGrid sandElev = duneModel.sand;

            int width = sandElev.Width;
            int height = sandElev.Height;

            float maxHeight = float.MinValue;

            // 1. Encontrar altura máxima
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dz = -radius; dz <= radius; dz++)
                {
                    int nx = centerX + dx;
                    int nz = centerZ + dz;
                    if (nx < 0 || nx >= width || nz < 0 || nz >= height) continue;

                    float h = Mathf.Max(sandElev[nx, nz], terrain[nx, nz]);
                    if (h > maxHeight) maxHeight = h;
                }
            }

            // 2. Cavar y acumular arena removida
            float totalRemoved = 0f;

            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dz = -radius; dz <= radius; dz++)
                {
                    int nx = centerX + dx;
                    int nz = centerZ + dz;
                    if (nx < 0 || nx >= width || nz < 0 || nz >= height) continue;

                    float dist = Mathf.Sqrt(dx * dx + dz * dz);

                    if (dist <= radius - 0.5f)
                    {
                        if (terrain[nx, nz] >= sandElev[nx, nz] || constructionGrid[nx, nz].Count > 0) continue;
                        float original = sandElev[nx, nz];
                        float newHeight = original - digDepth;
                        newHeight = newHeight > terrain[nx, nz] ? newHeight : terrain[nx, nz];
                        float removed = original - newHeight;
                        totalRemoved += removed;

                        //terrainElev[nx, nz] = newHeight;
                        sandElev[nx, nz] = newHeight;
                        duneModel.sandChanges.AddChanges(nx, nz);
                        duneModel.ActivateCell(nx, nz);
                        duneModel.UpdateShadow(nx, nz, duneModel.dx, duneModel.dz);
                    }
                }
            }

            if (!acumular)
            {
                resourceManager.AddResource("Sand", 1);
                return;
            }

            // 3. Recolectar celdas del anillo expandido con peso
                List<(int x, int z, float weight)> ringCells = new();
            float weightSum = 0f;

            int extraSpreadRadius = CalculateExtraSpreadRadius(totalRemoved, radius);

            for (int dx = -(radius + extraSpreadRadius); dx <= (radius + extraSpreadRadius); dx++)
            {
                for (int dz = -(radius + extraSpreadRadius); dz <= (radius + extraSpreadRadius); dz++)
                {
                    int nx = centerX + dx;
                    int nz = centerZ + dz;
                    if (nx < 0 || nx >= width || nz < 0 || nz >= height) continue;

                    float dist = Mathf.Sqrt(dx * dx + dz * dz);
                    if (dist > radius && dist <= radius + extraSpreadRadius)
                    {
                        if (constructionGrid[nx, nz].Count > 0) continue;
                        // Peso inverso a la distancia (más cerca → más arena)
                        float weight = 1f / (dist + 0.01f);
                        ringCells.Add((nx, nz, weight));
                        weightSum += weight;
                    }
                }
            }

            // 4. Distribuir la arena suavemente
            foreach (var (x, z, weight) in ringCells)
            {
                float amount = totalRemoved * (weight / weightSum);
                sandElev[x, z] += amount;
                duneModel.sandChanges.AddChanges(x, z);

                duneModel.ActivateCell(x, z);
                duneModel.UpdateShadow(x, z, duneModel.dx, duneModel.dz);
            }
        }


        int CalculateExtraSpreadRadius(float totalRemoved, float radius, float maxRimHeight = 0.5f)
        {
            float baseArea = Mathf.PI * radius * radius;
            float requiredArea = totalRemoved / maxRimHeight;

            float requiredTotalRadius = Mathf.Sqrt((requiredArea + baseArea) / Mathf.PI);
            float extraRadius = requiredTotalRadius - radius;

            return Mathf.CeilToInt(extraRadius);
        }
    }
}