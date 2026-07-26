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

    private Coroutine openingRoutine;

    private void Start()
    {
        if (!hasPlayed)
        {
            openingRoutine = StartCoroutine(PlayOpening());
        }
    }

    private void OnDisable()
    {
        if (openingRoutine != null)
        {
            StopCoroutine(openingRoutine);
            openingRoutine = null;
        }

        depthOfField = null;
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
        if (TryGetDepthOfField(out DepthOfField currentDepthOfField))
        {
            depthOfField = currentDepthOfField;
            ApplyDepthOfFieldSettings(depthOfField, startFocusDistance, startFocusDistance + 0.1f);
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
        if (!TryGetDepthOfField(out depthOfField))
        {
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < sitUpDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / sitUpDuration;
            float focus = Mathf.Lerp(startFocusDistance, endFocusDistance, t);
            if (!TryGetDepthOfField(out depthOfField))
            {
                yield break;
            }

            ApplyDepthOfFieldSettings(depthOfField, focus, focus + 3f);
            yield return null;
        }
    }

    private bool TryGetDepthOfField(out DepthOfField currentDepthOfField)
    {
        currentDepthOfField = null;

        if (postProcessVolume == null)
        {
            return false;
        }

        VolumeProfile volumeProfile;
        try
        {
            volumeProfile = postProcessVolume.profile;
        }
        catch (MissingReferenceException)
        {
            return false;
        }

        if (volumeProfile == null)
        {
            return false;
        }

        try
        {
            return volumeProfile.TryGet(out currentDepthOfField) && currentDepthOfField != null;
        }
        catch (MissingReferenceException)
        {
            currentDepthOfField = null;
            return false;
        }
    }

    private static void ApplyDepthOfFieldSettings(DepthOfField targetDepthOfField, float gaussianStart, float gaussianEnd)
    {
        if (targetDepthOfField == null)
        {
            return;
        }

        try
        {
            targetDepthOfField.active = true;
            targetDepthOfField.mode.Override(DepthOfFieldMode.Gaussian);
            targetDepthOfField.gaussianStart.Override(gaussianStart);
            targetDepthOfField.gaussianEnd.Override(gaussianEnd);
            targetDepthOfField.highQualitySampling.Override(true);
        }
        catch (MissingReferenceException)
        {
            // VolumeProfile再生成やPlay停止中に破棄済みなら、ぼかしだけ省略して進行する。
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
