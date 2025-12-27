using UnityEngine;
using UnityEngine.UI; // 記得要引入 UI 的命名空間

public class UIFlasher : MonoBehaviour
{
    [Header("設定")]
    [Tooltip("要閃爍的圖片組件，如果掛在同一個物件上可以不拉，會自動抓")]
    public Image targetImage;

    [Tooltip("閃爍的速度，數字越大越快")]
    [Range(0.1f, 10f)] // 限制調整範圍在 0.1 到 10 之間
    public float flashSpeed = 2f;

    [Header("透明度範圍 (深淺設定)")]
    [Tooltip("最淺時的透明度 (0 = 完全透明/最深, 1 = 完全不透明/最淺)")]
    [Range(0f, 1f)]
    public float minAlpha = 0.2f; // 建議不要設為 0，保留一點點影子比較好

    [Tooltip("最亮時的透明度")]
    [Range(0f, 1f)]
    public float maxAlpha = 1f;

    // 用來計算時間的正弦波變數
    private float timer;

    void Start()
    {
        // 如果沒有手動指定 targetImage，就嘗試抓取自己身上掛的 Image 組件
        if (targetImage == null)
        {
            targetImage = GetComponent<Image>();
        }

        // 如果還是找不到，就報錯提醒
        if (targetImage == null)
        {
            Debug.LogError("UIFlasher 腳本找不到 Image 組件！請確認它掛在有 Image 的物件上，或手動指定。");
            enabled = false; // 停用腳本以防報錯
        }
    }

    void Update()
    {
        // 如果沒有圖片就不執行
        if (targetImage == null) return;

        // 計算一個在 0 到 1 之間平滑來回變動的值 (利用 Sin 正弦波)
        // Time.time * flashSpeed 控制變動的頻率
        // (Mathf.Sin(...) + 1f) / 2f 是把原本 -1 到 1 的波形轉換成 0 到 1 的範圍
        timer = (Mathf.Sin(Time.time * flashSpeed) + 1f) / 2f;

        // 利用 Lerp 插值函數，根據 timer 的進度，在最小和最大透明度之間取值
        float newAlpha = Mathf.Lerp(minAlpha, maxAlpha, timer);

        // 先取出當前的顏色
        Color changingColor = targetImage.color;
        // 修改 Alpha 值 (透明度)
        changingColor.a = newAlpha;
        // 把修改後的顏色設定回去
        targetImage.color = changingColor;
    }

    // 這個方法可以讓你之後透過其他程式呼叫來開啟閃爍
    public void StartFlashing()
    {
        this.enabled = true;
    }

    // 這個方法可以讓你之後透過其他程式呼叫來關閉閃爍，並恢復到最亮狀態
    public void StopFlashing()
    {
        this.enabled = false;
        if (targetImage != null)
        {
            Color c = targetImage.color;
            c.a = maxAlpha; // 恢復到最大不透明度
            targetImage.color = c;
        }
    }
}