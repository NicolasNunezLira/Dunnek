using UnityEngine;
using Data;
using System.Collections.Generic;
using Unity.Mathematics;

namespace DunefieldModel_DualMesh
{
    public partial class ModelDM
    {
        #region Destroy buried builds

        public void TryToDeleteBuild(int checkX, int checkZ)
        {
            int id = constructionGrid[checkX, checkZ];
            if (!constructions.TryGetValue(id, out ConstructionData currentConstruction))
            {
                Debug.LogWarning($"ID {id} no encontrado en constructions.");
                return;
            }

            (bool isBuried, string toDestroyName, int idToDestroy, List<int2> needActivate) = currentConstruction.IsBuried(sand, constructionGrid);
            if (isBuried) { Debug.Log($"Construcción {toDestroyName} enterrada. No utilizable."); }

            DeleteBuild(idToDestroy);

            foreach (var cell in needActivate)
            {
                ActivateCell(cell.x, cell.y);
            }
        }

        public void DeleteBuild(int id)
        {
            if (!constructions.TryGetValue(id, out ConstructionData data))
            {
                Debug.LogWarning($"ID {id} no encontrado al intentar eliminar construcción.");
                return;
            }

            if (!data.isBuried) return;

            foreach (var cell in data.support)
            {
                constructionGrid[cell.x, cell.y] = 0;
                terrainShadow[cell.x, cell.y] = terrain[cell.x, cell.y];
            }
            foreach (var cell in data.boundarySupport)
            {
                constructionGrid[cell.x, cell.y] = 0;
                terrainShadow[cell.x, cell.y] = terrain[cell.x, cell.y];
            }


            if (data.obj != null)
            {
                UnityEngine.Object.Destroy(data.obj);
            }

            constructions.Remove(id);
        }
        #endregion
    }
}
