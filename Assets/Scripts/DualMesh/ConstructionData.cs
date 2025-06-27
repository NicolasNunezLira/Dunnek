using UnityEngine;
using System.Collections.Generic;
using Unity.Mathematics;
using System.Text.RegularExpressions;
using System;

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

        public bool isBuried = false;
        #endregion

        #region Metodos
        public (bool, string, int, List<int2>) IsBuried(
            float[,] sandElev, float[,] terrainElev, float[,] realTerrain, int[,] constructionGrid,
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

            isBuried = (supportRatio >= supportThreshold && boundaryRatio >= boundaryThreshold)
                    || (supportRatio >= 3 / 2 * supportThreshold && boundaryRatio >= 2 / 3 * boundaryThreshold)
                    || (supportRatio >= 1 / 2 * supportThreshold && boundaryRatio >= 2 * boundaryThreshold);

            string constructionName = obj.name;

            List<int2> needActivate = new List<int2>();
            if (isBuried)
            {
                needActivate = ErodeBuild(sandElev, terrainElev, realTerrain, constructionGrid);
            }

            return (isBuried, constructionName, int.Parse(Regex.Match(constructionName, @"\d+$").Value), needActivate);
        }

        public List<int2> ErodeBuild(float[,] sandElev, float[,] terrainElev, float[,] realTerrain, int[,] constructionGrid)
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

                terrainElev[cell.x, cell.y] = realTerrain[cell.x, cell.y];             
                constructionGrid[cell.x, cell.y] = 0;
            }
            foreach (var cell in boundarySupport)
            {
                constructionGrid[cell.x, cell.y] = 0;
                needActivate.Add(cell);
            }

            return needActivate;
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
