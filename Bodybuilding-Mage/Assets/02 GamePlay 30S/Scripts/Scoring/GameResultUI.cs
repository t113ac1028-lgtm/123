using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;

public class GameResultUI : MonoBehaviour
{
    [Header("UI 元件參考")]
    [Tooltip("黑色半透明背景 (這個會保留)")]
    public CanvasGroup backgroundGroup;
    
    [Tooltip("結算主面板 (用來做彈出動畫)")]
    public RectTransform mainPanel;

    [Header("離開設定")]
    [Tooltip("切換到排行榜時，要淡出的物件 (請在你的結算圖案物件上加 CanvasGroup 並拖進來)")]
    public CanvasGroup contentToFade;

    [Header("數字顯示 (請拖曳 Text)")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI comboText;
    public TextMeshProUGUI bestScoreText;
    public TextMeshProUGUI bestComboText;
    public TextMeshProUGUI playerInfoText;

    [Header("狀態提示")]
    [Tooltip("用來顯示 'Uploading...' 或 'Success' 的文字框")]
    public TextMeshProUGUI uploadStatusText;

    // ★★★ 新增：音效設定 ★★★
    [Header("音效設定")]
    [Tooltip("請掛一個 AudioSource 在這個物件上並拖進來")]
    public AudioSource audioSource;
    [Tooltip("跑分時的音效 (循環播放，例如快速的嘟嘟聲)")]
    public AudioClip rollingSfx;
    [Tooltip("分數定住時的音效 (單次播放，例如 鏘！)")]
    public AudioClip finishSfx;

    [Header("動畫設定")]
    public float fadeInDuration = 0.5f;
    public float popUpDuration = 0.6f;
    public float numberRollDuration = 1.0f;
    public AnimationCurve popCurve = new AnimationCurve(
        new Keyframe(0, 0), 
        new Keyframe(0.5f, 1.1f),
        new Keyframe(0.8f, 0.95f), 
        new Keyframe(1, 1)
    );

    [Header("待機晃動設定")]
    public float shakeAngle = 2.0f;
    public float shakeSpeed = 2.0f;

    private bool isAnimationDone = false;

    void Awake()
    {
        if (backgroundGroup) 
        {
            backgroundGroup.alpha = 0;
            backgroundGroup.gameObject.SetActive(false);
        }
        if (mainPanel) 
        {
            mainPanel.localScale = Vector3.zero;
        }
        
        if (contentToFade)
        {
            contentToFade.alpha = 1f;
            contentToFade.gameObject.SetActive(true);
        }

        if (uploadStatusText) uploadStatusText.text = "";

        // 自動抓取 AudioSource (如果你忘了拉)
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        if (isAnimationDone && mainPanel != null)
        {
            float z = Mathf.Sin(Time.time * shakeSpeed) * shakeAngle;
            mainPanel.localRotation = Quaternion.Euler(0, 0, z);
        }
    }

    public void ShowResult(int score, int combo, int bestScore, int bestCombo, string playerId, int rank, bool isRankUp)
    {
        gameObject.SetActive(true);
        if (backgroundGroup) backgroundGroup.gameObject.SetActive(true);
        
        if (contentToFade) 
        {
            contentToFade.alpha = 1f;
            contentToFade.gameObject.SetActive(true);
        }

        if (bestScoreText) bestScoreText.text = bestScore.ToString();
        if (bestComboText) bestComboText.text = bestCombo.ToString();

        if (playerInfoText)
        {
            string displayId = string.IsNullOrEmpty(playerId) ? "Guest" : playerId;
            
            // 設定箭頭
            string rankSuffix = isRankUp ? " <color=#00FF00>↑</color>" : ""; 
            
            playerInfoText.text = $"{displayId}   -   Rank :   {rank}{rankSuffix}";
        }

        if (uploadStatusText) uploadStatusText.text = "";

        StartCoroutine(ShowSequence(score, combo));
    }

    public void SetUploadStatus(string message, Color color)
    {
        if (uploadStatusText != null)
        {
            uploadStatusText.text = message;
            uploadStatusText.color = color;
        }
    }

    IEnumerator ShowSequence(int targetScore, int targetCombo)
    {
        // --- A. 背景變黑 ---
        float t = 0;
        while (t < 1.0f)
        {
            t += Time.unscaledDeltaTime / fadeInDuration;
            if (backgroundGroup) backgroundGroup.alpha = Mathf.Lerp(0, 1, t);
            yield return null;
        }

        // --- B. 面板 Q 彈跳出 ---
        t = 0;
        while (t < 1.0f)
        {
            t += Time.unscaledDeltaTime / popUpDuration;
            float scale = popCurve.Evaluate(t);
            if (mainPanel) mainPanel.localScale = Vector3.one * scale;
            yield return null;
        }

        // --- C. 數字滾動 (碼表效果) ---
        
        // ★ 開始播放跑分音效 (循環)
        if (audioSource != null && rollingSfx != null)
        {
            audioSource.clip = rollingSfx;
            audioSource.loop = true; // 設定為循環
            audioSource.Play();
        }

        t = 0;
        while (t < 1.0f)
        {
            t += Time.unscaledDeltaTime / numberRollDuration;
            float progress = 1f - Mathf.Pow(1f - t, 3); 

            int curScore = Mathf.RoundToInt(Mathf.Lerp(0, targetScore, progress));
            int curCombo = Mathf.RoundToInt(Mathf.Lerp(0, targetCombo, progress));

            if (scoreText) scoreText.text = curScore.ToString();
            if (comboText) comboText.text = curCombo.ToString();

            yield return null;
        }

        // 確保最後數字是對的
        if (scoreText) scoreText.text = targetScore.ToString();
        if (comboText) comboText.text = targetCombo.ToString();

        // ★ 停止跑分音效，播放定格音效
        if (audioSource != null)
        {
            audioSource.Stop();
            audioSource.loop = false; // 取消循環
            
            if (finishSfx != null)
            {
                audioSource.PlayOneShot(finishSfx);
            }
        }

        isAnimationDone = true; 
    }

    public IEnumerator FadeOutBoardRoutine(float duration)
    {
        if (contentToFade != null)
        {
            float t = 0;
            float startAlpha = contentToFade.alpha;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / duration;
                contentToFade.alpha = Mathf.Lerp(startAlpha, 0f, t);
                yield return null;
            }
            contentToFade.alpha = 0f;
            contentToFade.gameObject.SetActive(false); 
        }
        else
        {
            if (mainPanel != null) mainPanel.localScale = Vector3.zero;
        }
    }
}