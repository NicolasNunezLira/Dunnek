using UnityEngine;
using System;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public partial class DualMesh : MonoBehaviour
{
    public void SimulationMode()
    {
        builder.HideAllPreviews();
        if (windDirection.x != 0 || windDirection.y != 0)
        {
            duneModel.Tick(grainsPerStep, (int)windDirection.x, (int)windDirection.y, heightVariation, heightVariation);
        }

        for (int i = 0; i < 100; i++)
        {
            grainsForAvalanche = duneModel.RunAvalancheBurst(Math.Max(maxCellsPerFrame, grainsForAvalanche));
        }

    }
}