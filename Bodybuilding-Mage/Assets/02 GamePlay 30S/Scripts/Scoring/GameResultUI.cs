using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;

public class GameResultUI : MonoBehaviour
{
    [Header("UI 元件參考")]
    [Tooltip("黑色半透明背景 (這個會保留，不會被關掉)")]
    public CanvasGroup backgroundGroup;
    
    [Tooltip("結算主面板 (用來做彈出動畫)")]
    public RectTransform mainPanel;

    // ★★★ 新增：指定要淡出的物件 ★★★
    [Header("離開設定")]
    [Tooltip("切換到排行榜時，要淡出的物件 (請在你的結算圖案物件上加 CanvasGroup 並拖進來)")]
    public CanvasGroup contentToFade;

    [Header("數字顯示 (請拖曳 Text)")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI comboText;
    public TextMeshProUGUI bestScoreText;
    public TextMeshProUGUI bestComboText;
    public TextMeshProUGUI playerInfoText;

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
        
        // 確保內容一開始是可見的 (Alpha = 1)
        if (contentToFade)
        {
            contentToFade.alpha = 1f;
            contentToFade.gameObject.SetActive(true);
        }
    }

    void Update()
    {
        if (isAnimationDone && mainPanel != null)
        {
            float z = Mathf.Sin(Time.time * shakeSpeed) * shakeAngle;
            mainPanel.localRotation = Quaternion.Euler(0, 0, z);
        }
    }

    public void ShowResult(int score, int combo, int bestScore, int bestCombo, string playerId, int rank)
    {
        gameObject.SetActive(true);
        if (backgroundGroup) backgroundGroup.gameObject.SetActive(true);
        
        // 重置狀態：確保內容是開著的，不然後面再玩一次會看不到
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
            playerInfoText.text = $"{displayId}   -   Rank :   {rank}";
        }

        StartCoroutine(ShowSequence(score, combo));
    }

    IEnumerator ShowSequence(int targetScore, int targetCombo)
    {
        float t = 0;
        while (t < 1.0f)
        {
            t += Time.unscaledDeltaTime / fadeInDuration;
            if (backgroundGroup) backgroundGroup.alpha = Mathf.Lerp(0, 1, t);
            yield return null;
        }

        t = 0;
        while (t < 1.0f)
        {
            t += Time.unscaledDeltaTime / popUpDuration;
            float scale = popCurve.Evaluate(t);
            if (mainPanel) mainPanel.localScale = Vector3.one * scale;
            yield return null;
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

        if (scoreText) scoreText.text = targetScore.ToString();
        if (comboText) comboText.text = targetCombo.ToString();

        isAnimationDone = true; 
    }

    // ★★★ 新增：專門用來淡出內容的協程 ★★★
    public IEnumerator FadeOutBoardRoutine(float duration)
    {
        if (contentToFade != null)
        {
            float t = 0;
            float startAlpha = contentToFade.alpha;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / duration;
                // 只淡出你指定的那個物件，背景不動
                contentToFade.alpha = Mathf.Lerp(startAlpha, 0f, t);
                yield return null;
            }
            contentToFade.alpha = 0f;
            contentToFade.gameObject.SetActive(false); // 淡出完再把這個子物件關掉 (不關父物件)
        }
        else
        {
            // 如果沒設定，就縮小 MainPanel 當作備案
            if (mainPanel != null) mainPanel.localScale = Vector3.zero;
        }
    }
}