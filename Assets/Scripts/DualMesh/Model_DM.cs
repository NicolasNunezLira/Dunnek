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
        public float[,] sandElev, terrainElev, surfaceElev;
        public float[,] Shadow;

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
        public const float SHADOW_SLOPE = 10;  //  3 * tan(15 degrees) \approx 0.803847577f

        public float depositeHeight;
        public float erosionHeight;

        public float slopeThreshold = 2f; // slope threshold for deposition

        public int grainsPerStep;

        public float slope;


        public int dx, dy;

        private float[,] fixedShadow = null;

        //private float offSet;
        #endregion

        #region Init model
        public ModelDM(IFindSlope SlopeFinder, float[,] sandElev, float[,] terrainElev, int Width, int Length, float slope, int dx, int dy,
            float depositeHeight, float erosionHeight)
        {
            FindSlope = SlopeFinder;
            this.sandElev = sandElev;
            this.terrainElev = terrainElev;
            this.depositeHeight = depositeHeight;
            this.erosionHeight = erosionHeight;
            // Búsqueda del offset y vertices erosionables iniciales
            //offSet = 0;
            surfaceElev = new float[Width, Length];
            isErodable = new bool[Width, Length];
            for (int x = 0; x < sandElev.GetLength(0); x++)
            {
                for (int y = 0; y < sandElev.GetLength(1); y++)
                {
                    //float h = sandElev[x, y] - terrainElev[x, y];
                    isErodable[x, y] = sandElev[x,y] > terrainElev[x,y];
                    //if (isErodable[x, y]) { offSet = Math.Min(offSet, sandElev[x, y]); };
                    surfaceElev[x, y] = Math.Max(sandElev[x, y], terrainElev[x, y]);
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
            shadowInit();
            FindSlope.Init(ref surfaceElev, Width, Length, this.slope);
            FindSlope.SetOpenEnded(openEnded);
        }
        #endregion

        #region Shadows
        public void shadowInit()
        {
            shadowCheck(false, dx, dy);
        }
        protected int shadowCheck(bool ReportErrors, int dx, int dy)
        {
            float[,] newShadow = new float[Shadow.GetLength(0), Shadow.GetLength(1)];
            Array.Clear(newShadow, 0, newShadow.Length);

            int height = surfaceElev.GetLength(0);
            int width = surfaceElev.GetLength(1);
            int errors = 0;

            for (int w = 0; w < height; w++)
            {
                for (int x = 0; x < width; x++)
                {
                    float h = surfaceElev[w, x];
                    if (h == 0) continue;

                    int wNext = w + dy;
                    int xNext = x + dx;

                    float hs = h;

                    while (IsInside(wNext, xNext, height, width) && hs >= surfaceElev[wNext, xNext])
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
                    if (newShadow[w, x] <= surfaceElev[w, x])
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
        public virtual void erodeGrain(int w, int x, int dx, int dy, float erosionHeight)
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

            if (!isErodable[w, x]) return;

            // Erosión
            sandElev[w, x] -= erosionHeight;
            surfaceElev[w, x] -= erosionHeight;
            if (sandElev[w, x] < terrainElev[w, x])
            {
                //sandElev[w, x] = terrainElev[w, x];
                surfaceElev[w, x] = terrainElev[w, x];
                isErodable[w, x] = false;
            }

            float h = sandElev[w, x];
            float hs;

            int wPrev = w - dy;
            int xPrev = x - dx;

            if (openEnded && IsOutside(wPrev, xPrev))
                hs = h;
            else
            {
                (wPrev, xPrev) = WrapCoords(wPrev, xPrev);
                hs = Math.Max(h, Math.Max(surfaceElev[wPrev, xPrev], Shadow[wPrev, xPrev]) - SHADOW_SLOPE);
            }

            int wNext = w;
            int xNext = x;

            while (true)
            {
                h = surfaceElev[wNext, xNext];
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
                        h = surfaceElev[wNext, xNext];
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


        public virtual void depositGrain(int w, int x, int dx, int dy, float depositeHeight)
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

            surfaceElev[w, x] += depositeHeight;
            sandElev[w,x] +=depositeHeight;
            //sandElev[w, x] = depositeHeight + (isErodable[w, x] ? sandElev[w, x] : terrainElev[w, x]);
            isErodable[w, x] = true;

            float h = surfaceElev[w, x];
            float hs;

            if (openEnded && (x == 0 || w == 0))
                hs = h;
            else
            {
                int ws = (w - dy + Width) % Width;
                int xs = (x - dx + Length) % Length;
                hs = Math.Max(h, Math.Max(surfaceElev[ws, xs], Shadow[ws, xs]) - SHADOW_SLOPE);
            }

            while (hs >= (h = surfaceElev[w, x]))
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



        public virtual void Tick(int grainsPerStep, int dx, int dy, float erosionHeight, float depositeHeight, bool verbose = false)
        {
            int count = 0;
            for (int i = 0; i < isErodable.GetLength(0); i++)
            {
                for (int j = 0; j < isErodable.GetLength(1); j++)
                {
                    if (isErodable[i, j]) count++;
                }
            }
            ue.Debug.Log("Cantidad de nodos erosionables:" + count);

            count = 0;
            for (int subticks = grainsPerStep; subticks > 0; subticks--)
            {
                int x = rnd.Next(0, Length);
                int w = rnd.Next(0, Width);

                if (surfaceElev[w, x] == 0)
                {
                    if (verbose) { ue.Debug.Log("Grano (" + w + "," + x + ") sin altura."); }
                    ;
                    continue;
                }
                if (Shadow[w, x] > 0)
                {
                    if (verbose) { ue.Debug.Log("Grano (" + w + "," + x + ") en sombra."); }
                    ;
                    continue;
                }
                /*
                if (!isErodable[w, x])
                {
                    if (verbose) { ue.Debug.Log("Grano (" + w + "," + x + ") no erosionable."); }
                    ;
                    continue;
                }
                */

                if (verbose) { ue.Debug.Log("Grano a erosionar en (" + w + "," + x + ")."); }
                erodeGrain(w, x, dx, dy, erosionHeight);
                
                count++;

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
                        if (verbose) { ue.Debug.Log("Grano a depositar en (" + wCurr + "," + xCurr + ")."); }
                        depositGrain(wCurr, xCurr, dx, dy, depositeHeight);
                        break;
                    }

                    if (--i <= 0)
                    {
                        if (rnd.NextDouble() < (surfaceElev[wCurr, xCurr] > terrainElev[wCurr, xCurr] ? pSand : pNoSand))
                        {
                            depositGrain(wCurr, xCurr, dx, dy, depositeHeight);
                            if (verbose) { ue.Debug.Log("Grano a depositar en (" + wCurr + "," + xCurr + ")."); }
                            break;
                        }
                        i = HopLength;
                    }
                }
            }
            ue.Debug.Log("Granos erosionados en este tick:" + count + "/" + grainsPerStep);

            //shadowCheck(true, dx, dy);
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