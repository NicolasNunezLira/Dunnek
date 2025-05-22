using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using Unity.Mathematics;

namespace DunefieldModel_8D
{
    public class Model8D
    {
        public float[,] Elev;
        public float[,] Shadow;
        public int Width = 0;    // across wind
        public int Length = 0;
        public int HopLength = 1;
        //public float AverageHeight;
        public float pSand = 0.6f;
        public float pNoSand = 0.4f;
        public IFindSlope FindSlope;
        protected int mWidth, mLength;
        protected System.Random rnd = new System.Random(123);
        protected bool openEnded = false;
        public const float SHADOW_SLOPE = 0.803847577f;  //  3 * tan(15 degrees)

        public float depositeHeight = 1f;
        public float erosionHeight = 1f;

        public float slopeThreshold = 2f; // slope threshold for deposition

        public int grainsPerStep;

        public float slope;


        public int dx, dy;


        public Model8D(IFindSlope SlopeFinder, float[,] Elev, int Width, int Length, float slope, int dx, int dy)
        {
            FindSlope = SlopeFinder;
            this.Elev = Elev;
            this.slope = slope;
            this.dx = dx;
            this.dy = dy;
            this.Width = (int)Math.Pow(2, (int)Math.Log(Width, 2));
            this.Length = (int)Math.Pow(2, (int)Math.Log(Length, 2));
            mWidth = this.Width - 1;
            mLength = this.Length - 1;
            //Elev = new float[Width, Length];
            //Array.Clear(Elev, 0, Length * Width);
            Shadow = new float[Width, Length];
            Array.Clear(Shadow, 0, Length * Width);
            FindSlope.Init(ref this.Elev, Width, Length, this.slope);
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

        /*
        public void InitRandom(int AverageSandDepth)
        {
            bool saveOpenEnded = openEnded;
            openEnded = false;
            for (int i = AverageSandDepth * Length * Width; i > 0; i--)
                depositGrain(rnd.Next(0, Width), rnd.Next(0, Length));
            openEnded = saveOpenEnded;
            shadowInit();
            //AverageHeight = AveHeight();
        }

        public void InitUniform(int SandDepth)
        {
            for (int x = 0; x < Length; x++)
                for (int w = 0; w < Width; w++)
                    Elev[w, x] = SandDepth; // -(int)((float)x * ((float)SandDepth / 2.0) / ((float)Length)); ;
            shadowInit();
            //AverageHeight = AveHeight();
        }

        public void InitSquare(int SandDepth)
        {
            for (int x = 0; x < Length; x++)
                for (int w = 0; w < Width; w++)
                    Elev[w, x] = ((w > Width / 8) && (w < Width / 2)) ? SandDepth : 1;

            //AverageHeight = AveHeight();
        }

        public void InitDune(int SandDepth, int Width)
        {
            InitUniform(SandDepth);
            int h = 0;
            int x = Length / 4;
            for (int n = 0; n < 60; n++)
            {
                for (int w = (Width - Width) / 2; w < ((Width + Width) / 2); w++)
                    Elev[w, x + n] += h / 2;
                h++;
            }
            h /= 2;
            for (int n = 60; n < 75; n++)
            {
                for (int w = (Width - Width) / 2; w < ((Width + Width) / 2); w++)
                    Elev[w, x + n] += h;
                h -= 2;
            }
            shadowInit();
            //AverageHeight = AveHeight();
        }

        public void InitLinear(int MaxDepth)
        {
            for (int x = 0; x < Length; x++)
                for (int w = 0; w < Width; w++)
                    Elev[w, x] = x * MaxDepth / Length;
            shadowInit();
            //AverageHeight = AveHeight();
        }
        */

        public void InitCompletion()
        {
            shadowInit();
            //AverageHeight = AveHeight();
            Vector2 df = MinMaxHeight();
        }

        public float MaxHeight()
        {
            float maxH = 0f;
            for (int x = 0; x < Length; x++)
                for (int w = 0; w < Width; w++)
                    if (Elev[w, x] > maxH)
                        maxH = Elev[w, x];
            return maxH;
        }

        public Vector2 MinMaxHeight()
        {
            float maxH = 0;
            float minH = 100000;
            for (int x = 0; x < Length; x++)
                for (int w = 0; w < Width; w++)
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
            for (int x = 0; x < Length; x++)
                for (int w = 0; w < Width; w++)
                    sum += Elev[w, x];
            return sum;
        }

        public float[] Profile(int WidthPosition)
        {
            float[] prof = new float[Length];
            for (int x = 0; x < Length; x++)
                prof[x] = Elev[WidthPosition, x];
            return prof;
        }

        public void shadowInit()
        {
            shadowCheck(false, dx, dy);
        }
        protected int shadowCheck(bool ReportErrors, int dx, int dy)
        {
            float[,] newShadow = new float[Shadow.GetLength(0), Shadow.GetLength(1)];
            Array.Clear(newShadow, 0, newShadow.Length);

            int height = Elev.GetLength(0);
            int width = Elev.GetLength(1);
            int errors = 0;

            for (int w = 0; w < height; w++)
            {
                for (int x = 0; x < width; x++)
                {
                    float h = Elev[w, x];
                    if (h == 0) continue;

                    int wNext = w + dy;
                    int xNext = x + dx;

                    float hs = h;

                    while (IsInside(wNext, xNext, height, width) && hs >= Elev[wNext, xNext])
                    {
                        newShadow[wNext, xNext] = hs;
                        hs -= SHADOW_SLOPE;
                        wNext += dy;
                        xNext += dx;
                    }
                }
            }

            // Ajustar sombra si iguala altura
            for (int w = 0; w < height; w++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (newShadow[w, x] == Elev[w, x])
                        newShadow[w, x] = 0;
                }
            }

            // Comparar con sombra anterior
            for (int w = 0; w < height; w++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (newShadow[w, x] != Shadow[w, x])
                        errors++;
                }
            }

