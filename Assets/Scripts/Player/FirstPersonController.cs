using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [SerializeField] private PlayerInputController inputController;
    [SerializeField] private Transform cameraRoot;
    [SerializeField] private float walkSpeed = 2.2f;
    [SerializeField] private float sprintSpeed = 3.6f;
    [SerializeField] private float lookSensitivity = 0.08f;
    [SerializeField] private float minPitch = -75f;
    [SerializeField] private float maxPitch = 75f;
    [SerializeField] private float gravity = -18f;

    private CharacterController characterController;
    private float pitch;
    private float verticalVelocity;
    private bool movementEnabled;
    private bool lookEnabled;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        if (cameraRoot == null && Camera.main != null)
        {
            cameraRoot = Camera.main.transform;
        }
    }

    private void Update()
    {
        UpdateLook();
        UpdateMovement();
    }

    public void SetMovementEnabled(bool enabled)
    {
        movementEnabled = enabled;
        if (!enabled)
        {
            verticalVelocity = 0f;
        }
    }

    public void SetLookEnabled(bool enabled)
    {
        lookEnabled = enabled;
    }

    private void UpdateLook()
    {
        if (!lookEnabled || inputController == null || cameraRoot == null)
        {
            return;
        }

        Vector2 look = inputController.LookValue * lookSensitivity;
        transform.Rotate(Vector3.up * look.x);

        pitch = Mathf.Clamp(pitch - look.y, minPitch, maxPitch);
        cameraRoot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void UpdateMovement()
    {
        if (characterController == null || !movementEnabled || inputController == null)
        {
            return;
        }

        Vector2 moveInput = inputController.MoveValue;
        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        move = Vector3.ClampMagnitude(move, 1f);

        float speed = inputController.IsSprinting ? sprintSpeed : walkSpeed;

        if (characterController.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -1f;
        }

        verticalVelocity += gravity * Time.deltaTime;
        Vector3 velocity = move * speed + Vector3.up * verticalVelocity;
        characterController.Move(velocity * Time.deltaTime);
    }
}
