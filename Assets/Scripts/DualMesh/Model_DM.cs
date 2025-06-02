using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using Unity.Collections;
using Unity.Mathematics;
using Unity.VisualScripting;
using ue=UnityEngine;

namespace DunefieldModel_DualMesh
{
    public class ModelDM
    {
        #region Variables
        public float[,] sandElev, terrainElev;
        public float[,] Shadow;

        public int Width = 0;    // across wind
        public int Length = 0;
        public int HopLength = 1;
        //public float AverageHeight;
        public float pSand = 0.6f;
        public float pNoSand = 0.4f;
        public IFindSlope FindSlope;
        protected int mWidth, mLength;
        protected System.Random rnd = new System.Random(42);
        protected bool openEnded = false;
        public const float SHADOW_SLOPE =  0.803847577f;  //  3 * tan(15 degrees) \approx 0.803847577f

        public float depositeHeight = .1f;
        public float erosionHeight = .1f;

        public float slopeThreshold = .2f; // slope threshold for deposition

        public int grainsPerStep;

        public float slope;


        public int dx, dz;

        private float erosionH, depositeH, aux;
        #endregion

        #region Init model
        public ModelDM(IFindSlope SlopeFinder, float[,] sandElev, float[,] terrainElev, int Width, int Length, float slope, int dx, int dz,
            float depositeHeight, float erosionHeight)
        {
            FindSlope = SlopeFinder;
            this.sandElev = sandElev;
            this.terrainElev = terrainElev;
            this.depositeHeight = depositeHeight;
            this.erosionHeight = erosionHeight;
            this.slope = slope;
            this.dx = dx;
            this.dz = dz;
            this.Width = Width;
            this.Length = Length;
            mWidth = this.Width - 1;
            mLength = this.Length - 1;
            Shadow = new float[Width, Length];
            Array.Clear(Shadow, 0, Length * Width);
            shadowInit();
            FindSlope.Init(ref sandElev, ref terrainElev, this.Width, this.Length, this.slope);
            FindSlope.SetOpenEnded(openEnded);
        }
        #endregion

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

