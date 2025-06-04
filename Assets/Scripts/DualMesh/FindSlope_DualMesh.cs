using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ue=UnityEngine;

namespace DunefieldModel_DualMesh
{

  // 'Valid' surrounding cells have elevations within +- 2 of this cell
  // We only care about the case of 2 or -2, but to gracefully accommodate
  // anomolies beyond that, we can use >= or <=.

  public interface IFindSlope
  {
    void Init(ref float[,] sandElev, ref float[,] terrainElev, int WidthAcross, int LengthDownwind, float slope);
    void SetOpenEnded(bool NewState);
    int Upslope(int x, int z, int dxWind, int dzWind, out int xSteep, out int zSteep);
    int Downslope(int x, int z, int dxWind, int dyWind, out int xLow, out int zLow);
    int AvalancheSlope(int x, int z, out int xLow, out int zLow);
  }

  public class FindSlopeMooreDeterministic : IFindSlope
  {
    public float[,] Elev, sandElev, terrainElev;
    public bool[,] isErodable;
    public int xDOF, zDOF; // xDOF = xResolution - 1, zDOF = zResolution - 1
    public bool OpenEnded = false;

    public float slope;

    public void Init(ref float[,] sandElev, ref float[,] terrainElev, int xResolution, int zResolution, float slope)
    {
      this.sandElev = sandElev;
      this.terrainElev = terrainElev;
      this.slope = slope;
      xDOF = xResolution - 1;
      zDOF = zResolution - 1;
    }

    public void SetOpenEnded(bool NewState)
    {
      OpenEnded = NewState;
    }
    public int Upslope(int xCenter, int zCenter, int dxWind, int dzWind, out int xSteep, out int zSteep)
    {
      xSteep = xCenter;
      zSteep = zCenter;

      if (terrainElev[xCenter, zCenter] >= sandElev[xCenter, zCenter])
        return 0;  // No se puede erosionar desde aquí

      float h = sandElev[xCenter, zCenter];
      float maxDelta = slope;
      float maxAlignment = float.NegativeInfinity;

      int[] dX = { -1, 0, 1, -1, 1, -1, 0, 1 };
      int[] dZ = { -1, -1, -1, 0, 0, 1, 1, 1 };

      for (int i = 0; i < 8; i++)
      {
        int xi = (xCenter + dX[i]) & xDOF;
        int zi = (zCenter + dZ[i]) & zDOF;

        if (OpenEnded && IsOutside(xi, zi))
          continue;

        float hi = sandElev[xi, zi];
        float delta = hi - h;

        if (delta >= slope)
        {
          float alignment = dxWind * dX[i] + dzWind * dZ[i];

          if (delta > maxDelta || (Math.Abs(delta - maxDelta) < 1e-6 && alignment > maxAlignment))
          {
            maxDelta = delta;
            maxAlignment = alignment;
            xSteep = xi;
            zSteep = zi;
          }
        }
      }

      return (xSteep != xCenter || zSteep != zCenter) ? 2 : 0;
    }

    

    public int Downslope(int xCenter, int zCenter, int dxWind, int dzWind, out int xLow, out int zLow)
    {
        xLow = xCenter;
        zLow = zCenter;

        if (terrainElev[xCenter, zCenter] >= sandElev[xCenter, zCenter] + slope)
        return 0; // No se puede transportar desde aquí

        float h = Math.Max(sandElev[xCenter, zCenter], terrainElev[xCenter, zCenter]);
        float minDelta = -slope;
        float maxAlignment = float.NegativeInfinity;

        int[] dX = { -1, 0, 1, -1, 1, -1, 0, 1 };
        int[] dZ = { -1, -1, -1, 0, 0, 1, 1, 1 };

        for (int i = 0; i < 8; i++)
        {
            int xi = (xCenter + dX[i]) & xDOF;
            int zi = (zCenter + dZ[i]) & zDOF;

            if (OpenEnded && IsOutside(xi, zi))
          continue;

            float hi = Math.Max(sandElev[xi, zi], terrainElev[xi, zi]);
            float delta = hi - h;  // queremos que sea negativo

            if (delta <= -slope)
            {
                float alignment = dxWind * dX[i] + dzWind * dZ[i];

                if (delta < minDelta || (Math.Abs(delta - minDelta) < 1e-6 && alignment > maxAlignment))
                {
                    minDelta = delta;
                    maxAlignment = alignment;
                    xLow = xi;
                    zLow = zi;
                }
            }
        }

        return (xLow != xCenter || zLow != zCenter) ? 2 : 0;
    }

    public int AvalancheSlope(int xCenter, int zCenter, out int xLow, out int zLow)
    {
      /// <summary>
      /// Encuentra la pendiente de avalancha desde la posición (x, z).
      /// </summary>
      /// <param name="xCenter">Coordenada X de la posición.</param>
      /// <param name="zCenter">Coordenada Z de la posición.</param> 
      /// <param name="xLow">Coordenada X del punto más bajo encontrado.</param> 
      /// <param name="zLow">Coordenada Z del punto más bajo encontrado.</param>
      /// <returns>0 si no se encuentra una pendiente, 2 si se encuentra una pendiente.</returns>

        xLow = xCenter;
        zLow = zCenter;

        if (terrainElev[xCenter, zCenter] >= sandElev[xCenter, zCenter] + slope)
        return 0; // No se puede transportar desde aquí

        float h = Math.Max(sandElev[xCenter, zCenter], terrainElev[xCenter, zCenter]);

        int[] dX = { -1, 0, 1, -1, 1, -1, 0, 1 };
        int[] dZ = { -1, -1, -1, 0, 0, 1, 1, 1 };

        for (int i = 0; i < 8; i++)
        {
            int xi = (xCenter + dX[i]) & xDOF;
            int zi = (zCenter + dZ[i]) & zDOF;

            if (OpenEnded && IsOutside(xi, zi)) continue;

            float hi = Math.Max(sandElev[xi, zi], terrainElev[xi, zi]);
            float delta = hi - h;  // queremos que sea negativo
            float minDelta = float.NegativeInfinity;

            if (delta <= -slope)
            {
                if (delta < minDelta || (Math.Abs(delta - minDelta) < 1e-6))
                {
                    minDelta = delta;
                    xLow = xi;
                    zLow = zi;
                }
            }
        }

        return (xLow != xCenter || zLow != zCenter) ? 2 : 0;
    }

    private bool IsOutside(int x, int z)
    {
      return x < 0 || x >= xDOF || z < 0 || z >= zDOF;
    }
  }
}
