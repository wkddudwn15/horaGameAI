using UnityEngine;

public class TitleMenuController : MonoBehaviour
{
    [SerializeField] private SceneTransitionController sceneTransitionController;
    [SerializeField] private QuitHandler quitHandler;

    public void OnStartClicked()
    {
        if (sceneTransitionController != null)
        {
            sceneTransitionController.LoadGameScene();
        }
    }

    public void OnQuitClicked()
    {
        if (quitHandler != null)
        {
            quitHandler.QuitGame();
        }
    }
}
