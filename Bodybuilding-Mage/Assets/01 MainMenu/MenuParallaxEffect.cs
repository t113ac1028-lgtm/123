using UnityEngine;

[System.Serializable]
public class ParallaxLayer
{
    public RectTransform rect;   // 這一層的圖
    public float parallax = 10f; // 視差強度（越小越遠）
    [Range(0f, 2f)]
    public float swayMultiplier = 0.5f; // 這層受到晃動影響的比例
}

public class MenuParallaxEffect : MonoBehaviour
{
    [Header("1. 背景圖層（遠景 / 中景 / 近景）")]
    public ParallaxLayer[] backgrounds;   // 多個圖層都丟進來

    [Header("2. UI 元素（標題、按鈕等）")]
    public RectTransform[] uiElements;

    [Header("3. 視差強度（UI 用）")]
    public float uiParallax = 30f;     // UI 移動幅度（通常比背景大）
    public float smoothTime = 5f;      // 跟隨平滑度

    [Header("4. 自動晃動 (呼吸感)")]
    public float swayAmount = 10f;     // 晃動距離
    public float swaySpeed = 0.5f;     // 晃動速度

    [Header("5. 大範圍飄移 (取代滑鼠)")]
    public float driftAmplitude = 0.3f;
    public float driftSpeed = 0.2f;

    // 內部變數
    private Vector2[] startBgPos;
    private Vector2[] startUiPos;

    void Start()
    {
        // 記錄每一層背景的一開始位置
        startBgPos = new Vector2[backgrounds.Length];
        for (int i = 0; i < backgrounds.Length; i++)
        {
            if (backgrounds[i].rect)
                startBgPos[i] = backgrounds[i].rect.anchoredPosition;
        }

        // 記錄每個 UI 的一開始位置
        startUiPos = new Vector2[uiElements.Length];
        for (int i = 0; i < uiElements.Length; i++)
        {
            if (uiElements[i])
                startUiPos[i] = uiElements[i].anchoredPosition;
        }
    }

    void Update()
    {
        float t = Time.time;

        // A. 大範圍飄移（假滑鼠）
        float autoX = Mathf.Sin(t * driftSpeed) * driftAmplitude;
        float autoY = Mathf.Cos(t * driftSpeed * 1.3f) * driftAmplitude;
        float mouseX = autoX;
        float mouseY = autoY;

        // B. 呼吸晃動
        float swayX = Mathf.Sin(t * swaySpeed) * swayAmount;
        float swayY = Mathf.Cos(t * swaySpeed * 0.8f) * swayAmount;
        Vector2 swayOffset = new Vector2(swayX, swayY);

        // C. 套用到每一層背景
        for (int i = 0; i < backgrounds.Length; i++)
        {
            var layer = backgrounds[i];
            if (layer.rect == null) continue;

            Vector2 target = startBgPos[i]
                             + new Vector2(-mouseX * layer.parallax,
                                           -mouseY * layer.parallax)
                             + swayOffset * layer.swayMultiplier;

            layer.rect.anchoredPosition =
                Vector2.Lerp(layer.rect.anchoredPosition, target, Time.deltaTime * smoothTime);
        }

        // D. 套用到 UI（標題、按鈕）
        for (int i = 0; i < uiElements.Length; i++)
        {
            if (uiElements[i] == null) continue;

            Vector2 targetUi = startUiPos[i]
                               + new Vector2(mouseX * uiParallax,
                                             mouseY * uiParallax)
                               + swayOffset;

            uiElements[i].anchoredPosition =
                Vector2.Lerp(uiElements[i].anchoredPosition, targetUi, Time.deltaTime * smoothTime);
        }
    }
}
