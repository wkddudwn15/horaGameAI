using UnityEngine;

public class TerminalInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private string interactionText = "E：調べる";
    [SerializeField] private TerminalUIController terminalUIController;
    [SerializeField] private Renderer screenRenderer;
    [SerializeField] private GameObject worldSpaceCanvas;
    [SerializeField] private Color screenEmissionColor = new Color(0.2f, 0.9f, 1f);
    [SerializeField] private float emissionIntensity = 1.8f;

    private Material screenMaterialInstance;

    private void Awake()
    {
        if (worldSpaceCanvas != null)
        {
            worldSpaceCanvas.SetActive(false);
        }
    }

    public void Interact()
    {
        if (terminalUIController != null)
        {
            terminalUIController.Open();
        }
    }

    public string GetInteractionText()
    {
        return interactionText;
    }

    public void TurnOnScreen()
    {
        if (worldSpaceCanvas != null)
        {
            worldSpaceCanvas.SetActive(true);
        }

        if (screenRenderer == null)
        {
            return;
        }

        if (screenMaterialInstance == null)
        {
            screenMaterialInstance = screenRenderer.material;
        }

        Color emission = screenEmissionColor * emissionIntensity;
        screenMaterialInstance.EnableKeyword("_EMISSION");
        screenMaterialInstance.SetColor("_EmissionColor", emission);
    }
}
