using UnityEngine;
using ConstructionSystem;
using System.Collections.Generic;
using Unity.Mathematics;
using ResourceSystem;

namespace DunefieldModel_DualMesh
{
    public partial class ModelDM
    {
        #region Destroy buried builds

        public void TryToDeleteBuild(int checkX, int checkZ)
        {
            List<int> ids = constructionGrid[checkX, checkZ];
            foreach (int id in ids)
            {
                if (!constructions.TryGetValue(id, out ConstructionInstance currentConstruction))
                {
                    Debug.LogWarning($"ID {id} no encontrado en constructions.");
                    return;
                }

                (bool isBuried, string toDestroyName, int idToDestroy, List<int2> needActivate) = currentConstruction.IsBuried(sand, constructionGrid, sandChanges);
                if (isBuried) { Debug.Log($"Construcción {toDestroyName} enterrada. No utilizable."); }

                DeleteBuild(idToDestroy);

                foreach (var cell in needActivate)
                {
                    ActivateCell(cell.x, cell.y);
                }
            }
        }

        public void DeleteBuild(int id)
        {
            if (!constructions.TryGetValue(id, out ConstructionInstance data))
            {
                Debug.LogWarning($"ID {id} no encontrado al intentar eliminar construcción.");
                return;
            }

            if (!data.isBuried) return;

            foreach (var cell in data.Support)
            {
                //constructionGrid[cell.x, cell.y] = 0;
                constructionGrid.TryRemoveConstruction(cell.x, cell.y, id);
                terrainShadow[cell.x, cell.y] = terrain[cell.x, cell.y];
            }
            foreach (var cell in data.BoundarySupport)
            {
                //constructionGrid[cell.x, cell.y] = 0;
                constructionGrid.TryRemoveConstruction(cell.x, cell.y, id);
                terrainShadow[cell.x, cell.y] = terrain[cell.x, cell.y];
                //AddChanges(terrainShadowChanges, cell.x, cell.y);
            }

            ResourcesLink link = data.Obj.GetComponent<ResourcesLink>();

            if (link != null && link.IsBonusProvider)
            {
                // Remover todos los bonuses locales que esta construcción proveía
                BonusSystem.BonusManager.RemoveLocalBonusesByProvider(data.Obj);
            }


            ResourceManager.RemoveConsumer(id, recycle: false);


            if (data.Obj != null)
            {
                UnityEngine.Object.Destroy(data.Obj);
            }

            constructions.Remove(id);
        }
        #endregion
    }
}
