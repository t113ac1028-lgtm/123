using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GameTimer : MonoBehaviour
{
    public TMP_Text timerText;   // 把你的 Text (TMP) 拉進來
    public float startSeconds = 30f;
    public bool autoStart = true;

    float remaining;
    bool running;
    // Start is called before the first frame update
    void Start()
    {
        Time.timeScale = 1f;    // 確保一開始不是暫停
        remaining = startSeconds;
        UpdateText(remaining);
        running = autoStart; // autoStart是決定是否自動開始計時的指令
    }

    // Update is called once per frame
    void Update()
    {
        if (!Countdown.gameStarted) return;  // 還在倒數就先不做事
        if (!running) return;

        remaining -= Time.deltaTime;   // 用遊戲時間倒數
        if (remaining <= 0f)
        {
            remaining = 0f;
            UpdateText(remaining);
            running = false;
            Time.timeScale = 0f;       // 歸零→暫停遊戲
            return;
        }
        UpdateText(remaining);
    }

    void UpdateText(float seconds)
    {
        if (timerText == null) return;
        int s = Mathf.CeilToInt(seconds);
        // 只顯示秒數
        timerText.text = s.ToString();

        // 如果想要 mm:ss 顯示，改成下面這行：
        // timerText.text = $"{Mathf.FloorToInt(seconds/60f):00}:{Mathf.CeilToInt(seconds%60f):00}";
    }
}
