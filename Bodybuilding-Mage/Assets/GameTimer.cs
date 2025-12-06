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

    private float timeLeft;
    private bool running;
    private bool timeUpSent;

    private void Start()
    {
        timeLeft = startSeconds;
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
}
