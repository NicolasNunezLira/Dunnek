using UnityEngine;
using System.Collections.Generic;
using Unity.Mathematics;
using System.Text.RegularExpressions;
using DunefieldModel_DualMesh;

namespace ConstructionSystem
{
    [System.Serializable]
    public class ConstructionInstance
    {
        #region Attributes
        public GameObject Obj { get; private set; }
        public int id => int.Parse(Regex.Match(Obj.name, @"\d+$").Value);
        public ConstructionConfig.ConfigData Config { get; private set; }
        public Vector3 Position { get; private set; }
        public Quaternion Rotation { get; private set; }
        public ConstructionCategory Category { get; private set; }
        public List<int2> Support { get; private set; }
        public List<int2> BoundarySupport { get; private set; }
        public float floorHeight;
        public float buildHeight;
        public float timeBuilt;
        public bool isBuried = false;
        public int? groupId = null;
        #endregion

        #region Constructor
        public ConstructionInstance(
            GameObject obj,
            Vector3 position,
            Quaternion rotation,
            ConstructionConfig.ConfigData config,
            List<int2> support,
            List<int2> boundarySupport,
            float floorHeight,
            float buildHeight
        )
        {
            Obj = obj;
            Position = position;
            Rotation = rotation;
            Config = config;
            Support = support;
            BoundarySupport = boundarySupport;

            this.floorHeight = floorHeight;
            this.buildHeight = buildHeight;

            timeBuilt = TimeManager.Instance.turn;
        }
        #endregion

        #region Is buried and erode
        public (bool, string, int, List<int2>) IsBuried(
            NativeGrid sandElev,
            ConstructionGrid constructionGrid,
            FrameVisualChanges sandChanges,
            float tolerance = 0.05f,
            float supportThreshold = 0.6f,
            float boundaryThreshold = 0.3f
        )
        {

            int buriedSupport = 0;
            foreach (var cell in Support)
            {
                if (sandElev[cell.x, cell.y] > floorHeight + tolerance)
                    buriedSupport++;
            }

            int buriedBoundary = 0;
            foreach (var cell in BoundarySupport)
            {
                if (sandElev[cell.x, cell.y] >= floorHeight + buildHeight - tolerance)
                    buriedBoundary++;
            }

            float supportRatio = (float)buriedSupport / Support.Count;
            float boundaryRatio = (float)buriedBoundary / BoundarySupport.Count;

            isBuried = supportRatio >= supportThreshold && boundaryRatio >= boundaryThreshold;

            string constructionName = Obj.name;

            List<int2> needActivate = new List<int2>();
            if (isBuried)
            {
                needActivate = ErodeBuild(sandElev, constructionGrid, sandChanges);
            }

            var match = Regex.Match(constructionName, @"\d+$");
            if (match.Success)
            {
                int id = int.Parse(match.Value);
                return (isBuried, constructionName, id, needActivate);
            }
            else
            {
                Debug.LogWarning($"No se encontró un número válido al final del nombre '{constructionName}'");
                return (isBuried, constructionName, -1, needActivate); // o lanza excepción personalizada si es crítico
            }

            //return (isBuried, constructionName, int.Parse(Regex.Match(constructionName, @"\d+$").Value), needActivate);
        }
        public List<int2> ErodeBuild(
            NativeGrid sandElev,
            ConstructionGrid constructionGrid,
            FrameVisualChanges changes)
        {
            List<int2> needActivate = new List<int2>();
            foreach (var cell in Support)
            {
                float sandHeight = sandElev[cell.x, cell.y];

                if (sandHeight <= buildHeight + floorHeight)
                {
                    needActivate.Add(cell);
                    sandElev[cell.x, cell.y] = Mathf.Max(buildHeight + floorHeight, sandHeight);
                }

                //constructionGrid[cell.x, cell.y] = 0;
                constructionGrid.TryRemoveConstruction(cell.x, cell.y, id);
                changes.AddChanges(cell.x, cell.y);
            }
            foreach (var cell in BoundarySupport)
            {
                //constructionGrid[cell.x, cell.y] = 0;
                constructionGrid.TryRemoveConstruction(cell.x, cell.y, id);
                needActivate.Add(cell);
            }

            return needActivate;
        }
        #endregion

        #region Pull Down
        public System.Collections.IEnumerator InitPulledDownCoroutine(NativeGrid sandElev, FrameVisualChanges sandChanges, float maxExtraHeight = 0.2f, float cellSize = 1f)
        {
            if (Obj == null) yield break;
            // Activar animación de derrumbe
            var pulled = Obj.transform.Find("default")?.GetComponent<PulledDown>();
            if (pulled != null)
            {
                pulled.activatePulledDown = true;
            }

            // Esperar a que el componente esté listo
            yield return new WaitUntil(() => pulled != null && pulled.IsCollapsing);

            // Calcular centro en coordenadas de grilla
            float cx = Position.x / cellSize;
            float cz = Position.z / cellSize;

            // Unir Support + boundary
            List<int2> allCells = new List<int2>();
            allCells.AddRange(Support);
            allCells.AddRange(BoundarySupport);

            // Calcular distancia máxima desde el centro
            List<(int2 cell, float dist)> distancias = new List<(int2, float)>();
            float maxDist = 0f;

            foreach (var cell in allCells)
            {
                float dx = cell.x - cx;
                float dz = cell.y - cz;
                float dist = Mathf.Sqrt(dx * dx + dz * dz);
                distancias.Add((cell, dist));
                if (dist > maxDist)
                    maxDist = dist;
            }

            // Ordenar de adentro hacia afuera
            distancias.Sort((a, b) => a.dist.CompareTo(b.dist));

            float duration = pulled != null ? pulled.Duration : 2f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float t = pulled != null ? pulled.CollapseProgress : elapsed / duration;
                float scaleY = pulled != null ? pulled.CurrentHeight : Mathf.Lerp(1f, 0f, t);

                // Aumentar arena en base al progreso de colapso
                foreach (var (cell, dist) in distancias)
                {
                    float coneHeight = maxExtraHeight * (1f - dist / maxDist);
                    float altura = floorHeight + buildHeight * (1f - scaleY) + coneHeight * (1f - scaleY);
                    sandElev[cell.x, cell.y] = Mathf.Max(sandElev[cell.x, cell.y], altura);
                    sandChanges.AddChanges(cell.x, cell.y);
                }

                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        public bool NeedPullDown()
        {
            return !isBuried && (Time.time - timeBuilt >= Config.duration);
        }

        public void RestoreTerrain(float[,] terrainElev, float[,] duneTerrain)
        {
            foreach (var cell in Support)
            {
                duneTerrain[cell.x, cell.y] = terrainElev[cell.x, cell.y];
            }
        }

        public void MarkCells(int[,] grid, int id)
        {
            foreach (var cell in Support)
            {
                grid[cell.x, cell.y] = id;
            }
        }
        #endregion

        #region Destroy
        //public abstract void OnDestroy();
        #endregion
    }
}