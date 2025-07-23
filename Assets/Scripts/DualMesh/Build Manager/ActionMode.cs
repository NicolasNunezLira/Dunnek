using System;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public partial class DualMesh : MonoBehaviour
{
    public void ActionsMode()
    {
        builder.HideAllPreviews();
        
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                currentActionMode = (ActionMode)(((int)currentActionMode - 1 + System.Enum.GetValues(typeof(ActionMode)).Length) % System.Enum.GetValues(typeof(ActionMode)).Length);
            }
            else
            {
                currentActionMode = (ActionMode)(((int)currentActionMode + 1) % System.Enum.GetValues(typeof(ActionMode)).Length);
            }

            builder.currentActionMode = currentActionMode;
            builder.UpdateActionPreviewVisual();
        }

        builder.HandleBuildPreview();

        if (Input.GetMouseButtonDown(0))
        {
            constructed = builder.ConfirmAction();
            inMode = !constructed ? inMode : PlayingMode.Simulation;        
        }
    }
}