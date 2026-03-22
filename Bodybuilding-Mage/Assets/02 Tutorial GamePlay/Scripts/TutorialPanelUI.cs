using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class TutorialPanelUI : MonoBehaviour
{
    [Header("Panel Root")]
    public GameObject panelRoot;
    public CanvasGroup canvasGroup;

    [Header("彈跳動畫目標（通常是 Message 訊息框）")]
    public RectTransform messageRoot;

    [Header("可切換顯示物件")]
    public GameObject dimBackground;
    public GameObject videoRoot;

    [Header("Text")]
    public TextMeshProUGUI tutorialText;
    public float typewriterSpeed = 0.05f;

    [Header("Video")]
    public VideoPlayer videoPlayer;
    public RawImage videoRawImage;

    [Header("Q彈效果設定")]
    public float showDuration = 0.35f;
    public float hideDuration = 0.22f;
    public AnimationCurve showScaleCurve = new AnimationCurve(
        new Keyframe(0f, 0.75f),
        new Keyframe(0.5f, 1.08f),
        new Keyframe(0.75f, 0.97f),
        new Keyframe(1f, 1f)
    );
    public AnimationCurve hideScaleCurve = new AnimationCurve(
        new Keyframe(0f, 1f),
        new Keyframe(1f, 0.85f)
    );

    private Coroutine typingCoroutine;
    private bool isTyping = false;
    private Vector3 messageOriginalScale = Vector3.one;

    public bool IsTyping => isTyping;

    private void Awake()
    {
        if (panelRoot != null)
            panelRoot.SetActive(true);

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        if (messageRoot != null)
            messageOriginalScale = messageRoot.localScale;
    }

    public void SetVideo(VideoClip clip, bool loop = true)
    {
        if (videoPlayer == null) return;

        videoPlayer.Stop();
        videoPlayer.clip = clip;
        videoPlayer.isLooping = loop;
        videoPlayer.Play();
    }

    public void StopVideo()
    {
        if (videoPlayer == null) return;
        videoPlayer.Stop();
    }

    public void SetTextImmediate(string content)
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        isTyping = false;

        if (tutorialText != null)
            tutorialText.text = content;
    }

    public void PlayTypewriterText(string content)
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeTextRoutine(content));
    }

    IEnumerator TypeTextRoutine(string content)
    {
        isTyping = true;

        if (tutorialText != null)
            tutorialText.text = "";

        for (int i = 0; i < content.Length; i++)
        {
            if (tutorialText != null)
                tutorialText.text += content[i];

            yield return new WaitForSecondsRealtime(typewriterSpeed);
        }

        isTyping = false;
    }

    public void SetDimVisible(bool visible)
    {
        if (dimBackground != null)
            dimBackground.SetActive(visible);
    }

    public void SetVideoVisible(bool visible)
    {
        if (videoRoot != null)
            videoRoot.SetActive(visible);
    }

    public void ShowPracticeMessage(string content)
    {
        if (panelRoot != null && !panelRoot.activeSelf)
            panelRoot.SetActive(true);

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;

        SetDimVisible(false);
        SetVideoVisible(false);
        SetTextImmediate(content);

        if (messageRoot != null)
            messageRoot.localScale = messageOriginalScale;
    }

    public void ShowFinishMessage(string content, bool showDim = true)
    {
        if (panelRoot != null && !panelRoot.activeSelf)
            panelRoot.SetActive(true);

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;

        SetDimVisible(showDim);
        SetVideoVisible(false);
        SetTextImmediate(content);

        if (messageRoot != null)
            messageRoot.localScale = messageOriginalScale;
    }

    public void ShowInstant()
    {
        if (panelRoot != null)
            panelRoot.SetActive(true);

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;

        if (messageRoot != null)
            messageRoot.localScale = messageOriginalScale;
    }

    public void HideInstant()
    {
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    public IEnumerator FadeIn(float duration)
    {
        if (panelRoot != null && !panelRoot.activeSelf)
            panelRoot.SetActive(true);

        if (messageRoot != null)
            messageRoot.localScale = messageOriginalScale * 0.75f;

        float time = 0f;
        float finalDuration = duration > 0f ? duration : showDuration;

        while (time < finalDuration)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / finalDuration);

            if (canvasGroup != null)
                canvasGroup.alpha = t;

            if (messageRoot != null)
            {
                float s = showScaleCurve.Evaluate(t);
                messageRoot.localScale = messageOriginalScale * s;
            }

            yield return null;
        }

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;

        if (messageRoot != null)
            messageRoot.localScale = messageOriginalScale;
    }

    public IEnumerator FadeOut(float duration)
    {
        float time = 0f;
        float startAlpha = canvasGroup != null ? canvasGroup.alpha : 1f;
        float finalDuration = duration > 0f ? duration : hideDuration;

        while (time < finalDuration)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / finalDuration);

            if (canvasGroup != null)
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);

            if (messageRoot != null)
            {
                float s = hideScaleCurve.Evaluate(t);
                messageRoot.localScale = messageOriginalScale * s;
            }

            yield return null;
        }

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        if (messageRoot != null)
            messageRoot.localScale = messageOriginalScale;

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    public IEnumerator PopMessage(float duration = 0.18f, float punchScale = 1.08f)
    {
        if (messageRoot == null) yield break;

        Vector3 baseScale = messageOriginalScale;
        Vector3 targetScale = messageOriginalScale * punchScale;

        float half = duration * 0.5f;
        float time = 0f;

        while (time < half)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / half);
            messageRoot.localScale = Vector3.Lerp(baseScale, targetScale, t);
            yield return null;
        }

        time = 0f;
        while (time < half)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / half);
            messageRoot.localScale = Vector3.Lerp(targetScale, baseScale, t);
            yield return null;
        }

        messageRoot.localScale = baseScale;
    }
}