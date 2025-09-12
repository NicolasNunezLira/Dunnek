using UnityEngine;
using Building;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public partial class DualMesh : MonoBehaviour
{
    public void RecycleMode()
    {
        builder.DetectConstructionUnderCursor(Color.red);
        if (Input.GetMouseButtonDown(0))
        {
            destructed = builder.DestroyConstruction();
            inMode = !destructed ? inMode : PlayingMode.Simulation;
            SetMode(inMode);
        }
    }
}
