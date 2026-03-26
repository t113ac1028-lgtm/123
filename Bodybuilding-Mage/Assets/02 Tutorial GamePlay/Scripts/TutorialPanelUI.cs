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

    [Header("黑幕")]
    public GameObject dimBackground;

    [Header("文字")]
    public TextMeshProUGUI tutorialText;
    public float typewriterSpeed = 0.05f;

    [Header("影片區塊")]
    public GameObject videoGroup;
    public GameObject videoFrame;
    public GameObject videoRoot;
    public RawImage videoRawImage;
    public VideoPlayer videoPlayer;

    [Header("前導圖片區塊")]
    public GameObject imageGroup;
    public GameObject imageFrame;
    public Image introImage;
    public CanvasGroup imageCanvasGroup;

    [Header("READY 圖片（直接用你手動擺好的物件）")]
    public GameObject readyObject;

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

    [Header("浮動效果")]
    public RectTransform videoFloatTarget;
    public RectTransform imageFloatTarget;
    public RectTransform readyFloatTarget;
    public float floatAmplitude = 8f;
    public float floatSpeed = 1.3f;
    public bool enableFloating = true;

    private Coroutine typingCoroutine;
    private bool isTyping = false;
    private Vector3 messageOriginalScale = Vector3.one;

    private Vector2 videoOriginalAnchoredPos;
    private Vector2 imageOriginalAnchoredPos;
    private Vector2 readyOriginalAnchoredPos;

    public bool IsTyping => isTyping;

    private void Awake()
    {
        if (panelRoot != null)
            panelRoot.SetActive(true);

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        if (messageRoot != null)
            messageOriginalScale = messageRoot.localScale;

        if (videoFloatTarget != null)
            videoOriginalAnchoredPos = videoFloatTarget.anchoredPosition;

        if (imageFloatTarget != null)
            imageOriginalAnchoredPos = imageFloatTarget.anchoredPosition;

        if (readyFloatTarget != null)
            readyOriginalAnchoredPos = readyFloatTarget.anchoredPosition;

        if (imageCanvasGroup != null)
            imageCanvasGroup.alpha = 1f;
    }

    private void Update()
    {
        if (!enableFloating) return;

        float offset = Mathf.Sin(Time.unscaledTime * floatSpeed) * floatAmplitude;

        if (videoFloatTarget != null && IsVideoVisible())
        {
            videoFloatTarget.anchoredPosition = videoOriginalAnchoredPos + new Vector2(0f, offset);
        }

        if (imageFloatTarget != null && IsImageVisible())
        {
            imageFloatTarget.anchoredPosition = imageOriginalAnchoredPos + new Vector2(0f, offset);
        }

        if (readyFloatTarget != null && IsReadyVisible())
        {
            readyFloatTarget.anchoredPosition = readyOriginalAnchoredPos + new Vector2(0f, offset);
        }
    }

    bool IsVideoVisible()
    {
        return videoGroup != null && videoGroup.activeSelf;
    }

    bool IsImageVisible()
    {
        return imageGroup != null && imageGroup.activeSelf;
    }

    bool IsReadyVisible()
    {
        return readyObject != null && readyObject.activeSelf;
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

    public void SetIntroImage(Sprite sprite)
    {
        if (introImage != null)
        {
            introImage.sprite = sprite;
        }

        if (imageCanvasGroup != null)
        {
            imageCanvasGroup.alpha = 1f;
        }
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
        if (videoGroup != null)
            videoGroup.SetActive(visible);
        else
        {
            if (videoFrame != null) videoFrame.SetActive(visible);
            if (videoRoot != null) videoRoot.SetActive(visible);
        }

        if (!visible)
            StopVideo();
    }

    public void SetImageVisible(bool visible, bool showFrame = true)
    {
        if (imageGroup != null)
            imageGroup.SetActive(visible);

        if (imageFrame != null)
            imageFrame.SetActive(visible && showFrame);

        if (introImage != null)
            introImage.gameObject.SetActive(visible);

        if (imageCanvasGroup != null && visible)
        {
            imageCanvasGroup.alpha = 1f;
        }
    }

    public void SetReadyVisible(bool visible)
    {
        if (readyObject != null)
            readyObject.SetActive(visible);
    }

    public void ShowPracticeMessage(string content)
    {
        if (panelRoot != null && !panelRoot.activeSelf)
            panelRoot.SetActive(true);

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;

        SetDimVisible(false);
        SetVideoVisible(false);
        SetImageVisible(false);
        SetReadyVisible(false);
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
        SetImageVisible(false);
        SetReadyVisible(false);
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