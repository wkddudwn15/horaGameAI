using UnityEngine;

public class TerminalUIController : MonoBehaviour
{
    [SerializeField] private GameObject terminalPanel;
    [SerializeField] private GameStateController gameStateController;
    [SerializeField] private AIChatUIController chatUIController;

    private void Awake()
    {
        ClosePanelOnly();
    }

    public void Open()
    {
        if (terminalPanel != null)
        {
            terminalPanel.SetActive(true);
        }

        if (gameStateController != null)
        {
            gameStateController.SetState(GameState.Terminal);
        }
    }

    public void Close()
    {
        ClosePanelOnly();
        if (gameStateController != null)
        {
            gameStateController.SetState(GameState.Gameplay);
        }
    }

    public void ClosePanelOnly()
    {
        if (terminalPanel != null)
        {
            terminalPanel.SetActive(false);
        }
    }

    public void OnStartChatClicked()
    {
        ClosePanelOnly();
        if (chatUIController != null)
        {
            chatUIController.Open();
        }
    }
}
