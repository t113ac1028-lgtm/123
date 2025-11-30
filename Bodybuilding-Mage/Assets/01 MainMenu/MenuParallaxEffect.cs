using UnityEngine;

public class MenuParallaxEffect : MonoBehaviour
{
    [Header("1. 綁定物件")]
    public RectTransform background;   // 拖入背景圖
    public RectTransform[] uiElements; // 拖入標題、按鈕 (可以多選)

    [Header("2. 視差強度 (自動飄移)")]
    public float bgParallax = 15f;     // 背景移動幅度 (建議小)
    public float uiParallax = 30f;     // UI 移動幅度 (建議大，產生深度差)
    public float smoothTime = 5f;      // 跟隨平滑度

    [Header("3. 自動晃動 (待機時的呼吸感)")]
    public float swayAmount = 10f;     // 晃動距離
    public float swaySpeed = 0.5f;     // 晃動速度 (越小越慢)

    [Header("4. 大範圍飄移 (取代滑鼠)")]
    public float driftAmplitude = 0.3f; // -0.3 ~ 0.3 類似原本的 -0.5 ~ 0.5
    public float driftSpeed = 0.2f;     // 飄移速度

    // 內部變數
    private Vector2 startBgPos;
    private Vector2[] startUiPos;

    void Start()
    {
        // 記錄所有東西的一開始位置
        if (background) startBgPos = background.anchoredPosition;

        startUiPos = new Vector2[uiElements.Length];
        for (int i = 0; i < uiElements.Length; i++)
        {
            if (uiElements[i]) startUiPos[i] = uiElements[i].anchoredPosition;
        }
    }

    void Update()
    {
        float t = Time.time;

        // --- A. 用時間產生「假滑鼠偏移」 ---
        // 讓畫面慢慢往左、右／上、下飄
        float autoX = Mathf.Sin(t * driftSpeed) * driftAmplitude;          // -driftAmplitude ~ +driftAmplitude
        float autoY = Mathf.Cos(t * driftSpeed * 1.3f) * driftAmplitude;   // 用不同倍數讓 X/Y 不同步

        float mouseX = autoX; // 直接拿來當原本的 mouseX
        float mouseY = autoY; // 直接拿來當原本的 mouseY

        // --- B. 計算自動晃動量 (呼吸感) ---
        float swayX = Mathf.Sin(t * swaySpeed) * swayAmount;
        float swayY = Mathf.Cos(t * swaySpeed * 0.8f) * swayAmount;
        Vector2 swayOffset = new Vector2(swayX, swayY);

        // --- C. 應用到背景 (移動少一點) ---
        if (background)
        {
            // 目標位置 = 初始 + 假滑鼠反向偏移 + 自動晃動
            Vector2 targetBg = startBgPos
                               + new Vector2(-mouseX * bgParallax, -mouseY * bgParallax)
                               + (swayOffset * 0.5f);

            background.anchoredPosition =
                Vector2.Lerp(background.anchoredPosition, targetBg, Time.deltaTime * smoothTime);
        }

        // --- D. 應用到 UI (移動多一點，產生立體感) ---
        for (int i = 0; i < uiElements.Length; i++)
        {
            if (uiElements[i])
            {
                Vector2 targetUi = startUiPos[i]
                                   + new Vector2(mouseX * uiParallax, mouseY * uiParallax)
                                   + swayOffset;

                uiElements[i].anchoredPosition =
                    Vector2.Lerp(uiElements[i].anchoredPosition, targetUi, Time.deltaTime * smoothTime);
            }
        }
    }
}
