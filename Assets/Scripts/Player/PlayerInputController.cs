using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputController : MonoBehaviour
{
    [Header("Input Actions")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference lookAction;
    [SerializeField] private InputActionReference interactAction;
    [SerializeField] private InputActionReference sprintAction;
    [SerializeField] private InputActionReference pauseAction;

    public event Action InteractPressed;
    public event Action PausePressed;

    public Vector2 MoveValue => ReadValue(moveAction);
    public Vector2 LookValue => ReadValue(lookAction);
    public bool IsSprinting => sprintAction != null && sprintAction.action != null && sprintAction.action.IsPressed();

    private void OnEnable()
    {
        EnableAction(moveAction);
        EnableAction(lookAction);
        EnableAction(interactAction);
        EnableAction(sprintAction);
        EnableAction(pauseAction);

        if (interactAction != null && interactAction.action != null)
        {
            interactAction.action.performed += OnInteractPerformed;
        }

        if (pauseAction != null && pauseAction.action != null)
        {
            pauseAction.action.performed += OnPausePerformed;
        }
    }

    private void OnDisable()
    {
        if (interactAction != null && interactAction.action != null)
        {
            interactAction.action.performed -= OnInteractPerformed;
        }

        if (pauseAction != null && pauseAction.action != null)
        {
            pauseAction.action.performed -= OnPausePerformed;
        }

        DisableAction(moveAction);
        DisableAction(lookAction);
        DisableAction(interactAction);
        DisableAction(sprintAction);
        DisableAction(pauseAction);
    }

    private static Vector2 ReadValue(InputActionReference actionReference)
    {
        if (actionReference == null || actionReference.action == null)
        {
            return Vector2.zero;
        }

        return actionReference.action.ReadValue<Vector2>();
    }

    private static void EnableAction(InputActionReference actionReference)
    {
        if (actionReference != null && actionReference.action != null)
        {
            actionReference.action.Enable();
        }
    }

    private static void DisableAction(InputActionReference actionReference)
    {
        if (actionReference != null && actionReference.action != null)
        {
            actionReference.action.Disable();
        }
    }

    private void OnInteractPerformed(InputAction.CallbackContext context)
    {
        InteractPressed?.Invoke();
    }

    private void OnPausePerformed(InputAction.CallbackContext context)
    {
        PausePressed?.Invoke();
    }
}
