using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private PlayerInputController inputController;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private InteractionPromptController promptController;
    [SerializeField] private float interactDistance = 2.5f;
    [SerializeField] private LayerMask interactableLayers = ~0;

    private IInteractable currentTarget;
    private bool interactionEnabled;

    private void Awake()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
    }

    private void OnEnable()
    {
        if (inputController != null)
        {
            inputController.InteractPressed += TryInteract;
        }
    }

    private void OnDisable()
    {
        if (inputController != null)
        {
            inputController.InteractPressed -= TryInteract;
        }
    }

    private void Update()
    {
        UpdateTarget();
    }

    public void SetInteractionEnabled(bool enabled)
    {
        interactionEnabled = enabled;
        if (!enabled)
        {
            currentTarget = null;
            if (promptController != null)
            {
                promptController.Hide();
            }
        }
    }

    private void UpdateTarget()
    {
        if (!interactionEnabled || playerCamera == null)
        {
            return;
        }

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactableLayers, QueryTriggerInteraction.Ignore))
        {
            currentTarget = hit.collider.GetComponentInParent<IInteractable>();
            if (currentTarget != null)
            {
                if (promptController != null)
                {
                    promptController.Show(currentTarget.GetInteractionText());
                }
                return;
            }
        }

        currentTarget = null;
        if (promptController != null)
        {
            promptController.Hide();
        }
    }

    private void TryInteract()
    {
        if (interactionEnabled && currentTarget != null)
        {
            currentTarget.Interact();
        }
    }
}
