using System.Collections;
using Data;
using UnityEngine;
using CameraManager;
using Unity.Mathematics;
using System.Linq;
using DunefieldModel_DualMesh;

public partial class DualMesh
{
    public void CheckForPullDowns()
    {
        if (!isHandlingPullDown) StartCoroutine(HandlePullDownsSequentially());
    }

    private IEnumerator HandlePullDownsSequentially()
    {
        isHandlingPullDown = true;
        foreach (var build in constructions.ToList()) // copia para evitar modificación durante iteración
        {
            int id = build.Key;
            ConstructionData construction = build.Value;

            if (construction.NeedPullDown())
            {
                isPaused = true;
                CameraController.Instance.isControllable = false;

                yield return StartCoroutine(FocusAndCollapse(id, construction));

                CameraController.Instance.isControllable = true;
                isPaused = false;
            }
        }
        isHandlingPullDown = false;
    }

    private IEnumerator FocusAndCollapse(int id, ConstructionData construction)
    {
        yield return StartCoroutine(CameraController.Instance.MoveCameraTo(construction.obj.transform.position));

        yield return new WaitForSeconds(0.5f);

        yield return construction.InitPulledDownCoroutine(duneModel.sand);

        yield return new WaitForSeconds(0.5f);

        bool isDestroyed = DestroyBuildForID(id, out string name);
        if (isDestroyed) Debug.Log($"{name} derrumbado.");
  
    }

    public bool DestroyBuildForID(int id, out string name)
    {
        if (!constructions.ContainsKey(id)) { name = null; return false; }
        else{ name = constructions[id].obj.name; }
        ConstructionData data = constructions[id];

        // Liberar celdas ocupadas
        foreach (int2 coord in data.support)
        {
            int cx = coord.x;
            int cz = coord.y;

            if (cx < 0 || cz < 0 || cx >= constructionGrid.GetLength(0) || cz >= constructionGrid.GetLength(1)) continue;

            constructionGrid[cx, cz] = 0;
            //duneModel.constructionGrid[cx, cz] = 0;

            /*
            if (duneModel.sandElev[cx, cz] >= data.buildHeight)
            {
                duneModel.sandElev[cx, cz] -= data.buildHeight;
            }
            */

            duneModel.terrainShadow[cx, cz] = terrainShadow[cx, cz]; // restaura altura original
            duneModel.ActivateCell(cx, cz);
            duneModel.UpdateShadow(cx, cz, duneModel.dx, duneModel.dz);
        }
        foreach (int2 coord in data.boundarySupport)
        {
            int cx = coord.x;
            int cz = coord.y;

            if (cx < 0 || cz < 0 || cx >= constructionGrid.GetLength(0) || cz >= constructionGrid.GetLength(1)) continue;

            constructionGrid[cx, cz] = 0;
            //duneModel.constructionGrid[cx, cz] = 0;

            duneModel.terrainShadow[cx, cz] = terrainShadow[cx, cz]; // restaura altura original
            duneModel.ActivateCell(cx, cz);
            duneModel.UpdateShadow(cx, cz, duneModel.dx, duneModel.dz);
        }
        
        data.obj.SetActive(false);
        GameObject.Destroy(data.obj, 1f);
        constructions.Remove(id);
        return true;
    }
}
