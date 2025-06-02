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
    int Upslope(int w, int x, int dxWind, int dyWind, out int wSteep, out int xSteep);
    int Downslope(int w, int x, int dxWind, int dyWind, out int wLow, out int xLow);
  }

  /*
  #region Von Neumann Deterministic
  public class FindSlopeVonNeumannDeterministic : IFindSlope
  {
    public float[,] Elev;
    public int mWidth;
    public int mLength;

    public float slope;
    public bool OpenEnded = false;

    public int dx, dy;

    public void Init(ref float[,] Elev, int WidthAcross, int LenghtDownwind, float slope, int dx, int dy)
    {
      this.Elev = Elev;
      this.slope = slope;
      this.dx = dx;
      this.dy = dy;
      mWidth = WidthAcross - 1;
      mLength = LenghtDownwind - 1;
    }

    public void SetOpenEnded(bool NewState)
    {
      OpenEnded = NewState;
    }

    public int Upslope(int wCenter, int xCenter, int dx, int dy, out int wSteep, out int xSteep)
    {
      wSteep = wCenter;
      xSteep = xCenter;

      float h = Elev[wCenter, xCenter];

      int wn = (wCenter + dy + mWidth) & mWidth;
      int xn = (xCenter + dx + mLength) & mLength;

      if (OpenEnded && (xn < 0 || xn > mLength || wn < 0 || wn > mWidth))
        return 0;

      float diff = Elev[wn, xn] - h;

      if (diff >= slope)
      {
        wSteep = wn;
        xSteep = xn;
        return 2;
      }

      return 0;
    }

    public int Downslope(int wCenter, int xCenter, int dx, int dy, out int wSteep, out int xSteep)
    {
      wSteep = wCenter;
      xSteep = xCenter;

      float h = Elev[wCenter, xCenter];

      int wn = (wCenter + dy + mWidth) & mWidth;
      int xn = (xCenter + dx + mLength) & mLength;

      if (OpenEnded && (xn < 0 || xn > mLength || wn < 0 || wn > mWidth))
        return 0;

      float diff = h - Elev[wn, xn];

      if (diff >= slope)
      {
        wSteep = wn;
        xSteep = xn;
        return 2;
      }

      return 0;
    }

  }
  

  #endregion
  */

  /*
  #region Von Neumann Stochastic
  //    X
  //  X   X
  //    X
  public class FindSlopeVonNeumannStochastic : IFindSlope
  {
    public float[,] Elev;
    public int mWidth;
    public int mLength;
    public bool OpenEnded = false;

    public float slope;
    protected Random rnd = new Random(123);

    public void Init(ref float[,] Elev, int WidthAcross, int LenghtDownwind)
    {
      this.Elev = Elev;
      mWidth = WidthAcross - 1;
      mLength = LenghtDownwind - 1;
    }

    public void SetOpenEnded(bool NewState)
    {
      OpenEnded = NewState;
    }

    public int Upslope(int wCenter, int xCenter, out int wSteep, out int xSteep)
    {
      int[] rises = new int[4];
      int wLeft, wRight, xUp, xDown, nRises;
      wSteep = wCenter; xSteep = xCenter;
      xUp = xDown = nRises = 0;
      float h = Elev[wCenter, xCenter];
      if ((!OpenEnded || (xCenter > 0)) && ((Elev[wCenter, xUp = (xCenter - 1) & mLength] - h) >= 2))
        rises[nRises++] = 0;
      if ((Elev[wRight = (wCenter + 1) & mWidth, xCenter] - h) >= 2)
        rises[nRises++] = 1;
      if ((!OpenEnded || (xCenter != mLength)) && ((Elev[wCenter, xDown = (xCenter + 1) & mLength] - h) >= 2))
        rises[nRises++] = 2;
      if ((Elev[wLeft = (wCenter - 1) & mWidth, xCenter] - h) >= 2)
        rises[nRises++] = 3;
      if (nRises == 0)
        return 0;
      switch (rises[rnd.Next(0, nRises)])
      {
        case 0: xSteep = xUp; return 2;
        case 1: wSteep = wRight; return 2;
        case 2: xSteep = xDown; return 2;
        case 3: wSteep = wLeft; return 2;
      }
      return 0;
    }

    public int Downslope(int wCenter, int xCenter, out int wSteep, out int xSteep)
    {
      int[] drops = new int[4];
      int wLeft, wRight, xUp, xDown, nDrops;
      wSteep = wCenter; xSteep = xCenter;
      xUp = xDown = nDrops = 0;
      float h = Elev[wCenter, xCenter];
      if ((!OpenEnded || (xCenter > 0)) && ((h - Elev[wCenter, xUp = (xCenter - 1) & mLength]) >= 2))
        drops[nDrops++] = 0;
      if ((h - Elev[wRight = (wCenter + 1) & mWidth, xCenter]) >= 2)
        drops[nDrops++] = 1;
      if ((!OpenEnded || (xCenter != mLength)) && ((h - Elev[wCenter, xDown = (xCenter + 1) & mLength]) >= 2))
        drops[nDrops++] = 2;
      if ((h - Elev[wLeft = (wCenter - 1) & mWidth, xCenter]) >= 2)
        drops[nDrops++] = 3;
      if (nDrops == 0)
        return 0;
      switch (drops[rnd.Next(0, nDrops)])
      {
        case 0: xSteep = xUp; return 2;
        case 1: wSteep = wRight; return 2;
        case 2: xSteep = xDown; return 2;
        case 3: wSteep = wLeft; return 2;
      }
      return 0;
    }

  }
  #endregion
  */

  #region Moore Deterministic
  public class FindSlopeMooreDeterministic : IFindSlope
  {
    public float[,] Elev, sandElev, terrainElev;
    public bool[,] isErodable;
    public int mWidth;
    public int mLength;
    public bool OpenEnded = false;

    public float slope;

    public void Init(ref float[,] sandElev, ref float[,] terrainElev, int WidthAcross, int LenghtDownwind, float slope)
    {
      this.sandElev = sandElev;
      this.terrainElev = terrainElev;
      this.slope = slope;
      mWidth = WidthAcross - 1;
      mLength = LenghtDownwind - 1;
    }

    public void SetOpenEnded(bool NewState)
    {
      OpenEnded = NewState;
    }

    /*
    public int Upslope(int wCenter, int xCenter, out int wSteep, out int xSteep)
  {
    //  6 2 8
    //  1   4
    //  5 3 7
    int wLeft, wRight, xUp, xDown;
    wSteep = wCenter; xSteep = xCenter;
    xUp = xDown = 0;
    float h = Elev[wCenter, xCenter];  // first check Von Neumann neighbours
    if ((!OpenEnded || (xCenter > 0)) && ((Elev[wCenter, xUp = (xCenter - 1) & mLength] - h) >= slope))
    {
      xSteep = xUp; return 2;
    }
    if ((Elev[wRight = (wCenter + 1) & mWidth, xCenter] - h) >= slope)
    {
      wSteep = wRight; return 2;
    }
    if ((Elev[wLeft = (wCenter - 1) & mWidth, xCenter] - h) >= slope)
    {
      wSteep = wLeft; return 2;
    }
    if ((!OpenEnded || (xCenter != mLength)) && ((Elev[wCenter, xDown = (xCenter + 1) & mLength] - h) >= slope))
    {
      xSteep = xDown; return 2;
    }
    // now check diagonal neighbours
    if (!OpenEnded || (xCenter > 0))
    {
      if ((Elev[wLeft, xUp] - h) >= slope)
      {
        wSteep = wLeft; xSteep = xUp; return 2;
      }
      if ((Elev[wRight, xUp] - h) >= slope)
      {
        wSteep = wRight; xSteep = xUp; return 2;
      }
    }
    if (!OpenEnded || (xCenter != mLength))
    {
      if ((Elev[wLeft, xDown] - h) >= slope)
      {
        wSteep = wLeft; xSteep = xDown; return 2;
      }
      if ((Elev[wRight, xDown] - h) >= slope)
      {
        wSteep = wRight; xSteep = xDown; return 2;
      }
    }
    return 0;
  }
  */
    public int Upslope(int wCenter, int xCenter, int dxWind, int dyWind, out int wSteep, out int xSteep)
    {
      wSteep = wCenter;
      xSteep = xCenter;

      if (terrainElev[xCenter, wCenter] >= sandElev[xCenter, wCenter])
        return 0;  // No se puede erosionar desde aquí

      float h = sandElev[wCenter, xCenter];
      float maxDelta = slope;
      float maxAlignment = float.NegativeInfinity;

      int[] dW = { -1, 0, 1, -1, 1, -1, 0, 1 };
      int[] dX = { -1, -1, -1, 0, 0, 1, 1, 1 };

      for (int i = 0; i < 8; i++)
      {
        int wi = (wCenter + dW[i]) & mWidth;
        int xi = (xCenter + dX[i]) & mLength;

        if (OpenEnded && IsOutside(wi, xi))
          continue;

        float hi = sandElev[wi, xi];
        float delta = hi - h;

        if (delta >= slope)
        {
          float alignment = dxWind * dW[i] + dyWind * dX[i];

          if (delta > maxDelta || (Math.Abs(delta - maxDelta) < 1e-6 && alignment > maxAlignment))
          {
            maxDelta = delta;
            maxAlignment = alignment;
            wSteep = wi;
            xSteep = xi;
          }
        }
      }

      return (wSteep != wCenter || xSteep != xCenter) ? 2 : 0;
    }

    /*
    public int Downslope(int wCenter, int xCenter, out int wSteep, out int xSteep)
    {
      //  8 2 6
      //  4   1
      //  7 3 5
      int wLeft, wRight, xUp, xDown;
      wSteep = wCenter; xSteep = xCenter;
      xUp = xDown = 0;
      float h = Elev[wCenter, xCenter];
      if ((!OpenEnded || (xCenter != mLength)) && ((h - Elev[wCenter, xDown = (xCenter + 1) & mLength]) >= slope))
      {
        xSteep = xDown; return 2;
      }
      if ((h - Elev[wRight = (wCenter + 1) & mWidth, xCenter]) >= slope)
      {
        wSteep = wRight; return 2;
      }
      if ((h - Elev[wLeft = (wCenter - 1) & mWidth, xCenter]) >= slope)
      {
        wSteep = wLeft; return 2;
      }
      if ((!OpenEnded || (xCenter > 0)) && ((h - Elev[wCenter, xUp = (xCenter - 1) & mLength]) >= slope))
      {
        xSteep = xUp; return 2;
      }
      // now check diagonal neighbours
      if (!OpenEnded || (xCenter != mLength))
      {
        if ((h - Elev[wLeft, xDown]) >= slope)
        {
          wSteep = wLeft; xSteep = xDown; return 2;
        }
        if ((h - Elev[wRight, xDown]) >= slope)
        {
          wSteep = wRight; xSteep = xDown; return 2;
        }
      }
      if (!OpenEnded || (xCenter > 0))
      {
        if ((h - Elev[wLeft, xUp]) >= slope)
        {
          wSteep = wLeft; xSteep = xUp; return 2;
        }
        if ((h - Elev[wRight, xUp]) >= slope)
        {
          wSteep = wRight; xSteep = xUp; return 2;
        }
      }
      return 0;
    }
    */

    public int Downslope(int wCenter, int xCenter, int dxWind, int dyWind, out int wLow, out int xLow)
    {
        wLow = wCenter;
        xLow = xCenter;

        if (terrainElev[xCenter, wCenter] >= sandElev[xCenter, wCenter] + slope)
            return 0; // No se puede transportar desde aquí

        float h = Math.Max(sandElev[wCenter, xCenter], terrainElev[wCenter, xCenter]);
        float minDelta = -slope;
        float maxAlignment = float.NegativeInfinity;

        int[] dW = { -1, 0, 1, -1, 1, -1, 0, 1 };
        int[] dX = { -1, -1, -1, 0, 0, 1, 1, 1 };

        for (int i = 0; i < 8; i++)
        {
            int wi = (wCenter + dW[i]) & mWidth;
            int xi = (xCenter + dX[i]) & mLength;

            if (OpenEnded && IsOutside(wi, xi))
                continue;

            float hi = Math.Max(sandElev[wi, xi], terrainElev[wi, xi]);
            float delta = hi - h;  // queremos que sea negativo

            if (delta <= -slope)
            {
                float alignment = dxWind * dW[i] + dyWind * dX[i];

                if (delta < minDelta || (Math.Abs(delta - minDelta) < 1e-6 && alignment > maxAlignment))
                {
                    minDelta = delta;
                    maxAlignment = alignment;
                    wLow = wi;
                    xLow = xi;
                }
            }
        }

        return (wLow != wCenter || xLow != xCenter) ? 1 : 0;
    }



    private bool IsInside(int w, int x, int height, int width)
    {
      return w >= 0 && w < height && x >= 0 && x < width;
    }

    private bool IsOutside(int w, int x)
    {
      return w < 0 || w >= mWidth || x < 0 || x >= mLength;
    }
    #endregion

    /*

      #region Moore Stochastic
    //  + X +
    //  X   X
    //  + X +
    public class FindSlopeMooreStochastic : IFindSlope {
      public float[,] Elev;
      public int mWidth;
      public int mLength;

      public float slope;
      public bool OpenEnded = false;
      protected Random rnd = new Random(123);

      public void Init(ref float[,] Elev, int WidthAcross, int LenghtDownwind) {
        this.Elev = Elev;
        mWidth = WidthAcross - 1;
        mLength = LenghtDownwind - 1;
      }

      public void SetOpenEnded(bool NewState) {
        OpenEnded = NewState;
      }

      public int Upslope(int wCenter, int xCenter, out int wSteep, out int xSteep) {
        int[] rises = new int[4];
        int wLeft, wRight, xUp, xDown, nRises;
        wSteep = wCenter; xSteep = xCenter;
        xUp = xDown = nRises = 0;
        float h = Elev[wCenter, xCenter];
        if ((!OpenEnded || (xCenter > 0)) && ((Elev[wCenter, xUp = (xCenter - 1) & mLength] - h) >= 2))
          rises[nRises++] = 0;
        if ((Elev[wRight = (wCenter + 1) & mWidth, xCenter] - h) >= 2)
          rises[nRises++] = 1;
        if ((!OpenEnded || (xCenter != mLength)) && ((Elev[wCenter, xDown = (xCenter + 1) & mLength] - h) >= 2))
          rises[nRises++] = 2;
        if ((Elev[wLeft = (wCenter - 1) & mWidth, xCenter] - h) >= 2)
          rises[nRises++] = 3;
        if (nRises > 0)
          switch (rises[rnd.Next(0, nRises)]) {
            case 0: xSteep = xUp; return 2;
            case 1: wSteep = wRight; return 2;
            case 2: xSteep = xDown; return 2;
            case 3: wSteep = wLeft; return 2;
          }
        // none of the Von Neumann cells qualified; how about diagonal neighbours?
        if (!OpenEnded || (xCenter > 0)) {
          if ((Elev[wLeft, xUp] - h) >= 2)
            rises[nRises++] = 0;
          if ((Elev[wRight, xUp] - h) >= 2)
            rises[nRises++] = 2;
        }
        if (!OpenEnded || (xCenter != mLength)) {
          if ((Elev[wLeft, xDown] - h) >= 2)
            rises[nRises++] = 1;
          if ((Elev[wRight, xDown] - h) >= 2)
            rises[nRises++] = 3;
        }
        if (nRises == 0)
          return 0;
        switch (rises[rnd.Next(0, nRises)]) {
          case 0: wSteep = wLeft; xSteep = xUp; return 2;
          case 1: wSteep = wLeft; xSteep = xDown; return 2;
          case 2: wSteep = wRight; xSteep = xUp; return 2;
          case 3: wSteep = wRight; xSteep = xDown; return 2;
        }
        return 0;
      }

      public int Downslope(int wCenter, int xCenter, out int wSteep, out int xSteep) {
        int[] drops = new int[4];
        int wLeft, wRight, xUp, xDown, nDrops;
        wSteep = wCenter; xSteep = xCenter;
        xUp = xDown = nDrops = 0;
        float h = Elev[wCenter, xCenter];
        if ((!OpenEnded || (xCenter > 0)) && ((h - Elev[wCenter, xUp = (xCenter - 1) & mLength]) >= 2))
          drops[nDrops++] = 0;
        if ((h - Elev[wRight = (wCenter + 1) & mWidth, xCenter]) >= 2)
          drops[nDrops++] = 1;
        if ((!OpenEnded || (xCenter != mLength)) && ((h - Elev[wCenter, xDown = (xCenter + 1) & mLength]) >= 2))
          drops[nDrops++] = 2;
        if ((h - Elev[wLeft = (wCenter - 1) & mWidth, xCenter]) >= 2)
          drops[nDrops++] = 3;
        if (nDrops > 0)
          switch (drops[rnd.Next(0, nDrops)]) {
            case 0: xSteep = xUp; return 2;
            case 1: wSteep = wRight; return 2;
            case 2: xSteep = xDown; return 2;
            case 3: wSteep = wLeft; return 2;
          }
        // none of the Von Neumann cells qualified; how about diagonal neighbours?
        if (!OpenEnded || (xCenter > 0)) {
          if ((h - Elev[wLeft, xUp]) >= 2)
            drops[nDrops++] = 0;
          if ((h - Elev[wRight, xUp]) >= 2)
            drops[nDrops++] = 2;
        }
        if (!OpenEnded || (xCenter != mLength)) {
          if ((h - Elev[wLeft, xDown]) >= 2)
            drops[nDrops++] = 1;
          if ((h - Elev[wRight, xDown]) >= 2)
            drops[nDrops++] = 3;
        }
        if (nDrops == 0)
          return 0;
        switch (drops[rnd.Next(0, nDrops)]) {
          case 0: wSteep = wLeft; xSteep = xUp; return 2;
          case 1: wSteep = wLeft; xSteep = xDown; return 2;
          case 2: wSteep = wRight; xSteep = xUp; return 2;
          case 3: wSteep = wRight; xSteep = xDown; return 2;
        }
        return 0;
      }
    }
      #endregion

      #region Moore Deterministic, Downwind only (no backsliding upwind)
      public class FindSlopeMooreDeterministicDownwind : IFindSlope {
        public float[,] Elev;
        public int mWidth;
        public int mLength;

        public float slope;
        public bool OpenEnded = false;

        public void Init(ref float[,] Elev, int WidthAcross, int LenghtDownwind) {
          this.Elev = Elev;
          mWidth = WidthAcross - 1;
          mLength = LenghtDownwind - 1;
        }

        public void SetOpenEnded(bool NewState) {
          OpenEnded = NewState;
        }

        public int Upslope(int wCenter, int xCenter, out int wSteep, out int xSteep) {
          //  5 2 .
          //  1   .
          //  4 3 .
          int wLeft, wRight, xUp;
          wSteep = wCenter; xSteep = xCenter;
          xUp = 0;
          float h = Elev[wCenter, xCenter];  // first check Von Neumann neighbours
          if ((!OpenEnded || (xCenter > 0)) && ((Elev[wCenter, xUp = (xCenter - 1) & mLength] - h) >= 2)) {
            xSteep = xUp; return 2;
          }
          if ((Elev[wRight = (wCenter + 1) & mWidth, xCenter] - h) >= 2) {
            wSteep = wRight; return 2;
          }
          if ((Elev[wLeft = (wCenter - 1) & mWidth, xCenter] - h) >= 2) {
            wSteep = wLeft; return 2;
          }
          // now check diagonal neighbours
          if (!OpenEnded || (xCenter > 0)) {
            if ((Elev[wLeft, xUp] - h) >= 2) {
              wSteep = wLeft; xSteep = xUp; return 2;
            }
            if ((Elev[wRight, xUp] - h) >= 2) {
              wSteep = wRight; xSteep = xUp; return 2;
            }
          }
          return 0;
        }

        public int Downslope(int wCenter, int xCenter, out int wSteep, out int xSteep) {
          //  . 2 5
          //  .   1
          //  . 3 4
          int wLeft, wRight, xDown;
          wSteep = wCenter; xSteep = xCenter;
          xDown = 0;
          float h = Elev[wCenter, xCenter];
          if ((!OpenEnded || (xCenter != mLength)) && ((h - Elev[wCenter, xDown = (xCenter + 1) & mLength]) >= 2)) {
            xSteep = xDown; return 2;
          }
          if ((h - Elev[wRight = (wCenter + 1) & mWidth, xCenter]) >= 2) {
            wSteep = wRight; return 2;
          }
          if ((h - Elev[wLeft = (wCenter - 1) & mWidth, xCenter]) >= 2) {
            wSteep = wLeft; return 2;
          }
          // now check diagonal neighbours
          if (!OpenEnded || (xCenter != mLength)) {
            if ((h - Elev[wLeft, xDown]) >= 2) {
              wSteep = wLeft; xSteep = xDown; return 2;
            }
            if ((h - Elev[wRight, xDown]) >= 2) {
              wSteep = wRight; xSteep = xDown; return 2;
            }
          }
          return 0;
        }
      }
      #endregion

      #region Moore Stochastic, Downwind only (no upwind)
      public class FindSlopeMooreStochasticDownwind : IFindSlope {
        public float[,] Elev;
        public int mWidth;
        public int mLength;
        public bool OpenEnded = false;

        public float slope;
        protected Random rnd = new Random(123);

        public void Init(ref float[,] Elev, int WidthAcross, int LenghtDownwind) {
          this.Elev = Elev;
          mWidth = WidthAcross - 1;
          mLength = LenghtDownwind - 1;
        }

        public void SetOpenEnded(bool NewState) {
          OpenEnded = NewState;
        }

        public int Upslope(int wCenter, int xCenter, out int wSteep, out int xSteep) {
          //  + X .
          //  X   .
          //  + X .
          int[] rises = new int[4];
          int wLeft, wRight, xUp, nRises;
          wSteep = wCenter; xSteep = xCenter;
          xUp = nRises = 0;
          float h = Elev[wCenter, xCenter];
          if ((!OpenEnded || (xCenter > 0)) && ((Elev[wCenter, xUp = (xCenter - 1) & mLength] - h) >= 2))
            rises[nRises++] = 0;
          if ((Elev[wRight = (wCenter + 1) & mWidth, xCenter] - h) >= 2)
            rises[nRises++] = 1;
          if ((Elev[wLeft = (wCenter - 1) & mWidth, xCenter] - h) >= 2)
            rises[nRises++] = 3;
          if (nRises > 0)
            switch (rises[rnd.Next(0, nRises)]) {
              case 0: xSteep = xUp; return 2;
              case 1: wSteep = wRight; return 2;
              case 3: wSteep = wLeft; return 2;
            }
          // none of the Von Neumann cells qualified; how about diagonal neighbours?
          if (!OpenEnded || (xCenter > 0)) {
            if ((Elev[wLeft, xUp] - h) >= 2)
              rises[nRises++] = 0;
            if ((Elev[wRight, xUp] - h) >= 2)
              rises[nRises++] = 2;
          }
          if (nRises == 0)
            return 0;
          switch (rises[rnd.Next(0, nRises)]) {
            case 0: wSteep = wLeft; xSteep = xUp; return 2;
            case 2: wSteep = wRight; xSteep = xUp; return 2;
          }
          return 0;
        }

        public int Downslope(int wCenter, int xCenter, out int wSteep, out int xSteep) {
          //  . X X
          //  .   X
          //  . X X
          int[] drops = new int[4];
          int wLeft, wRight, xDown, nDrops;
          wSteep = wCenter; xSteep = xCenter;
          xDown = nDrops = 0;
          float h = Elev[wCenter, xCenter];
          if ((h - Elev[wRight = (wCenter + 1) & mWidth, xCenter]) >= 2)
            drops[nDrops++] = 1;
          if ((!OpenEnded || (xCenter != mLength)) && ((h - Elev[wCenter, xDown = (xCenter + 1) & mLength]) >= 2))
            drops[nDrops++] = 2;
          if ((h - Elev[wLeft = (wCenter - 1) & mWidth, xCenter]) >= 2)
            drops[nDrops++] = 3;
          if (nDrops > 0)
            switch (drops[rnd.Next(0, nDrops)]) {
              case 1: wSteep = wRight; return 2;
              case 2: xSteep = xDown; return 2;
              case 3: wSteep = wLeft; return 2;
            }
          // none of the Von Neumann cells qualified; how about diagonal neighbours?
          if (!OpenEnded || (xCenter != mLength)) {
            if ((h - Elev[wLeft, xDown]) >= 2)
              drops[nDrops++] = 1;
            if ((h - Elev[wRight, xDown]) >= 2)
              drops[nDrops++] = 3;
          }
          if (nDrops == 0)
            return 0;
          switch (drops[rnd.Next(0, nDrops)]) {
            case 1: wSteep = wLeft; xSteep = xDown; return 2;
            case 3: wSteep = wRight; xSteep = xDown; return 2;
          }
          return 0;
        }
      }
      #endregion

      #region Lateral avalanching only
      public class FindSlopeLateral : IFindSlope {
        public float[,] Elev;
        public int mWidth;
        public int mLength;
        public bool OpenEnded = false;

        public void Init(ref float[,] Elev, int WidthAcross, int LenghtDownwind) {
          this.Elev = Elev;
          mWidth = WidthAcross - 1;
          mLength = LenghtDownwind - 1;
        }

        public void SetOpenEnded(bool NewState) {
          OpenEnded = NewState;
        }

        public int Upslope(int wCenter, int xCenter, out int wSteep, out int xSteep) {
          //  . 2 .
          //  .   .
          //  . 1 .
          float maxHeight;
          maxHeight = Elev[(wCenter - 1) & mWidth, xCenter];
          xSteep = xCenter;
          wSteep = (wCenter - 1) & mWidth;
          if (Elev[(wCenter + 1) & mWidth, xCenter] > maxHeight) {
            xSteep = xCenter; wSteep = (wCenter + 1) & mWidth;
            maxHeight = Elev[wSteep, xSteep];
          }
          return (int)Math.Floor(maxHeight - Elev[wCenter, xCenter]);
        }

        public int Downslope(int wCenter, int xCenter, out int wSteep, out int xSteep) {
          //  . 2 .
          //  .   .
          //  . 1 .
          float minHeight;
          minHeight = Elev[(wCenter - 1) & mWidth, xCenter];
          xSteep = xCenter;
          wSteep = (wCenter - 1) & mWidth;
          if (Elev[(wCenter + 1) & mWidth, xCenter] < minHeight) {
            xSteep = xCenter; wSteep = (wCenter + 1) & mWidth;
            minHeight = Elev[wSteep, xSteep];
          }
          return (int)Math.Floor(Elev[wCenter, xCenter] - minHeight);
        }
      }
      #endregion
      */

  }
}
