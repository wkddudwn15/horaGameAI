using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    private enum GameState
    {
        Intro,
        Before,
        Chasing,
        Escape,
        CaughtCutscene,
        Clear,
        GameOver
    }

    [Header("Scene References")]
    [SerializeField] private CharacterController playerController;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Light flashlight;
    [SerializeField] private GameObject enemy;
    [SerializeField] private GameObject sealedTorii;
    [SerializeField] private GameObject bell;
    [SerializeField] private List<GameObject> ofudaItems = new List<GameObject>();

    [Header("UI")]
    [SerializeField] private GameObject startPanel;
    [SerializeField] private GameObject endingPanel;
    [SerializeField] private Text objectiveText;
    [SerializeField] private Text ofudaText;
    [SerializeField] private Text staminaText;
    [SerializeField] private Text blindText;
    [SerializeField] private Text promptText;
    [SerializeField] private Text endingTitleText;
    [SerializeField] private Text endingBodyText;

    [Header("Player")]
    [SerializeField] private float walkSpeed = 3.2f;
    [SerializeField] private float runSpeed = 5.2f;
    [SerializeField] private float mouseSensitivity = 2.1f;
    [SerializeField] private float gravity = -18f;

    [Header("Interaction")]
    [SerializeField] private float bellRange = 2.3f;
    [SerializeField] private float ofudaRange = 2.0f;
    [SerializeField] private float exitRange = 1.25f;

    [Header("Enemy")]
    [SerializeField] private float catchDistance = 0.68f;
    [SerializeField] private float chaseStartDelay = 2.2f;
    [SerializeField] private float blindDuration = 3.2f;

    private readonly Vector3 startPosition = new Vector3(0f, 1.7f, 18f);
    private readonly Vector3 enemyHiddenPosition = new Vector3(0f, 0f, -15f);
    private readonly Vector3 enemySpawnPosition = new Vector3(0f, 0f, -10.95f);
    private readonly Vector3 exitPosition = new Vector3(0f, 1.7f, 18f);

    private GameState state = GameState.Intro;
    private float yaw;
    private float pitch;
    private float verticalVelocity;
    private float stamina = 1f;
    private float chaseDelay;
    private float blindTimer;
    private int blindUses = 3;
    private int ofudaCount;
    private float caughtTimer;
    private bool flashlightOn = true;

    private void Start()
    {
        ResetRuntime(false);
        SetCursor(false);
        if (startPanel != null) startPanel.SetActive(true);
        if (endingPanel != null) endingPanel.SetActive(false);
    }

    private void Update()
    {
        if (state == GameState.Intro || state == GameState.Clear || state == GameState.GameOver)
        {
            return;
        }

        if (state == GameState.CaughtCutscene)
        {
            UpdateCaughtCutscene();
            return;
        }

        UpdateLook();
        UpdatePlayerMovement();
        UpdateInteraction();
        UpdateEnemy();
        UpdateHotkeys();
        UpdateHud();
    }

    public void StartGame()
    {
        state = GameState.Before;
        if (startPanel != null) startPanel.SetActive(false);
        SetCursor(true);
        SetObjective("拝殿の鈴を鳴らせ");
    }

    public void Retry()
    {
        ResetRuntime(true);
        SetCursor(true);
    }

    private void ResetRuntime(bool startImmediately)
    {
        state = startImmediately ? GameState.Before : GameState.Intro;
        yaw = 0f;
        pitch = 0f;
        verticalVelocity = 0f;
        stamina = 1f;
        chaseDelay = 0f;
        blindTimer = 0f;
        blindUses = 3;
        ofudaCount = 0;
        caughtTimer = 0f;
        flashlightOn = true;

        if (playerController != null)
        {
            playerController.enabled = false;
            playerController.transform.position = startPosition;
            playerController.transform.rotation = Quaternion.identity;
            playerController.enabled = true;
        }

        if (playerCamera != null)
        {
            playerCamera.transform.localRotation = Quaternion.identity;
        }

        if (flashlight != null)
        {
            flashlight.enabled = true;
        }

        if (enemy != null)
        {
            enemy.SetActive(false);
            enemy.transform.position = enemyHiddenPosition;
            enemy.transform.localScale = Vector3.one;
        }

        if (sealedTorii != null) sealedTorii.SetActive(false);

        foreach (GameObject item in ofudaItems)
        {
            if (item != null) item.SetActive(true);
        }

        if (endingPanel != null) endingPanel.SetActive(false);
        SetObjective("拝殿の鈴を鳴らせ");
        UpdateHud();
    }

    private void UpdateLook()
    {
        yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
        pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, -77f, 72f);

        if (playerController != null)
        {
            playerController.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        }

        if (playerCamera != null)
        {
            playerCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }
    }

    private void UpdatePlayerMovement()
    {
        if (playerController == null) return;

        Vector3 input = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
        input = Vector3.ClampMagnitude(input, 1f);

        bool running = Input.GetKey(KeyCode.LeftShift) && stamina > 0.05f && input.sqrMagnitude > 0.01f;
        float speed = running ? runSpeed : walkSpeed;

        if (running)
        {
            stamina = Mathf.Max(0f, stamina - Time.deltaTime * 0.24f);
        }
        else
        {
            stamina = Mathf.Min(1f, stamina + Time.deltaTime * 0.17f);
        }

        Vector3 movement = playerController.transform.TransformDirection(input) * speed;

        if (playerController.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -1f;
        }

        verticalVelocity += gravity * Time.deltaTime;
        movement.y = verticalVelocity;
        playerController.Move(movement * Time.deltaTime);
    }

    private void UpdateInteraction()
    {
        if (promptText != null) promptText.text = "";

        if (bell != null && state == GameState.Before)
        {
            float bellDistance = Vector3.Distance(PlayerPosition(), bell.transform.position);
            if (bellDistance <= bellRange)
            {
                if (promptText != null) promptText.text = "E 鈴を鳴らす";
                if (Input.GetKeyDown(KeyCode.E)) RingBell();
                return;
            }
        }

        if (state == GameState.Chasing || state == GameState.Escape)
        {
            for (int i = 0; i < ofudaItems.Count; i++)
            {
                GameObject item = ofudaItems[i];
                if (item == null || !item.activeSelf) continue;

                float distance = Vector3.Distance(PlayerPosition(), item.transform.position);
                if (distance <= ofudaRange)
                {
                    if (promptText != null) promptText.text = "E 札を取る";
                    if (Input.GetKeyDown(KeyCode.E)) CollectOfuda(item);
                    return;
                }
            }
        }

        if (state == GameState.Escape && Vector3.Distance(PlayerPosition(), exitPosition) <= exitRange)
        {
            EndGame(true);
        }
    }

    private void UpdateEnemy()
    {
        if (enemy == null || !(state == GameState.Chasing || state == GameState.Escape)) return;

        blindTimer = Mathf.Max(0f, blindTimer - Time.deltaTime);

        if (chaseDelay > 0f)
        {
            chaseDelay = Mathf.Max(0f, chaseDelay - Time.deltaTime);
            float progress = state == GameState.Escape ? 1f : 1f - chaseDelay / chaseStartDelay;
            enemy.transform.localScale = Vector3.one * Mathf.Max(0.18f, progress);
            enemy.transform.Rotate(0f, Time.deltaTime * 140f, 0f);

            if (chaseDelay <= 0f && state == GameState.Chasing)
            {
                enemy.transform.localScale = Vector3.one;
                SetObjective("札を3枚集めて鳥居へ戻れ");
            }
            return;
        }

        Vector3 enemyPosition = enemy.transform.position;
        Vector3 target = PlayerPosition();
        target.y = enemyPosition.y;
        Vector3 delta = target - enemyPosition;
        float distance = delta.magnitude;

        if (blindTimer > 0f)
        {
            enemy.transform.Rotate(0f, Time.deltaTime * 430f, 0f);
            return;
        }

        if (distance > 0.1f)
        {
            float speed = state == GameState.Escape ? 3.95f : 1.55f + ofudaCount * 0.34f;
            enemy.transform.position += delta.normalized * speed * Time.deltaTime;
        }

        Vector3 lookAt = PlayerPosition();
        lookAt.y = enemy.transform.position.y;
        enemy.transform.LookAt(lookAt);

        if (distance < catchDistance)
        {
            StartCaughtCutscene();
        }
    }

    private void UpdateCaughtCutscene()
    {
        caughtTimer += Time.deltaTime;
        if (enemy != null && playerCamera != null)
        {
            float progress = Mathf.Clamp01(caughtTimer / 2.1f);
            Vector3 forward = playerCamera.transform.forward;
            enemy.SetActive(true);
            enemy.transform.localScale = Vector3.one * Mathf.Lerp(1.05f, 2.35f, progress);
            enemy.transform.position = playerCamera.transform.position + forward * Mathf.Lerp(2.4f, 0.28f, progress);
            enemy.transform.LookAt(playerCamera.transform.position);
        }

        if (caughtTimer > 2.1f)
        {
            EndGame(false);
        }
    }

    private void UpdateHotkeys()
    {
        if (Input.GetKeyDown(KeyCode.F) || Input.GetMouseButtonDown(0))
        {
            ToggleFlashlight();
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            UseBlind();
        }
    }

    private void RingBell()
    {
        if (state != GameState.Before) return;

        state = GameState.Chasing;
        SetObjective("狐が現れた。離れろ");
        if (sealedTorii != null) sealedTorii.SetActive(true);
        if (enemy != null)
        {
            enemy.SetActive(true);
            enemy.transform.position = enemySpawnPosition;
            enemy.transform.localScale = Vector3.one * 0.18f;
        }
        chaseDelay = chaseStartDelay;
    }

    private void CollectOfuda(GameObject item)
    {
        item.SetActive(false);
        ofudaCount++;

        if (ofudaCount >= 3)
        {
            state = GameState.Escape;
            SetObjective("鳥居が戻った。走れ");
            if (sealedTorii != null) sealedTorii.SetActive(false);
            chaseDelay = 0.45f;
        }
    }

    private void UseBlind()
    {
        if (!(state == GameState.Chasing || state == GameState.Escape)) return;
        if (chaseDelay > 0f || blindTimer > 0f || blindUses <= 0) return;

        blindUses--;
        blindTimer = blindDuration;
        SetObjective(blindUses > 0 ? "狐が怯んだ。今のうちに離れろ" : "狐が怯んだ。これが最後だ");
    }

    private void ToggleFlashlight()
    {
        flashlightOn = !flashlightOn;
        if (flashlight != null)
        {
            flashlight.enabled = flashlightOn;
        }
    }

    private void StartCaughtCutscene()
    {
        state = GameState.CaughtCutscene;
        caughtTimer = 0f;
        SetObjective("逃げられない");
        SetCursor(false);
    }

    private void EndGame(bool win)
    {
        state = win ? GameState.Clear : GameState.GameOver;
        SetCursor(false);

        if (endingPanel != null) endingPanel.SetActive(true);
        if (endingTitleText != null) endingTitleText.text = win ? "脱出" : "捕まった";
        if (endingBodyText != null)
        {
            endingBodyText.text = win
                ? "鳥居を抜けた瞬間、背後の鈴だけが鳴り続けていた。"
                : "最後に見えたのは、赤い目と閉じていく牙だった。";
        }
    }

    private void UpdateHud()
    {
        if (ofudaText != null) ofudaText.text = $"札 {ofudaCount}/3";
        if (staminaText != null) staminaText.text = $"スタミナ {Mathf.RoundToInt(stamina * 100f)}%";
        if (blindText != null) blindText.text = $"目くらまし {blindUses}/3 Q";
    }

    private void SetObjective(string message)
    {
        if (objectiveText != null) objectiveText.text = message;
    }

    private Vector3 PlayerPosition()
    {
        return playerController != null ? playerController.transform.position : Vector3.zero;
    }

    private static void SetCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
