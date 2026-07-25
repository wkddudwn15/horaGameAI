using System.Collections;
using TMPro;
using UnityEngine;

public class SubtitleController : MonoBehaviour
{
    [SerializeField] private CanvasGroup subtitleGroup;
    [SerializeField] private TextMeshProUGUI subtitleText;
    [SerializeField] private float fadeDuration = 0.35f;
    [SerializeField] private float displayDuration = 1.8f;

    private void Awake()
    {
        if (subtitleGroup != null)
        {
            subtitleGroup.alpha = 0f;
        }
    }

    public IEnumerator PlaySubtitleSequence(string[] lines, float interval)
    {
        if (lines == null)
        {
            yield break;
        }

        foreach (string line in lines)
        {
            yield return ShowLine(line);
            if (interval > 0f)
            {
                yield return new WaitForSeconds(interval);
            }
        }
    }

    private IEnumerator ShowLine(string line)
    {
        if (subtitleText == null || subtitleGroup == null)
        {
            yield break;
        }

        subtitleText.text = line;
        yield return Fade(0f, 1f);
        yield return new WaitForSeconds(displayDuration);
        yield return Fade(1f, 0f);
    }

    private IEnumerator Fade(float from, float to)
    {
        if (subtitleGroup == null)
        {
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            subtitleGroup.alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
            yield return null;
        }

        subtitleGroup.alpha = to;
    }
}