        #region Shadows
        public void shadowInit()
        {
            shadowCheck(false, dx, dz);
        }
        protected int shadowCheck(bool ReportErrors, int dx, int dy)
        {
            float[,] newShadow = new float[Shadow.GetLength(0), Shadow.GetLength(1)];
            Array.Clear(newShadow, 0, newShadow.Length);

            int height = sandElev.GetLength(0);
            int width = sandElev.GetLength(1);
            int errors = 0;

            for (int w = 0; w < height; w++)
            {
                for (int x = 0; x < width; x++)
                {
                    float h = Math.Max(sandElev[w, x], terrainElev[w, x]);
                    if (h <= 0) continue;

                    int wNext = w + dy;
                    int xNext = x + dx;

                    float hs = h;

                    while (IsInside(wNext, xNext, height, width) && hs >= Math.Max(sandElev[wNext, xNext], terrainElev[wNext, xNext]))
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
                    if (newShadow[w, x] <= Math.Max(sandElev[w, x], terrainElev[w, x]))
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
        public virtual float erodeGrain(int w, int x, int dx, int dy, float erosionHeight)
        {
            int wSteep, xSteep;

            while (FindSlope.Upslope(w, x, dx, dy, out wSteep, out xSteep) >= 2)
            {
                // Si se sale del dominio en campo abierto
                if (openEnded && IsOutside(wSteep, xSteep))
                    return 0f;

                w = wSteep;
                x = xSteep;
            }

            if (terrainElev[w,x] >= sandElev[w,x]) return 0f;


            erosionH = Math.Min(erosionHeight, sandElev[w, x] - terrainElev[w, x]);
            // Erosión
            sandElev[w, x] -= erosionH;

            float h = sandElev[w, x];
            float hs;

            int wPrev = w - dy;
            int xPrev = x - dx;

            if (openEnded && IsOutside(wPrev, xPrev))
                hs = h;
            else
            {
                (wPrev, xPrev) = WrapCoords(wPrev, xPrev);
                hs = Math.Max(h, Math.Max(Math.Max(sandElev[wPrev, xPrev], terrainElev[wPrev, xPrev]), Shadow[wPrev, xPrev]) - SHADOW_SLOPE);
            }

            int wNext = w;
            int xNext = x;

            while (true)
            {
                h = Math.Max(sandElev[wNext, xNext], terrainElev[wNext, xNext]);
                if (hs < h) break;

                Shadow[wNext, xNext] = (hs == h) ? 0 : hs;
                hs -= SHADOW_SLOPE;

                wNext += dy;
                xNext += dx;
                if (openEnded && IsOutside(wNext, xNext)) return 0f;

                (wNext, xNext) = WrapCoords(wNext, xNext);
            }

            while (Shadow[wNext, xNext] > 0)
            {
                Shadow[wNext, xNext] = 0;
                wNext += dy;
                xNext += dx;
                if (openEnded && IsOutside(wNext, xNext)) return 0f;

                (wNext, xNext) = WrapCoords(wNext, xNext);

                hs = h - SHADOW_SLOPE;
                if (Shadow[wNext, xNext] > hs)
                {
                    while (true)
                    {
                        h = Math.Max(terrainElev[wNext, xNext], sandElev[wNext, xNext]);
                        if (hs < h) break;

                        Shadow[wNext, xNext] = (hs == h) ? 0 : hs;
                        hs -= SHADOW_SLOPE;

                        wNext += dy;
                        xNext += dx;
                        if (openEnded && IsOutside(wNext, xNext)) return 0f;

                        (wNext, xNext) = WrapCoords(wNext, xNext);
                    }
                }
            }

            return erosionH;
        }

        #endregion

        #region Deposit Grain


        public virtual void depositGrain(int w, int x, int dx, int dy, float depositeHeight)
        {
            int wLow, xLow;

            while (FindSlope.Downslope(w, x, dx, dy, out wLow, out xLow) >= 2)
            {
                if (openEnded &&
                    ((xLow == mLength && x == 0) || (xLow == 0 && x == mLength) ||
                    (wLow == mWidth && w == 0) || (wLow == 0 && w == mWidth)))
                    break;

                w = wLow;
                x = xLow;
            }

            if (terrainElev[w, x] >= sandElev[w, x] + depositeHeight) return;
            if (terrainElev[w, x] >= sandElev[w, x])
            {
                sandElev[w, x] = terrainElev[w, x] + depositeHeight;
            }
            else
            {
                sandElev[w, x] += depositeHeight;
            }
            

            float h = Math.Max(sandElev[w, x], terrainElev[w, x]);
            float hs;

            if (openEnded && (x == 0 || w == 0))
                hs = h;
            else
            {
                int ws = (w - dy + Width) % Width;
                int xs = (x - dx + Length) % Length;
                hs = Math.Max(h, Math.Max(Math.Max(terrainElev[ws, xs], sandElev[ws, xs]), Shadow[ws, xs]) - SHADOW_SLOPE);
            }

            while (hs >= (h = Math.Max(sandElev[w, x], terrainElev[w,x])))
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
            if (verbose)
            {
                int count1 = 0;
                int count2 = 0;
                int count3 = 0;
                for (int i = 0; i < sandElev.GetLength(0); i++)
                {
                    for (int j = 0; j < sandElev.GetLength(1); j++)
                    {
                        if ((sandElev[i, j] - terrainElev[i, j]) > 0) count1++;
                        if (Shadow[i, j] <= 0) count2++;
                        if ((sandElev[i, j] - terrainElev[i, j] > 0) && Shadow[i, j] <= 0) count3++;
                    }
                }
                ue.Debug.Log("Cantidad de nodos erosionables:" + count1);
                ue.Debug.Log("Cantidad de nodos sin sombra:" + count2);
                ue.Debug.Log("Cantidad de nodos erosionables sin sombra:" + count3);
            }

            int count = 0;
            for (int subticks = grainsPerStep; subticks > 0; subticks--)
            {
                int x = rnd.Next(0, Length);
                int w = rnd.Next(0, Width);

                if (Math.Max(sandElev[w,x], terrainElev[w,x]) <= 0)
                {
                    if (verbose) { ue.Debug.Log("Grano (" + w + "," + x + ") sin altura."); }
                    ;
                    continue;
                }
                if (Shadow[w, x] > 0 || terrainElev[w, x] >= sandElev[w, x])
                {
                    if (verbose) { ue.Debug.Log("Grano (" + w + "," + x + ") en sombra o terreno elevado."); }
                    ;
                    continue;
                }



                if (verbose) { ue.Debug.Log("Grano a erosionar en (" + w + "," + x + ")."); }
                aux = erodeGrain(w, x, dx, dy, erosionHeight);
                depositeH = (aux > 0) ? aux : depositeHeight;
                
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
                        depositGrain(wCurr, xCurr, dx, dy, depositeH);
                        break;
                    }

                    if (--i <= 0)
                    {
                        if (rnd.NextDouble() < (sandElev[wCurr, xCurr] > terrainElev[wCurr, xCurr] ? pSand : pNoSand))
                        {
                            depositGrain(wCurr, xCurr, dx, dy, depositeH);
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