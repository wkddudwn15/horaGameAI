using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class OpeningSequenceController : MonoBehaviour
{
    [Header("State")]
    [SerializeField] private GameStateController gameStateController;

    [Header("Camera")]
    [SerializeField] private Transform playerCamera;
    [SerializeField] private Transform lyingCameraPose;
    [SerializeField] private Transform standingCameraPose;
    [SerializeField] private float sitUpDuration = 3f;

    [Header("Fade")]
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeInDuration = 3f;

    [Header("Depth of Field")]
    [SerializeField] private Volume postProcessVolume;
    [SerializeField] private float startFocusDistance = 0.1f;
    [SerializeField] private float endFocusDistance = 8f;

    [Header("Audio")]
    [SerializeField] private AudioSource medicalBeepAudioSource;

    [Header("Subtitle")]
    [SerializeField] private SubtitleController subtitleController;
    [SerializeField] private float subtitleInterval = 0.5f;

    [Header("Terminal")]
    [SerializeField] private TerminalInteractable terminal;

    private bool hasPlayed;
    private DepthOfField depthOfField;

    private readonly string[] openingLines =
    {
        "おはようございます。",
        "体調はいかがですか。",
        "現在、生活支援AIが起動しました。"
    };

    private void Start()
    {
        if (!hasPlayed)
        {
            StartCoroutine(PlayOpening());
        }
    }

    private IEnumerator PlayOpening()
    {
        hasPlayed = true;

        if (gameStateController != null)
        {
            gameStateController.SetState(GameState.Opening);
        }

        SetupDepthOfField();
        SetCameraPose(lyingCameraPose);
        SetFadeAlpha(1f);

        if (medicalBeepAudioSource != null && medicalBeepAudioSource.clip != null)
        {
            medicalBeepAudioSource.Play();
        }

        yield return FadeIn();
        yield return ReleaseBlur();
        yield return MoveCameraToStandingPose();

        if (subtitleController != null)
        {
            yield return subtitleController.PlaySubtitleSequence(openingLines, subtitleInterval);
        }

        if (terminal != null)
        {
            terminal.TurnOnScreen();
        }

        if (gameStateController != null)
        {
            gameStateController.SetState(GameState.Gameplay);
        }
    }

    private void SetupDepthOfField()
    {
        depthOfField = null;
        if (postProcessVolume != null && postProcessVolume.profile != null)
        {
            postProcessVolume.profile.TryGet(out depthOfField);
        }

        if (depthOfField != null)
        {
            depthOfField.active = true;
            depthOfField.mode.Override(DepthOfFieldMode.Gaussian);
            depthOfField.gaussianStart.Override(startFocusDistance);
            depthOfField.gaussianEnd.Override(startFocusDistance + 0.1f);
            depthOfField.highQualitySampling.Override(true);
        }
    }

    private IEnumerator FadeIn()
    {
        if (fadeImage == null)
        {
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            SetFadeAlpha(Mathf.Lerp(1f, 0f, elapsed / fadeInDuration));
            yield return null;
        }

        SetFadeAlpha(0f);
    }

    private IEnumerator ReleaseBlur()
    {
        if (depthOfField == null)
        {
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < sitUpDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / sitUpDuration;
            float focus = Mathf.Lerp(startFocusDistance, endFocusDistance, t);
            depthOfField.gaussianStart.Override(focus);
            depthOfField.gaussianEnd.Override(focus + 3f);
            yield return null;
        }
    }

    private IEnumerator MoveCameraToStandingPose()
    {
        if (playerCamera == null || lyingCameraPose == null || standingCameraPose == null)
        {
            yield break;
        }

        Vector3 startPosition = lyingCameraPose.position;
        Quaternion startRotation = lyingCameraPose.rotation;
        Vector3 endPosition = standingCameraPose.position;
        Quaternion endRotation = standingCameraPose.rotation;

        float elapsed = 0f;
        while (elapsed < sitUpDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / sitUpDuration);
            playerCamera.SetPositionAndRotation(
                Vector3.Lerp(startPosition, endPosition, t),
                Quaternion.Slerp(startRotation, endRotation, t));
            yield return null;
        }

        playerCamera.SetPositionAndRotation(endPosition, endRotation);
    }

    private void SetCameraPose(Transform pose)
    {
        if (playerCamera != null && pose != null)
        {
            playerCamera.SetPositionAndRotation(pose.position, pose.rotation);
        }
    }

    private void SetFadeAlpha(float alpha)
    {
        if (fadeImage == null)
        {
            return;
        }

        Color color = fadeImage.color;
        color.a = alpha;
        fadeImage.color = color;
    }
}
