using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using Unity.Collections;
using Unity.Mathematics;
using ue=UnityEngine;

namespace DunefieldModel_DualMesh
{
    public class ModelDM
    {
        #region Variables
        public float[,] sandElev, terrainElev, Elev;
        public float[,] terrainShadow, Shadow;

        public bool[,] isErodable;
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

        private float[,] fixedShadow = null;

        private float offSet;
        #endregion

        #region Init model
        public ModelDM(IFindSlope SlopeFinder, float[,] sandElev, float[,] terrainElev, int Width, int Length, float slope, int dx, int dy)
        {
            FindSlope = SlopeFinder;
            this.sandElev = sandElev;
            this.terrainElev = terrainElev;
            // Búsqueda del offset y vertices erosionables iniciales
            offSet = 0;
            Elev = new float[Width, Length];
            isErodable = new bool[Width, Length];
            for (int x = 0; x < sandElev.GetLength(0); x++)
            {
                for (int y = 0; y < sandElev.GetLength(1); y++)
                {
                    float h = sandElev[x, y] - terrainElev[x, y];
                    isErodable[x, y] = h >= 0;
                    if (isErodable[x, y]) { offSet = Math.Min(offSet, sandElev[x, y]); }
                    ;
                    Elev[x, y] = Math.Max(sandElev[x, y], terrainElev[x, y]);
                }
            }
            this.slope = slope;
            this.dx = dx;
            this.dy = dy;
            this.Width = (int)Math.Pow(2, (int)Math.Log(Width, 2));
            this.Length = (int)Math.Pow(2, (int)Math.Log(Length, 2));
            mWidth = this.Width - 1;
            mLength = this.Length - 1;
            Shadow = new float[Width, Length];
            Array.Clear(Shadow, 0, Length * Width);
            FindSlope.Init(ref this.Elev, Width, Length, this.slope);
            FindSlope.SetOpenEnded(openEnded);
        }
        #endregion

        #region Shadows
        public void shadowInit()
        {
            shadowCheck(true, dx, dy);
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
        #endregion

        #region Erode Grain
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
            sandElev[w, x] -= erosionHeight;
            if (sandElev[w, x] < 0) sandElev[w, x] = 0;

            float h = sandElev[w, x];
            float hs;

            int wPrev = w - dy;
            int xPrev = x - dx;

            if (openEnded && IsOutside(wPrev, xPrev))
                hs = h;
            else
            {
                (wPrev, xPrev) = WrapCoords(wPrev, xPrev);
                hs = Math.Max(h, Math.Max(sandElev[wPrev, xPrev], Shadow[wPrev, xPrev]) - SHADOW_SLOPE);
            }

            int wNext = w;
            int xNext = x;

            while (true)
            {
                h = sandElev[wNext, xNext];
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
                        h = sandElev[wNext, xNext];
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

        #endregion

        #region Deposit Grain


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
            float h = sandElev[w, x];
            float hs;

            if (openEnded && (x == 0 || w == 0))
                hs = h;
            else
            {
                int ws = (w - dy + Width) % Width;
                int xs = (x - dx + Length) % Length;
                hs = Math.Max(h, Math.Max(sandElev[ws, xs], Shadow[ws, xs]) - SHADOW_SLOPE);
            }

            while (hs >= (h = sandElev[w, x]))
            {
                Shadow[w, x] = (hs == h) ? 0 : hs;
                hs -= SHADOW_SLOPE;

                w = (w + dy + Width) % Width;
                x = (x + dx + Length) % Length;

                if (openEnded && (x == 0 || w == 0))
                    return;
            }
        }

        #endregion

        #region Tick



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
        #endregion

        #region Auxiliar functions


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
        
        #endregion


    }
}