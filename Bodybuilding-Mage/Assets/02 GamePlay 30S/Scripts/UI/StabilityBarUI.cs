using UnityEngine;
using UnityEngine.UI;

public class StabilityBarUI : MonoBehaviour
{
    [Header("來源")]
    public DamageCalculator damageCalc;   // 拖場景裡的 DamageCalculator 進來

    [Header("UI 元件")]
    public RectTransform barArea;         // 條：BarBg 的 RectTransform
    public RectTransform pointer;         // 指針：Pointer 的 RectTransform
    public Image pointerImage;            // 指針的 Image（要變色就改它）

    [Header("平滑設定")]
    public float smoothSpeed = 8f;        // 越大越跟得緊，越小越滑順

    [Header("顏色區間（可選）")]
    public Color slowColor  = Color.cyan;    // 太慢 / 不穩
    public Color idealColor = Color.green;   // 範圍內
    public Color fastColor  = Color.red;     // 太快

    float displayT = 0.5f;   // 目前用來顯示的位置(0~1)，一開始先放中間

    void Start()
    {
        // 一開始先放條中央
        displayT = 0.5f;
        UpdatePointerPosition();
    }

    void Update()
    {
        if (!damageCalc || !barArea || !pointer) return;

        // 1. 從 DamageCalculator 拿當前頻率
        float hz = damageCalc.lastHz;

        // 2. 把頻率映射到 0~1，得到 targetT
        float minHz = damageCalc.targetHzMin * 0.5f;   // 左端：比理想慢很多
        float maxHz = damageCalc.targetHzMax * 1.5f;   // 右端：比理想快很多
        float targetT = Mathf.InverseLerp(minHz, maxHz, hz);
        targetT = Mathf.Clamp01(targetT);

        // 3. 用 Lerp 讓 displayT 慢慢追上 targetT → 這樣就會滑順
        displayT = Mathf.Lerp(displayT, targetT, Time.deltaTime * smoothSpeed);

        // 4. 根據 displayT 更新指針位置
        UpdatePointerPosition();

        // 5. 如果要提示慢 / 中 / 快，就用顏色改「指針」即可
        if (pointerImage)
        {
            float idealMinT = Mathf.InverseLerp(minHz, maxHz, damageCalc.targetHzMin);
            float idealMaxT = Mathf.InverseLerp(minHz, maxHz, damageCalc.targetHzMax);

            Color c;
            if (displayT < idealMinT)       c = slowColor;
            else if (displayT > idealMaxT)  c = fastColor;
            else                            c = idealColor;

            pointerImage.color = c;
        }
    }

    void UpdatePointerPosition()
    {
        float width = barArea.rect.width;
        float leftX  = -width * 0.5f;
        float rightX =  width * 0.5f;

        Vector2 p = pointer.anchoredPosition;
        p.x = Mathf.Lerp(leftX, rightX, displayT);
        pointer.anchoredPosition = p;
    }
}