            if (errors > 0)
            {
                if (ReportErrors)
                    Console.WriteLine("shadowCheck error count: " + errors);
                Array.Copy(newShadow, Shadow, Shadow.Length);
            }

            return errors;
        }

        public virtual void erodeGrain(int w, int x, int dx, int dy, float erosionHeight = 1f)
        {
            int wSteep, xSteep;

            while (FindSlope.Upslope(w, x, out wSteep, out xSteep) >= 2)
            {
                // Si se sale del dominio en campo abierto
                if (openEnded && IsOutside(wSteep, xSteep))
                    return;

                w = wSteep;
                x = xSteep;
            }

            // Erosión
            Elev[w, x] -= erosionHeight;
            if (Elev[w, x] < 0) Elev[w, x] = 0;

            float h = Elev[w, x];
            float hs;

            int wPrev = w - dy;
            int xPrev = x - dx;

            if (openEnded && IsOutside(wPrev, xPrev))
                hs = h;
            else
            {
                (wPrev, xPrev) = WrapCoords(wPrev, xPrev);
                hs = Math.Max(h, Math.Max(Elev[wPrev, xPrev], Shadow[wPrev, xPrev]) - SHADOW_SLOPE);
            }

            int wNext = w;
            int xNext = x;

            while (true)
            {
                h = Elev[wNext, xNext];
                if (hs < h) break;

                Shadow[wNext, xNext] = (hs == h) ? 0 : hs;
                hs -= SHADOW_SLOPE;

                wNext += dy;
                xNext += dx;
                if (openEnded && IsOutside(wNext, xNext)) return;

                (wNext, xNext) = WrapCoords(wNext, xNext);
            }

            while (Shadow[wNext, xNext] > 0)
            {
                Shadow[wNext, xNext] = 0;
                wNext += dy;
                xNext += dx;
                if (openEnded && IsOutside(wNext, xNext)) return;

                (wNext, xNext) = WrapCoords(wNext, xNext);

                hs = h - SHADOW_SLOPE;
                if (Shadow[wNext, xNext] > hs)
                {
                    while (true)
                    {
                        h = Elev[wNext, xNext];
                        if (hs < h) break;

                        Shadow[wNext, xNext] = (hs == h) ? 0 : hs;
                        hs -= SHADOW_SLOPE;

                        wNext += dy;
                        xNext += dx;
                        if (openEnded && IsOutside(wNext, xNext)) return;

                        (wNext, xNext) = WrapCoords(wNext, xNext);
                    }
                }
            }
        }


