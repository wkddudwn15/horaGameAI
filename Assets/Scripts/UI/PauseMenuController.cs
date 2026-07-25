using UnityEngine;

public class PauseMenuController : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameStateController gameStateController;
    [SerializeField] private PlayerInputController inputController;
    [SerializeField] private AIChatUIController chatUIController;
    [SerializeField] private TerminalUIController terminalUIController;
    [SerializeField] private SceneTransitionController sceneTransitionController;
    [SerializeField] private QuitHandler quitHandler;

    private void Awake()
    {
        ClosePanelOnly();
    }

    private void OnEnable()
    {
        if (inputController != null)
        {
            inputController.PausePressed += HandlePausePressed;
        }
    }

    private void OnDisable()
    {
        if (inputController != null)
        {
            inputController.PausePressed -= HandlePausePressed;
        }
    }

    public void HandlePausePressed()
    {
        if (gameStateController == null)
        {
            return;
        }

        switch (gameStateController.CurrentState)
        {
            case GameState.Opening:
                return;
            case GameState.Chat:
                if (chatUIController != null)
                {
                    chatUIController.Close();
                }
                return;
            case GameState.Terminal:
                if (terminalUIController != null)
                {
                    terminalUIController.Close();
                }
                return;
            case GameState.Paused:
                ResumeGame();
                return;
            case GameState.Gameplay:
                OpenPause();
                return;
        }
    }

    public void OpenPause()
    {
        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
        }

        if (gameStateController != null)
        {
            gameStateController.SetState(GameState.Paused);
        }
    }

    public void ResumeGame()
    {
        ClosePanelOnly();
        if (gameStateController != null)
        {
            gameStateController.SetState(GameState.Gameplay);
        }
    }

    public void ReturnToTitle()
    {
        if (sceneTransitionController != null)
        {
            sceneTransitionController.LoadTitleScene();
        }
    }

    public void QuitGame()
    {
        if (quitHandler != null)
        {
            quitHandler.QuitGame();
        }
    }

    private void ClosePanelOnly()
    {
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }
    }
}
