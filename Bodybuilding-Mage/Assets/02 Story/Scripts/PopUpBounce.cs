using System.Collections;
using UnityEngine;

// 這個腳本要掛在有 RectTransform 的 UI 物件上
[RequireComponent(typeof(RectTransform))]
public class PopUpBounceLoop : MonoBehaviour
{
    [Header("動畫設定")]
    [Tooltip("完成「一次」彈出動畫所需的時間 (秒)")]
    public float duration = 0.6f;

    [Tooltip("兩次彈跳動作之間的休息間隔時間 (秒)。設為 0 就會完全無縫接軌。")]
    public float intervalDelay = 0.8f; // 建議給一點點休息時間，看起來比較自然

    [Tooltip("彈出的曲線。記得要高過 1 才會 Q 喔！")]
    // 預設一個 Q 彈曲線
    public AnimationCurve bounceCurve = new AnimationCurve(
        new Keyframe(0f, 0f, 0f, 5f),    // 時間0，大小0 (開始)
        new Keyframe(0.7f, 1.15f),       // 時間0.7，大小1.15 (衝過頭)
        new Keyframe(0.9f, 0.95f),       // 時間0.9，大小0.95 (縮回來)
        new Keyframe(1f, 1f)             // 時間1，大小1 (停住)
    );

    private RectTransform targetRect;

    void Awake()
    {
        // 抓取自己的 RectTransform 組件，用來控制大小
        targetRect = GetComponent<RectTransform>();
    }

    // OnEnable 會在物件被開啟 (SetActive true) 的時候執行
    void OnEnable()
    {
        // 開始執行「循環」動畫的協程
        StartCoroutine(LoopAnimationProcess());
    }

    // OnDisable 會在物件被關閉或切換場景時執行
    void OnDisable()
    {
        // 務必停止所有協程，避免物件消失了程式還在背景跑而報錯
        StopAllCoroutines();
    }

    // === 主要的循環控制協程 ===
    IEnumerator LoopAnimationProcess()
    {
        // 【重點】while(true) 是一個無窮迴圈，讓裡面的動作永遠重複執行
        while (true)
        {
            float timer = 0f;
            // 每一輪開始前，先把大小重置為 0 (隱藏起來，準備彈出)
            targetRect.localScale = Vector3.zero;

            // --- 開始執行單次 Q 彈動畫 ---
            while (timer < duration)
            {
                timer += Time.deltaTime;
                // 計算目前進度百分比 (0 到 1)
                float percentage = Mathf.Clamp01(timer / duration);

                // 根據進度查詢曲線上的數值
                float scaleValue = bounceCurve.Evaluate(percentage);

                // 套用縮放大小
                targetRect.localScale = new Vector3(scaleValue, scaleValue, 1f);

                // 等待下一幀
                yield return null;
            }

            // 動畫結束，確保大小精準停在 1
            targetRect.localScale = Vector3.one;
            // --- 單次動畫結束 ---

            // 【重點】動畫跑完一次後，休息一下
            if (intervalDelay > 0)
            {
                // 等待設定的間隔秒數
                yield return new WaitForSeconds(intervalDelay);
            }

            // 休息完畢，while(true) 迴圈會回到最上面
            // 重新把大小設為 0，再次開始新的一輪彈跳
        }
    }
}