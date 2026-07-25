using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionController : MonoBehaviour
{
    [SerializeField] private string titleSceneName = "TitleScene";
    [SerializeField] private string gameSceneName = "GameScene";

    public void LoadTitleScene()
    {
        if (!string.IsNullOrWhiteSpace(titleSceneName))
        {
            SceneManager.LoadScene(titleSceneName);
        }
    }

    public void LoadGameScene()
    {
        if (!string.IsNullOrWhiteSpace(gameSceneName))
        {
            SceneManager.LoadScene(gameSceneName);
        }
    }
}