        public virtual void depositGrain(int w, int x, int dx, int dy, float depositeHeight = 1f)
        {
            int wSteep, xSteep;

            while (FindSlope.Downslope(w, x, out wSteep, out xSteep) >= slopeThreshold)
            {
                if (openEnded &&
                    ((xSteep == mLength && x == 0) || (xSteep == 0 && x == mLength) ||
                    (wSteep == mWidth && w == 0) || (wSteep == 0 && w == mWidth)))
                    break;

                w = wSteep;
                x = xSteep;
            }

            Elev[w, x] += depositeHeight;
            float h = Elev[w, x];
            float hs;

            if (openEnded && (x == 0 || w == 0))
                hs = h;
            else
            {
                int ws = (w - dy + Width) % Width;
                int xs = (x - dx + Length) % Length;
                hs = Math.Max(h, Math.Max(Elev[ws, xs], Shadow[ws, xs]) - SHADOW_SLOPE);
            }

            while (hs >= (h = Elev[w, x]))
            {
                Shadow[w, x] = (hs == h) ? 0 : hs;
                hs -= SHADOW_SLOPE;

                w = (w + dy + Width) % Width;
                x = (x + dx + Length) % Length;

                if (openEnded && (x == 0 || w == 0))
                    return;
            }
        }



        public virtual void Tick(int grainsPerStep, int dx, int dy, float erosionHeight = 1f, float depositeHeight = 1f)
        {
            for (int subticks = grainsPerStep; subticks > 0; subticks--)
            {
                int x = rnd.Next(0, Length);
                int w = rnd.Next(0, Width);

                if (Elev[w, x] == 0) continue;
                if (Shadow[w, x] > 0) continue;

                erodeGrain(w, x, dx, dy, erosionHeight);

                int i = HopLength;
                int wCurr = w;
                int xCurr = x;

                while (true)
                {
                    wCurr = (wCurr + dy + Width) % Width;
                    xCurr = (xCurr + dx + Length) % Length;

                    if (openEnded && (xCurr < 0 || xCurr >= Length || wCurr < 0 || wCurr >= Width))
                        break;

                    if (Shadow[wCurr, xCurr] > 0)
                    {
                        depositGrain(wCurr, xCurr, dx, dy, depositeHeight);
                        break;
                    }

                    if (--i <= 0)
                    {
                        if (rnd.NextDouble() < (Elev[wCurr, xCurr] > 0 ? pSand : pNoSand))
                        {
                            depositGrain(wCurr, xCurr, dx, dy, depositeHeight);
                            break;
                        }
                        i = HopLength;
                    }
                }
            }

            shadowCheck(true, dx, dy);
        }


        public virtual int SaltationLength(int w, int x)
        {
            return HopLength;
        }

        public virtual int SpecialField(int w, int x)
        {
            return 0;
        }

        private bool IsInside(int w, int x, int height, int width)
        {
            return w >= 0 && w < height && x >= 0 && x < width;
        }
        
        private bool IsOutside(int w, int x)
        {
            return w < 0 || w >= Width || x < 0 || x >= Length;
        }

        private (int, int) WrapCoords(int w, int x)
        {
            w = (w + Width) % Width;
            x = (x + Length) % Length;
            return (w, x);
        }


    }
}
