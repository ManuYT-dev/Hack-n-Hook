using UnityEngine;
using UnityEngine.InputSystem;

public class DEBUG_CONTROLLS : MonoBehaviour
{
    #region Unity Funcs
    private void OnEnable()
    {
        foreach (InputAction action in Controlls.inputActions)
            if (action.actionMap.name != "UI")
                action.started += OnStarted;
    }

    private void OnDisable()
    {
        foreach (InputAction action in Controlls.inputActions)
            action.started -= OnStarted;
    }

    private static void OnStarted(InputAction.CallbackContext ctx)
    {
        Debug.Log($"[CONTROLLS] {ctx.action.actionMap.name}.{ctx.action.name} ({ctx.control?.path})");
    }
    #endregion
}
