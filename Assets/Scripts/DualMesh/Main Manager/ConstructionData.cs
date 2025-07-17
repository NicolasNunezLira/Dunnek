using UnityEngine;
using System.Collections.Generic;
using Unity.Mathematics;
using System.Text.RegularExpressions;
using System;
using DunefieldModel_DualMesh;
using Unity.Collections;
using DunefieldModel_DualMeshJobs;

namespace Data
{
    [System.Serializable]
    public class ConstructionData
    {
        #region Atributos
        public GameObject obj;
        public Vector3 position;
        public Quaternion rotation;
        public DualMesh.BuildMode type;
        public List<int2> support;
        public List<int2> boundarySupport;
        public float floorHeight;
        public float buildHeight;
        public float duration;
        public float timeBuilt;

        public bool isBuried = false;
        #endregion

        #region Metodos
        public (bool, string, int, List<int2>) IsBuried(
            NativeGrid sandElev, int[,] constructionGrid, FrameVisualChanges sandChanges,
            float tolerance = 0.05f, float supportThreshold = 0.6f, float boundaryThreshold = 0.3f)
        {
            int buriedSupport = 0;
            foreach (var cell in support)
            {
                if (sandElev[cell.x, cell.y] > floorHeight + tolerance)
                    buriedSupport++;
            }

            int buriedBoundary = 0;
            foreach (var cell in boundarySupport)
            {
                if (sandElev[cell.x, cell.y] >= floorHeight + buildHeight - tolerance)
                    buriedBoundary++;
            }

            float supportRatio = (float)buriedSupport / support.Count;
            float boundaryRatio = (float)buriedBoundary / boundarySupport.Count;

            isBuried = supportRatio >= supportThreshold && boundaryRatio >= boundaryThreshold;

            string constructionName = obj.name;

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

        public List<int2> ErodeBuild(NativeGrid sandElev, int[,] constructionGrid, FrameVisualChanges changes)
        {
            List<int2> needActivate = new List<int2>();
            foreach (var cell in support)
            {
                float sandHeight = sandElev[cell.x, cell.y];

                if (sandHeight <= buildHeight + floorHeight)
                {
                    needActivate.Add(cell);
                    sandElev[cell.x, cell.y] = Math.Max(buildHeight + floorHeight, sandHeight);
                }

                constructionGrid[cell.x, cell.y] = 0;
                changes.AddChanges(cell.x, cell.y);
            }
            foreach (var cell in boundarySupport)
            {
                constructionGrid[cell.x, cell.y] = 0;
                needActivate.Add(cell);
            }            

            return needActivate;
        }

        public System.Collections.IEnumerator InitPulledDownCoroutine(NativeGrid sandElev, FrameVisualChanges sandChanges, float maxExtraHeight = 0.2f, float cellSize = 1f)
        {
            if (obj == null) yield break;
            // Activar animación de derrumbe
            var pulled = obj.transform.Find("default")?.GetComponent<PulledDown>();
            if (pulled != null)
            {
                pulled.activatePulledDown = true;
            }

            // Esperar a que el componente esté listo
            yield return new WaitUntil(() => pulled != null && pulled.IsCollapsing);

            // Calcular centro en coordenadas de grilla
            float cx = position.x / cellSize;
            float cz = position.z / cellSize;

            // Unir support + boundary
            List<int2> allCells = new List<int2>();
            allCells.AddRange(support);
            allCells.AddRange(boundarySupport);

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
            return !isBuried && (Time.time - timeBuilt >= duration);
        }

        public void RestoreTerrain(float[,] terrainElev, float[,] duneTerrain)
        {
            foreach (var cell in support)
            {
                duneTerrain[cell.x, cell.y] = terrainElev[cell.x, cell.y];
            }
        }

        public void MarkCells(int[,] grid, int id)
        {
            foreach (var cell in support)
            {
                grid[cell.x, cell.y] = id;
            }
        }

        #endregion
    }
    

}
