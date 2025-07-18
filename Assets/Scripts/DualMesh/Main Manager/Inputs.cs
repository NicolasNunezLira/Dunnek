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
            // Update the meshcolliders
            sandGO.GetComponent<MeshCollider>().sharedMesh = sandGO.GetComponent<MeshFilter>().mesh; // demasiado caro para realizarlo todos los frames
            terrainGO.GetComponent<MeshCollider>().sharedMesh = terrainGO.GetComponent<MeshFilter>().mesh; // demasiado caro para realizarlo todos los frames
        }

        if (Input.GetKeyDown(KeyCode.X) && inMode != PlayingMode.Build)
        {
            inMode = (inMode == PlayingMode.Destroy) ? PlayingMode.Simulation : PlayingMode.Destroy;
            // Update the meshcolliders
            sandGO.GetComponent<MeshCollider>().sharedMesh = sandGO.GetComponent<MeshFilter>().mesh; // demasiado caro para realizarlo todos los frames
            terrainGO.GetComponent<MeshCollider>().sharedMesh = terrainGO.GetComponent<MeshFilter>().mesh; // demasiado caro para realizarlo todos los frames

            if (inMode == PlayingMode.Simulation)
            {
                builder.RestoreHoverMaterials();
            }
        }
        #endregion
    }
}
