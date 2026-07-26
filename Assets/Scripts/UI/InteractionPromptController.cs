using TMPro;
using UnityEngine;

public class InteractionPromptController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI promptText;

    private void Awake()
    {
        Hide();
    }

    public void Show(string message)
    {
        if (promptText == null)
        {
            return;
        }

        promptText.text = string.IsNullOrWhiteSpace(message) ? "Press E to use" : message;
        promptText.gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (promptText != null)
        {
            promptText.gameObject.SetActive(false);
        }
    }
}
