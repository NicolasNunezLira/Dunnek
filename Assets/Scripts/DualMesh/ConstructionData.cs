using UnityEngine;
using System.Collections.Generic;
using Unity.Mathematics;

namespace Data
{
    [System.Serializable]
    public class ConstructionData
    {
        #region Atributos
        public Vector3 position;
        public Quaternion rotation;
        public DualMesh.BuildMode type;
        public List<int2> support;
        public List<int2> boundarySupport;
        public float floorHeight;
        public float buildHeight;
        #endregion

        #region Metodos
        public bool IsBuried(float[,] sandElev, float tolerance = 0.05f, float supportThreshold = 0.6f, float boundaryThreshold = 0.3f)
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

            return supportRatio >= supportThreshold && boundaryRatio >= boundaryThreshold;
        }

        /*public void UpdateUsability(float[,] sandElev)
        {
            isUsable = !IsBuried(sandElev);
        }*/

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
