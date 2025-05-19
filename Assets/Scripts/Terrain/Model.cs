using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;

namespace DunefieldModel
{
    public class Model
    {
        public float[,] Elev;
        public float[,] Shadow;
        public int WidthAcross = 0;    // across wind
        public int LengthDownwind = 0;
        public int HopLength = 1;
        //public float AverageHeight;
        public float pSand = 0.6f;
        public float pNoSand = 0.4f;
        public IFindSlope FindSlope;
        protected int mWidth, mLength;
        protected Random rnd = new Random(123);
        protected bool openEnded = false;
        public const float SHADOW_SLOPE = 0.803847577f;  //  3 * tan(15 degrees)


        public Model(IFindSlope SlopeFinder, int WidthAcross, int LengthDownwind)
        {
            FindSlope = SlopeFinder;
            this.WidthAcross = (int)Math.Pow(2, (int)Math.Log(WidthAcross, 2));
            this.LengthDownwind = (int)Math.Pow(2, (int)Math.Log(LengthDownwind, 2));
            mWidth = this.WidthAcross - 1;
            mLength = this.LengthDownwind - 1;
            Elev = new float[WidthAcross, LengthDownwind];
            Array.Clear(Elev, 0, LengthDownwind * WidthAcross);
            Shadow = new float[WidthAcross, LengthDownwind];
            Array.Clear(Shadow, 0, LengthDownwind * WidthAcross);
            FindSlope.Init(ref Elev, WidthAcross, LengthDownwind);
            FindSlope.SetOpenEnded(openEnded);
        }

        public virtual bool UsesHopLength()
        {
            return true;  // does this model use the user-provided value of hop length?
        }

        public virtual bool UsesSandProbabilities()
        {
            return true;  // does this model use the user-provided values of sand depositing probabilities?
        }

        public void SetOpenEnded(bool NewState)
        {  // 'true' means dunefield is open-ended (no wrapping)
            openEnded = NewState;
            FindSlope.SetOpenEnded(openEnded);
        }

        public void InitRandom(int AverageSandDepth)
        {
            bool saveOpenEnded = openEnded;
            openEnded = false;
            for (int i = AverageSandDepth * LengthDownwind * WidthAcross; i > 0; i--)
                depositGrain(rnd.Next(0, WidthAcross), rnd.Next(0, LengthDownwind));
            openEnded = saveOpenEnded;
            shadowInit();
            //AverageHeight = AveHeight();
        }

        public void InitUniform(int SandDepth)
        {
            for (int x = 0; x < LengthDownwind; x++)
                for (int w = 0; w < WidthAcross; w++)
                    Elev[w, x] = SandDepth; // -(int)((float)x * ((float)SandDepth / 2.0) / ((float)LengthDownwind)); ;
            shadowInit();
            //AverageHeight = AveHeight();
        }

        public void InitSquare(int SandDepth)
        {
            for (int x = 0; x < LengthDownwind; x++)
                for (int w = 0; w < WidthAcross; w++)
                    Elev[w, x] = ((w > WidthAcross / 8) && (w < WidthAcross / 2)) ? SandDepth : 1;

            //AverageHeight = AveHeight();
        }

        public void InitDune(int SandDepth, int Width)
        {
            InitUniform(SandDepth);
            int h = 0;
            int x = LengthDownwind / 4;
            for (int n = 0; n < 60; n++)
            {
                for (int w = (WidthAcross - Width) / 2; w < ((WidthAcross + Width) / 2); w++)
                    Elev[w, x + n] += h / 2;
                h++;
            }
            h /= 2;
            for (int n = 60; n < 75; n++)
            {
                for (int w = (WidthAcross - Width) / 2; w < ((WidthAcross + Width) / 2); w++)
                    Elev[w, x + n] += h;
                h -= 2;
            }
            shadowInit();
            //AverageHeight = AveHeight();
        }

        public void InitLinear(int MaxDepth)
        {
            for (int x = 0; x < LengthDownwind; x++)
                for (int w = 0; w < WidthAcross; w++)
                    Elev[w, x] = x * MaxDepth / LengthDownwind;
            shadowInit();
            //AverageHeight = AveHeight();
        }

        public void InitCompletion()
        {
            shadowInit();
            //AverageHeight = AveHeight();
            Vector2 df = MinMaxHeight();
        }

        public float MaxHeight()
        {
            float maxH = 0f;
            for (int x = 0; x < LengthDownwind; x++)
                for (int w = 0; w < WidthAcross; w++)
                    if (Elev[w, x] > maxH)
                        maxH = Elev[w, x];
            return maxH;
        }

        public Vector2 MinMaxHeight()
        {
            float maxH = 0;
            float minH = 100000;
            for (int x = 0; x < LengthDownwind; x++)
                for (int w = 0; w < WidthAcross; w++)
                {
                    if (Elev[w, x] > maxH)
                        maxH = Elev[w, x];
                    if (Elev[w, x] < minH)
                        minH = Elev[w, x];
                }
            /*
            Range r = new Range();
            r.Min = minH;
            r.Max = maxH;
            return r;
            */
            return new Vector2(minH, maxH);
        }

        public float Count()
        {
            float sum = 0;
            for (int x = 0; x < LengthDownwind; x++)
                for (int w = 0; w < WidthAcross; w++)
                    sum += Elev[w, x];
            return sum;
        }

        public float[] Profile(int WidthPosition)
        {
            float[] prof = new float[LengthDownwind];
            for (int x = 0; x < LengthDownwind; x++)
                prof[x] = Elev[WidthPosition, x];
            return prof;
        }

        public void shadowInit()
        {
            shadowCheck(false);
        }

