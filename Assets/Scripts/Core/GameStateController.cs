using System;
using UnityEngine;

public class GameStateController : MonoBehaviour
{
    [SerializeField] private FirstPersonController firstPersonController;
    [SerializeField] private PlayerInteraction playerInteraction;
    [SerializeField] private GameState initialState = GameState.Opening;

    public event Action<GameState> StateChanged;

    public GameState CurrentState { get; private set; }
    public bool IsGameplay => CurrentState == GameState.Gameplay;

    private void Awake()
    {
        SetState(initialState);
    }

    public void SetState(GameState newState)
    {
        if (CurrentState == newState)
        {
            ApplyState(newState);
            return;
        }

        CurrentState = newState;
        ApplyState(newState);
        StateChanged?.Invoke(newState);
    }

    private void ApplyState(GameState state)
    {
        bool gameplay = state == GameState.Gameplay;
        bool lockedCursor = state == GameState.Opening || gameplay;

        if (firstPersonController != null)
        {
            firstPersonController.SetMovementEnabled(gameplay);
            firstPersonController.SetLookEnabled(gameplay);
        }

        if (playerInteraction != null)
        {
            playerInteraction.SetInteractionEnabled(gameplay);
        }

        Cursor.visible = !lockedCursor;
        Cursor.lockState = lockedCursor ? CursorLockMode.Locked : CursorLockMode.None;
    }
}
