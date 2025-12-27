using UnityEngine;

/// 讓「手部模型(子物件)」依據 HandAdaptiveMapperV2 的 normalized01 轉動 X 軸
/// - 左手：X 往上加 = 往下揮
/// - 右手：X 往上加 = 抬手
[DefaultExecutionOrder(1100)] // 確保比 HandAdaptiveMapperV2(1000) 晚更新
public class HandSwingRotation : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private HandAdaptiveMapperV2 mapper; // 同一隻手物件上那個 Mapper
    [SerializeField] private Transform handModel;         // 你要旋轉的「手部模型」(不是 Hand_L_Root)

    [Header("Hand Side")]
    [SerializeField] private bool isLeftHand = true;      // 左手勾、右手不勾

    [Header("Angle Mapping (你來調)")]
    [Tooltip("normalized=0 時的角度增量 (度)")]
    [SerializeField] private float angleAtNorm0 = -10f;

    [Tooltip("normalized=1 時的角度增量 (度)")]
    [SerializeField] private float angleAtNorm1 = 25f;

    [Tooltip("把 0..1 再用曲線修飾：上揮/下揮更自然")]
    [SerializeField] private AnimationCurve response = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Smoothing")]
    [Tooltip("越大越跟手、越小越柔")]
    [SerializeField] private float rotateSmooth = 18f;

    [Header("Axis Lock")]
    [Tooltip("只動 X，不動 YZ")]
    [SerializeField] private bool onlyAffectX = true;

    // ---- runtime ----
    private Quaternion baseLocalRot;

    void Reset()
    {
        // 自動抓（如果你把它掛在手模型上）
        handModel = transform;
    }

    void Start()
    {
        if (!handModel) handModel = transform;
        baseLocalRot = handModel.localRotation;
    }

    void Update()
    {
        if (!mapper || !handModel) return;

        // 0..1
        float t = Mathf.Clamp01(mapper.normalized01);
        t = Mathf.Clamp01(response.Evaluate(t));

        // 依 0..1 映射到「角度增量」
        float deltaX = Mathf.Lerp(angleAtNorm0, angleAtNorm1, t);

        // 你提到左右手 X 方向相反
        // 左手：X 往上加 = 往下揮  (deltaX 正向視為下揮)
        // 右手：X 往上加 = 抬手    (所以同一套 deltaX 需要反向)
        if (!isLeftHand)
            deltaX = -deltaX;

        Quaternion target;
        if (onlyAffectX)
        {
            // 以你手動調好的原始角度為基底，只疊加 X
            Vector3 e = baseLocalRot.eulerAngles;
            // Euler 180 度跳動問題：先轉成 -180..180
            float baseX = NormalizeAngle(e.x);
            float newX = baseX + deltaX;

            target = Quaternion.Euler(newX, e.y, e.z);
        }
        else
        {
            target = baseLocalRot * Quaternion.Euler(deltaX, 0f, 0f);
        }

        // 平滑
        float k = 1f - Mathf.Exp(-rotateSmooth * Time.deltaTime);
        handModel.localRotation = Quaternion.Slerp(handModel.localRotation, target, k);
    }

    private float NormalizeAngle(float a)
    {
        a %= 360f;
        if (a > 180f) a -= 360f;
        return a;
    }

    [ContextMenu("Capture Current As Base")]
    public void CaptureCurrentAsBase()
    {
        if (!handModel) return;
        baseLocalRot = handModel.localRotation;
    }
}