        protected int shadowCheck(bool ReportErrors)
        {  // returns num of fixes
            // Rules:
            // - Shadows start from the downwind edge of slab
            // - A slab is in shadow if its edge is in shadow
            // - If the top slab of a stack not in shadow, that stack is a new peak
            // - If a stack has no accommodation space, it is zero; otherwise it is height of shadow
            float[,] newShadow = new float[Shadow.GetLength(0), Shadow.GetLength(1)];
            Array.Clear(newShadow, 0, newShadow.Length);
            int xs;
            float h;
            float hs;
            for (int w = 0; w < WidthAcross; w++)
                for (int x = 0; x < LengthDownwind; x++)
                {
                    h = Elev[w, x];
                    if (h == 0) continue;
                    hs = Math.Max(((float)h), newShadow[w, (x - 1) & mLength] - SHADOW_SLOPE);
                    xs = x;
                    while (hs >= ((float)Elev[w, xs]))
                    {
                        newShadow[w, xs] = hs;
                        hs -= SHADOW_SLOPE;
                        xs = (xs + 1) & mLength;
                    }
                }
            for (int x = 0; x < LengthDownwind; x++)
                for (int w = 0; w < WidthAcross; w++)
                    if (newShadow[w, x] == ((float)Elev[w, x]))
                        newShadow[w, x] = 0;
            int errors = 0;
            for (int x = 0; x < LengthDownwind; x++)
                for (int w = 0; w < WidthAcross; w++)
                    if (newShadow[w, x] != Shadow[w, x])
                        errors++;
            if (errors > 0)
            {
                if (ReportErrors)
                    Console.WriteLine("shadowCheck error count: " + errors);
                Array.Copy(newShadow, Shadow, Shadow.Length);
            }
            for (int x = 0; x < LengthDownwind; x++)
                for (int w = 0; w < WidthAcross; w++)
                    if ((Shadow[w, x] > 0) && (Shadow[w, x] < Elev[w, x]))
                        continue;  // bug -- should never get here
            return errors;
        }

        public virtual void erodeGrain(int w, int x)
        {
            int wSteep, xSteep;
            while (FindSlope.Upslope(w, x, out wSteep, out xSteep) >= 2)
            {
                if (openEnded && (((xSteep == mLength) && (x == 0)) || ((xSteep == 0) && (x == mLength))))
                    return;  // erosion happens off-field
                w = wSteep;
                x = xSteep;
            }
            float h = --Elev[w, x];
            float hs;
            if (openEnded && (x == 0))
                hs = h;
            else
            {
                int xs = (x - 1) & mLength;
                hs = Math.Max(h, Math.Max(Elev[w, xs], Shadow[w, xs]) - SHADOW_SLOPE);
            }
            while (hs >= (h = ((float)Elev[w, x])))
            {
                Shadow[w, x] = (hs == h) ? 0 : hs;
                hs -= SHADOW_SLOPE;
                x = (x + 1) & mLength;
                if (openEnded && (x == 0))
                    return;
            }
            while (Shadow[w, x] > 0)
            {
                Shadow[w, x] = 0;
                x = (x + 1) & mLength;
                if (openEnded && (x == 0))
                    return;
                hs = h - SHADOW_SLOPE;
                if (Shadow[w, x] > hs)
                    while (hs >= (h = ((float)Elev[w, x])))
                    {
                        Shadow[w, x] = (hs == h) ? 0 : hs;
                        hs -= SHADOW_SLOPE;
                        x = (x + 1) & mLength;
                        if (openEnded && (x == 0))
                            return;
                    }
            }
        }

        public virtual void depositGrain(int w, int x)
        {
            int xSteep, wSteep;
            while (FindSlope.Downslope(w, x, out wSteep, out xSteep) >= 2)
            {
                if (openEnded && (((xSteep == mLength) && (x == 0)) || ((xSteep == 0) && (x == mLength))))
                    break;  // deposit happens at boundary, to keep grains from rolling off
                w = wSteep;
                x = xSteep;
            }
            float h = ++Elev[w, x];
            float hs;
            if (openEnded && (x == 0))
                hs = h;
            else
            {
                int xs = (x - 1) & mLength;
                hs = Math.Max(h, Math.Max(Elev[w, xs], Shadow[w, xs]) - SHADOW_SLOPE);
            }
            while (hs >= (h = ((float)Elev[w, x])))
            {
                Shadow[w, x] = (hs == h) ? 0 : hs;
                hs -= SHADOW_SLOPE;
                x = (x + 1) & mLength;
                if (openEnded && (x == 0))
                    return;
            }
        }

        public virtual void Tick()
        {
            for (int subticks = LengthDownwind * WidthAcross; subticks > 0; subticks--)
            {
                int x = rnd.Next(0, LengthDownwind);
                int w = rnd.Next(0, WidthAcross);
                if (Elev[w, x] == 0) continue;
                if (Shadow[w, x] > 0) continue;
                erodeGrain(w, x);
                int i = HopLength;
                while (true)
                {
                    if (++x >= LengthDownwind)
                    {
                        if (openEnded)
                            break;
                        x &= mLength;
                    }
                    if (Shadow[w, x] > 0)
                    {
                        depositGrain(w, x);
                        break;
                    }
                    if (--i <= 0)
                    {
                        if (rnd.NextDouble() < (Elev[w, x] > 0 ? pSand : pNoSand))
                        {
                            depositGrain(w, x);
                            Debug.WriteLine("Depositing grain at " + w + ", " + x);
                            break;
                        }
                        i = HopLength;
                    }
                }
            }
            shadowCheck(true);
        }
        
        public virtual int SaltationLength(int w, int x) {
            return HopLength;
        }

        public virtual int SpecialField(int w, int x) {
            return 0;
        }

    }
}
