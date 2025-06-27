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

        public void FlatSand(int centerX, int centerZ, int radius)
        {
            int count = 0;
            float sum = 0f;

            // Paso 1: calcular promedio de altura total (arena + terreno)
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dz = -radius; dz <= radius; dz++)
                {
                    int x = centerX + dx;
                    int z = centerZ + dz;

                    if (x < 0 || x >= duneModel.xResolution || z < 0 || z >= duneModel.zResolution) continue;

                    //float h = Math.Max(terrainElev[x, z], duneModel.sandElev[x, z]);
                    float h = (terrainElev[x, z] >= duneModel.sandElev[x, z]) ? 0 : duneModel.sandElev[x, z];
                    sum += h;
                    count++;
                }
            }

            if (count == 0) return;
            float avg = sum / count;

            // Paso 2: nivelar el terreno con la arena para igualar la altura promedio
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dz = -radius; dz <= radius; dz++)
                {
                    int x = centerX + dx;
                    int z = centerZ + dz;

                    if (x < 0 || x >= duneModel.xResolution || z < 0 || z >= duneModel.zResolution) continue;

                    float total = Math.Max(duneModel.sandElev[x, z], terrainElev[x, z]);
                    float delta = avg - total;

                    // Aplicar cambio solo al terreno si hay más arena
                    if (delta < 0f)
                        duneModel.sandElev[x, z] += delta;

                    //terrainElev[x, z] = duneModel.terrainElev[x, z];

                    duneModel.UpdateShadow(x, z, duneModel.dx, duneModel.dz);
                    duneModel.ActivateCell(x, z);
                }
            }
        }
    }
}