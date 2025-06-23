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
        public void AddSandCone(int centerX, int centerZ, float maxHeight, float radius)
        {
            int xMin = Mathf.Max(0, Mathf.FloorToInt(centerX - radius));
            int xMax = Mathf.Min(duneModel.xResolution - 1, Mathf.CeilToInt(centerX + radius));
            int zMin = Mathf.Max(0, Mathf.FloorToInt(centerZ - radius));
            int zMax = Mathf.Min(duneModel.zResolution - 1, Mathf.CeilToInt(centerZ + radius));

            for (int x = xMin; x <= xMax; x++)
            {
                for (int z = zMin; z <= zMax; z++)
                {
                    float dx = x - centerX;
                    float dz = z - centerZ;
                    float dist = Mathf.Sqrt(dx * dx + dz * dz);
                    if (dist > radius) continue;

                    // Perfil cónico
                    float height = maxHeight * (1 - (dist / radius));
                    //float height = maxHeight * Mathf.Exp(- (dist * dist) / (2 * sigma * sigma));
                    duneModel.sandElev[x, z] += height;

                    duneModel.ActivateCell(x, z);
                    duneModel.UpdateShadow(x, z, duneModel.dx, duneModel.dz);
                }
            }
        }

    }
}