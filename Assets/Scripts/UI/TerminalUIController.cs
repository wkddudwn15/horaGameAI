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
            RestorePanelChildren();
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

    private void RestorePanelChildren()
    {
        if (terminalPanel == null)
        {
            return;
        }

        for (int i = 0; i < terminalPanel.transform.childCount; i++)
        {
            terminalPanel.transform.GetChild(i).gameObject.SetActive(true);
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
