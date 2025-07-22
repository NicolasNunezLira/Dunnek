using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public partial class DualMesh : MonoBehaviour
{
    #region Handle Input
    public void HandleInput()
    {
        // Enter/Exit Build Mode
        if (Input.GetKeyDown(KeyCode.C) && inMode != PlayingMode.Destroy)
        {
            inMode = (inMode == PlayingMode.Build) ? PlayingMode.Simulation : PlayingMode.Build;
            sandGO.GetComponent<MeshCollider>().sharedMesh = sandGO.GetComponent<MeshFilter>().mesh;
            terrainGO.GetComponent<MeshCollider>().sharedMesh = terrainGO.GetComponent<MeshFilter>().mesh;
        }

        if (Input.GetKeyDown(KeyCode.X) && inMode != PlayingMode.Build)
        {
            inMode = (inMode == PlayingMode.Destroy) ? PlayingMode.Simulation : PlayingMode.Destroy;
            sandGO.GetComponent<MeshCollider>().sharedMesh = sandGO.GetComponent<MeshFilter>().mesh; 
            terrainGO.GetComponent<MeshCollider>().sharedMesh = terrainGO.GetComponent<MeshFilter>().mesh;

            if (inMode == PlayingMode.Simulation)
            {
                builder.RestoreHoverMaterials();
            }
        }
        #endregion
    }
}
