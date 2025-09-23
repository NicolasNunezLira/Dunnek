using UnityEngine.EventSystems;
using UnityEngine;
using Utils;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public partial class DualMesh : MonoBehaviour
{
    public void ActionsMode()
    {
        builder.UpdateActionPreviewVisual();
        SetActionType(builder.currentActionMode);

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                currentActionMode = (ActionMode)(((int)currentActionMode + 1) % System.Enum.GetValues(typeof(ActionMode)).Length);
                //currentActionMode = (ActionMode)(((int)currentActionMode - 1 + System.Enum.GetValues(typeof(ActionMode)).Length) % System.Enum.GetValues(typeof(ActionMode)).Length);
            }
            else
            {
                //currentActionMode = (ActionMode)(((int)currentActionMode + 1) % System.Enum.GetValues(typeof(ActionMode)).Length);
                currentActionMode = (ActionMode)(((int)currentActionMode - 1 + System.Enum.GetValues(typeof(ActionMode)).Length) % System.Enum.GetValues(typeof(ActionMode)).Length);
            }

            SetActionType(currentActionMode);
        }

        if (EventSystem.current.IsPointerOverGameObject())
        {
            builder.HideAllPreviews();
            return;
        }

        builder.HandleBuildPreview();

        if (Input.GetMouseButtonDown(0))
        {
            bool constructed = builder.ConfirmAction();

            // Si se ejecutó correctamente la acción, volver a Simulation
            inMode = !constructed ? inMode : PlayingMode.Simulation;

            // Actualizar UI
            UIController uiController = UIController.Instance;


            uiController.UpdateMainButtonVisuals(inMode);

            // Si estamos en acciones, mantener la pestaña abierta
            if (inMode == PlayingMode.Action)
            {
                uiController.ShowCategory("Actions");
                uiController.UpdateActionsButtonVisual(currentActionMode.ToString().ToLower());
            }
        }
    }
}