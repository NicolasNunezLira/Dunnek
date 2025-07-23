using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public partial class DualMesh : MonoBehaviour
{
    public void DestructionMode()
    {
        builder.DetectConstructionUnderCursor(Color.red);
        if (Input.GetMouseButtonDown(0))
        {
            destructed = builder.DestroyConstruction();
            inMode = !destructed ? inMode : PlayingMode.Simulation;
        }
    }
}
