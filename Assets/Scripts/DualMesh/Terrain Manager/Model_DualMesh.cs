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
    public class ModelDualMesh
    {
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


        public ModelDualMesh(IFindSlope SlopeFinder, float[,] sandElev, float[,] terrainElev, int Width, int Length, float slope, int dx, int dy)
        {
            FindSlope = SlopeFinder;
            this.sandElev = sandElev;
            this.terrainElev = terrainElev;
            // Búsqueda del offset y vertices erosionables iniciales
            offSet = 0;
            for (int x = 0; x < sandElev.GetLength(0); x++)
            {
                for (int y = 0; y < sandElev.GetLength(1); y++)
                {
                    float h = sandElev[x, y] - terrainElev[x, y];
                    isErodable[x, y] = h >= 0;
                    if (isErodable[x,y]) { offSet = Math.Min(offSet, sandElev[x,y]); };
                }
            }
            // áltura de arena sobre el offset
            for (int x = 0; x < sandElev.GetLength(0); x++)
            {
                for (int y = 0; y < sandElev.GetLength(1); y++)
                {
                    Elev[x, y] = isErodable[x, y] ? sandElev[x, y] : terrainElev[x,y];
                }
            }
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
            FindSlope.Init(ref sandElev, ref terrainElev, Width, Length, this.slope);
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

        /*
        public float[] Profile(int WidthPosition)
        {
            float[] prof = new float[Length];
            for (int x = 0; x < Length; x++)
                prof[x] = Elev[WidthPosition, x];
            return prof;
        }
        */

        #region Sombras
        public void shadowInit()
        {
            shadowCheck(false, dx, dy);
        }

        protected void computeFixedTerrainShadow(int dx, int dy)
        {
            int height = terrainElev.GetLength(0);
            int width = terrainElev.GetLength(1);
            terrainShadow = new float[height, width];
                    

            for (int w = 0; w < height; w++)
            {
                for (int x = 0; x < width; x++)
                {
                    float h = (terrainElev[w, x] >= sandElev[w, x]) ? terrainElev[w, x] : 0f;

                    if (h <= 0) continue; // No proyecta sombra si no hay obstáculo

                    float hs = h;
                    int wNext = w + dy;
                    int xNext = x + dx;

                    while (IsInside(wNext, xNext, height, width))
                    {
                        // Si la sombra proyectada aún está sobre la arena, se guarda
                        if (hs > sandElev[wNext, xNext])
                            terrainShadow[wNext, xNext] = Math.Max(terrainShadow[wNext, xNext], hs);
                        else
                            break; // La sombra "choca" con la arena

                        hs -= SHADOW_SLOPE;

                        // Cortar si se baja del terreno
                        if (hs <= 0)
                            break;

                        wNext += dy;
                        xNext += dx;
                    }
                }
            }

            //return Shadow;
        }



        protected int shadowCheck(bool ReportErrors, int dx, int dy)
        {
            if (terrainShadow == null)
            {
                computeFixedTerrainShadow(dx, dy); // solo 1 vez
                Shadow = (float[,])terrainShadow.Clone(); // Inicializar sombra activa
                if (ReportErrors)
                    Console.WriteLine("Fixed shadow computed.");
                return 1;
            }

            // Ya no se recalcula sombra, se usa fixedShadow directamente
            int errors = 0;
            int height = Elev.GetLength(0);
            int width = Elev.GetLength(1);

            for (int w = 0; w < height; w++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (Shadow[w, x] != fixedShadow[w, x])
                    {
                        Shadow[w, x] = fixedShadow[w, x];
                        errors++;
                    }
                }
            }

            if (errors > 0 && ReportErrors)
                Console.WriteLine("Shadow mismatch fixed. Errors: " + errors);

            return errors;
        }

        void UpdateShadowFromPoint(int startW, int startX, int dx, int dy)
        {
            int height = Elev.GetLength(0);
            int width = Elev.GetLength(1);

            // Dirección ortogonal para expansión lateral opcional
            
            int orthX = -dy;
            int orthY = dx;
            
            int w = startW;
            int x = startX;

            float sourceHeight = ue.Mathf.Max(sandElev[w, x], terrainElev[w, x]); // la mayor de ambas

            float hs = sourceHeight;
            int spread = 0;

            w += dy;
            x += dx;

            while (IsInside(w, x, height, width))
            {
                float terrainH = terrainElev[w, x];
                float elevH = Elev[w, x];

                // Si terreno fijo es más alto, reempezar sombra desde él
                if (terrainH > hs)
                {
                    hs = terrainH;
                    spread = 0;
                }
                else if (elevH > hs)
                {
                    hs = elevH;
                    spread = 0;
                }

                // Si sombra es más baja que la superficie, ya no hay sombra
                if (hs < ue.Mathf.Max(terrainH, elevH))
                    break;

                // Rango de sombra lateralmente (opcional)
                int lateralRange = ue.Mathf.Max(1, 3 - spread / 8);

                for (int offset = -lateralRange; offset <= lateralRange; offset++)
                {
                    int wl = w + offset * orthY;
                    int xl = x + offset * orthX;

                    if (IsInside(wl, xl, height, width))
                    {
                        Shadow[wl, xl] = ue.Mathf.Max(Shadow[wl, xl], hs);
                    }
                }

                hs -= SHADOW_SLOPE;
                spread++;
                w += dy;
                x += dx;
            }
        }


        #endregion

        #region Erosion
        /*
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
        */
        public virtual void erodeGrain(int w, int x, int dx, int dy, float erosionHeight = 1f)
        {
            int wSteep, xSteep;

            while (FindSlope.Upslope(w, x, dx, dy, out wSteep, out xSteep) >= 2)
            {
                if (openEnded && IsOutside(wSteep, xSteep))
                    return;

                w = wSteep;
                x = xSteep;
            }

            // Evitar erosión si hay obstáculo del terreno fijo
            if (terrainElev[w, x] >= sandElev[w, x]) return;

            // Erosión
            sandElev[w, x] -= erosionHeight;
            if (sandElev[w, x] < terrainElev[w,x]) sandElev[w, x] = terrainElev[w,x];

            float h = sandElev[w, x];
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
                h = sandElev[wNext, xNext];

                // 
                // Detener sombra si hay obstáculo por terreno fijo
                if (hs < h || terrainElev[wNext, xNext] > sandElev[wNext, xNext])
                    break;

                Shadow[wNext, xNext] = (hs <= h) ? 0 : hs;
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

                // Verificar si ya no se puede proyectar más
                if (Shadow[wNext, xNext] > hs || terrainElev[wNext, xNext] > Elev[wNext, xNext])
                    break;

                while (true)
                {
                    h = Elev[wNext, xNext];
                    if (hs < h || terrainElev[wNext, xNext] > Elev[wNext, xNext])
                        break;

                    Shadow[wNext, xNext] = (hs == h) ? 0 : hs;
                    hs -= SHADOW_SLOPE;

                    wNext += dy;
                    xNext += dx;
                    if (openEnded && IsOutside(wNext, xNext)) return;

                    (wNext, xNext) = WrapCoords(wNext, xNext);
                }
            }
        }

        #endregion

        #region Deposicion
        /*
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
        */
        public virtual void depositGrain(int w, int x, int dx, int dy, float depositeHeight = 1f)
        {
            int wLow, xLow;

            while (FindSlope.Downslope(w, x, dx, dy, out wLow, out xLow) >= slopeThreshold)
            {
                if (openEnded &&
                    ((xLow == mLength && x == 0) || (xLow == 0 && x == mLength) ||
                    (wLow == mWidth && w == 0) || (wLow == 0 && w == mWidth)))
                    break;

                // No continuar si el terreno fijo está más alto
                if (terrainElev[wLow, xLow] >= Elev[wLow, xLow])
                    break;

                w = wLow;
                x = xLow;
            }

            // Evitar depósito si hay terreno fijo encima
            if (terrainElev[w, x] > Elev[w, x] + depositeHeight)
                return;

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
                // Detener sombra si hay obstáculo
                if (terrainElev[w, x] > Elev[w, x])
                    break;

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
        /*
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

                UpdateShadowFromPoint(x, w, dx, dy);
            }

            //shadowCheck(true, dx, dy);
        }
        */
        public virtual void Tick(int grainsPerStep, int dx, int dy, float erosionHeight, float depositeHeight)
        {
            for (int subticks = grainsPerStep; subticks > 0; subticks--)
            {
                int x = rnd.Next(0, Length);
                int w = rnd.Next(0, Width);

                // No erosionar si no hay arena o si hay sombra
                if (Elev[w, x] == 0) { //ue.Debug.Log("Sin elevacion");
                    continue; }
                if (Shadow[w, x] > 0) { //ue.Debug.Log("En sombra");
                    continue;}

                // No erosionar si el terreno fijo bloquea
                if (terrainElev[w, x] >= Elev[w, x]) continue;

                ue.Debug.Log("Erosionando grano" + w + "," + x);
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

                    // Evitar avanzar si el terreno fijo bloquea el paso
                    if (terrainElev[wCurr, xCurr] >= Elev[wCurr, xCurr])
                        break;

                    if (Shadow[wCurr, xCurr] > 0)
                    {
                        ue.Debug.Log("Depositando en" + w + "," + x);
                        depositGrain(wCurr, xCurr, dx, dy, depositeHeight);
                        break;
                    }

                    if (--i <= 0)
                    {
                        bool sandHere = Elev[wCurr, xCurr] > 0;
                        double p = sandHere ? pSand : pNoSand;

                        if (rnd.NextDouble() < p)
                        {
                            ue.Debug.Log("Depositando en" + w + "," + x);
                            depositGrain(wCurr, xCurr, dx, dy, depositeHeight);
                            break;
                        }

                        i = HopLength;
                    }
                }

                UpdateShadowFromPoint(x, w, dx, dy);
            }

            // shadowCheck(true, dx, dy); // Opcional si mantienes sombra dinámica
        }

        #endregion


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
