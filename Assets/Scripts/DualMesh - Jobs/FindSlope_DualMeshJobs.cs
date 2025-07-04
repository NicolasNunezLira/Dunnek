using Unity.Collections;
using Unity.Mathematics;
using UnityEditor;

namespace DunefieldModel_DualMeshJobs
{
  public static class FindSlope
  {
    static readonly int2[] Offsets = new int2[]
    {
      new(1, 0), new(1, 1), new(0, 1), new(-1, 1),
      new(-1, 0), new(-1, -1), new(0, -1), new(1, -1)
    };

    public struct SlopeResult
    {
      public int Code;
      public int X;
      public int Z;

      public bool isValid => Code > 0;
    }

    // -------------------- Upslope --------------------------
    public static SlopeResult Upslope(
      int x, int z,
      int dx, int dz,
      NativeArray<float> sand,
      NativeArray<float> terrain,
      int xResolution, int zResolution,
      float slope,
      bool openEnded
    )
    {
      int xDOF = xResolution - 1;

      int indexCenter = x + (xResolution * z);
      //UnityEngine.Debug.Log("Center = (" + x + ", " + z + ") = " + indexCenter);

      if (terrain[indexCenter] >= sand[indexCenter])
      {
        return new SlopeResult { Code = 0, X = -1, Z = -1 }; // No slope if terrain is higher or equal to sand
      }

      float h = sand[indexCenter];
      float maxDelta = slope;
      float maxAlign = float.NegativeInfinity;
      int xSteep = x;
      int zSteep = z;

      // Recorrer las 8 direcciones
      for (int i = 0; i < 8; i++)
      {
        int2 o = Offsets[i];
        int xi = (x + o.x + xResolution) & xDOF;   // wrap toroidal usando «&» o «%»
        int zi = (z + o.y + xResolution) & xDOF;
        int idx = xi + zi * xResolution;

        if (openEnded && IsOutside(xi, zi, xResolution))   // helper estático
          continue;

        float delta = sand[idx] - h;
        if (delta >= slope)
        {
          float align = dx * o.x + dz * o.y;

          if (delta > maxDelta || (math.abs(delta - maxDelta) < 1e-6f && align > maxAlign))
          {
            maxDelta = delta;
            maxAlign = align;
            xSteep = xi;
            zSteep = zi;
          }
        }
      }

      return (xSteep != x || zSteep != z) ? new SlopeResult { Code = 2, X = xSteep, Z = zSteep } : new SlopeResult { Code = -1, X = -1, Z = -1 };
    }

    // ---------- DOWNSLOPE ----------
    public static SlopeResult Downslope(
        int x, int z,
        int dx, int dz,
        NativeArray<float> sand, NativeArray<float> terrain,
        int xResolution,
        float slope,
        bool openEnded)
    {
      int xDOF = xResolution - 1;
      int indexCenter = x + z * xResolution;

      if (terrain[indexCenter] >= sand[indexCenter] + slope) new SlopeResult { Code = 0, X = -1, Z = -1 };

      float h = math.max(sand[indexCenter], terrain[indexCenter]);
      float minDelta = -slope;
      float maxAlign = float.NegativeInfinity;
      int xLow = x;
      int zLow = z;

      for (int i = 0; i < 8; i++)
      {
        int2 o = Offsets[i];
        int xi = (x + o.x + xResolution) & xDOF;
        int zi = (z + o.y + xResolution) & xDOF;
        int idx = xi + zi * xResolution;

        if (openEnded && IsOutside(xi, zi, xResolution))
          continue;

        float hi = math.max(sand[idx], terrain[idx]);
        float delta = hi - h;            // negativo deseado

        if (delta <= -slope)
        {
          float align = dx * o.x + dz * o.y;
          if (delta < minDelta || (math.abs(delta - minDelta) < 1e-6f && align > maxAlign))
          {
            minDelta = delta;
            maxAlign = align;
            xLow = xi;
            zLow = zi;
          }
        }
      }

      return (xLow != x || zLow != z) ? new SlopeResult{ Code = 2, X = xLow, Z = zLow } : new SlopeResult{ Code = -1, X = -1, Z = -1 };
    }
    
    // ---------- AVALANCHE ----------
    public static SlopeResult AvalancheSlope(
        int x, int z,
        NativeArray<float> sand, NativeArray<float> terrain,
        int xResolution,
        float avalancheSlope,
        bool openEnded)
    {
        int xDOF = xResolution - 1;
        int indexCenter = x + z * xResolution;

        if (terrain[indexCenter] >= sand[indexCenter] + avalancheSlope) new SlopeResult { Code = 0, X = -1, Z = -1 };

        float h     = math.max(sand[indexCenter], terrain[indexCenter]);
        float best  = float.NegativeInfinity;
        int   xLow  = x;
        int   zLow  = z;

        for (int i = 0; i < 8; i++)
        {
            int2 o  = Offsets[i];
            int xi  = (x + o.x + xResolution) & xDOF;
            int zi  = (z + o.y + xResolution) & xDOF;
            int idx = xi + zi * xResolution;

            if (openEnded && IsOutside(xi, zi, xResolution))
                continue;

            float hi    = math.max(sand[idx], terrain[idx]);
            float delta = hi - h;        // negativo deseado

            if (delta <= -avalancheSlope && delta < best)
            {
                best = delta;
                xLow = xi;
                zLow = zi;
            }
        }

        return (xLow != x || zLow != z) ? new SlopeResult{ Code = 2, X = xLow, Z = zLow } : new SlopeResult{ Code = -1, X = -1, Z = -1 };
    }

    // ---------- HELPERS ----------
    static bool IsOutside(int x, int z, int width) => x < 0 || x >= width || z < 0 || z >= width;
  }
}