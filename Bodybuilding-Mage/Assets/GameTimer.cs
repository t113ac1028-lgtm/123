using UnityEngine;
using TMPro;

public class GameTimer : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI timerText;

    [Header("Settings")]
    [SerializeField] private float startSeconds = 30f;

    [Header("Refs")]
    [SerializeField] private GamePlayController controller;

    [Header("Slam Phase & Warning")]
    [Tooltip("進入 Slam 階段的時間點（要跟其他腳本的設定對齊，例如 15 秒）")]
    [SerializeField] private float slamPhaseThreshold = 15f;

    [Tooltip("在 Slam 前要提前幾秒開始提醒，例如 2 秒 → 17~15 秒會閃爍")]
    [SerializeField] private float warningLeadSeconds = 2f;

    [Tooltip("閃爍速度（越大越快速）")]
    [SerializeField] private float warningFlashSpeed = 6f;

    [Tooltip("閃爍時最大放大倍率")]
    [SerializeField] private float warningMaxScale = 1.3f;

    [Tooltip("倒數字一般狀態顏色")]
    [SerializeField] private Color normalColor = Color.white;

    [Tooltip("進入 Slam 前提醒 & Slam 階段的顏色")]
    [SerializeField] private Color warningColor = Color.red;

    private float timeLeft;
    private bool running;
    private bool timeUpSent;

    // 記錄原本 scale，避免亂掉
    private Vector3 baseScale;

    public float TimeLeft => timeLeft;

    private void Start()
    {
        timeLeft = startSeconds;
        if (timerText != null)
        {
            baseScale = timerText.rectTransform.localScale;
            timerText.color = normalColor;
        }
        UpdateText();
    }

    private void Update()
    {
        // 還沒開始：等 Countdown 決定「正式開始遊戲」
        if (!running)
        {
            if (Countdown.gameStarted && !timeUpSent)
            {
                running = true;
            }
            else
            {
                return;
            }
        }

        if (timeUpSent) return;

        // 正在倒數
        timeLeft -= Time.deltaTime;
        if (timeLeft < 0f) timeLeft = 0f;

        UpdateText();
        UpdateWarningVisual();  // ★ 更新提醒動畫

        // 時間到
        if (timeLeft <= 0f)
        {
            running    = false;
            timeUpSent = true;

            Debug.Log("[GameTimer] Time up.");

            if (controller != null)
            {
                controller.OnTimerFinished();
            }
            else
            {
                Debug.LogWarning("[GameTimer] 沒有指定 GamePlayController，無法通知結束。");
            }
        }
    }

    private void UpdateText()
    {
        if (timerText != null)
        {
            timerText.text = Mathf.CeilToInt(timeLeft).ToString();
        }
    }

    // 控制 17~15 秒閃爍 & 15 秒後固定紅色
    private void UpdateWarningVisual()
    {
        if (timerText == null) return;

        float warnStart = slamPhaseThreshold + warningLeadSeconds;

        if (timeLeft <= slamPhaseThreshold)
        {
            // 已進入 Slam 段：固定紅色，scale 回到 1
            timerText.color = warningColor;
            timerText.rectTransform.localScale = baseScale;
        }
        else if (timeLeft <= warnStart)
        {
            // 在「提醒區間」：顏色與大小閃爍
            float t = Mathf.PingPong(Time.time * warningFlashSpeed, 1f);

            timerText.color = Color.Lerp(normalColor, warningColor, t);

            float scale = Mathf.Lerp(1f, warningMaxScale, t);
            timerText.rectTransform.localScale = baseScale * scale;
        }
        else
        {
            // 還沒到提醒時間：保持一般狀態
            timerText.color = normalColor;
            timerText.rectTransform.localScale = baseScale;
        }
    }
}
