using UnityEngine;

/// 來源手的上下位移 -> 正規化到 0..1 -> 映射到相機前(固定深度)的視口區域。
/// 只做垂直：X 固定在 viewX，不做左右。支援 Play 開始時自動抓 X / Depth，
/// 但不會覆蓋你手動設定的 viewMinY / viewMaxY（方便你直接填 0.68 之類的值）。
[DefaultExecutionOrder(1000)]
public class HandAdaptiveMapperV2 : MonoBehaviour
{
    [Header("Debug / Outputs")]
[Range(0,1)] public float normalized01; // 目前手勢正規化值(0..1)

    [Header("Refs")]
    public Transform sourceRaw;   // RawLeft / RawRight
    public Camera cam;

    [Header("Viewport Rect (0~1)")]
    [Range(0,1)] public float viewX    = 0.22f;  // 左建議 0.22、右建議 0.78
    [Range(0,1)] public float viewMinY = 0.10f;  // 你可手動填
    [Range(0,1)] public float viewMaxY = 0.68f;  // 你可手動填（例：0.68）
    public float depth = 1.1f;                   // 與相機距離（決定大小）

    [Header("Init from current placement (開場定位)")]
    public bool captureOnStart = true;           // 開場用當前擺放位置來初始化
    public bool captureViewX   = true;           // 只抓 X
    public bool captureDepth   = true;           // 只抓 Depth
    // 注意：不抓 Y 範圍，讓你自己填 viewMinY / viewMaxY

    [Header("Smoothing")]
    public float inputSmooth  = 10f;             // 原始訊號平滑
    public float outputSmooth = 12f;             // 顯示位置平滑

    [Header("Auto Range (最近窗口自適應，決定 0..1 的正規化)")]
    public float trackWindowSec = 1.2f;          // 視窗長度（秒）
    public float guardMargin    = 0.02f;         // 上下界保險邊
    public float minRange       = 0.06f;         // 最小活動範圍，避免卡死

    // ---- private ----
    float smoothedRawY;
    float hi, lo;

    void Reset(){ cam = Camera.main; }

    void Start()
    {
        if (!cam) cam = Camera.main;
        if (!cam || !sourceRaw) return;

        if (captureOnStart)
        {
            var vp = cam.WorldToViewportPoint(transform.position);
            if (captureViewX) viewX  = Mathf.Clamp01(vp.x);
            if (captureDepth) depth  = Mathf.Max(0.01f, vp.z);
            // 不自動覆蓋 viewMinY / viewMaxY，讓你手動填想要的 0~1 數值（例：0.68）
        }

        float y = GetRaw();
        smoothedRawY = y;
        hi = lo = y;
    }

    void Update()
    {
        if (!cam || !sourceRaw) return;

        // 取來源手「世界 Y」(若你是 local，就改成 sourceRaw.localPosition.y)
        float raw = GetRaw();
        smoothedRawY = Mathf.Lerp(smoothedRawY, raw, 1f - Mathf.Exp(-inputSmooth * Time.deltaTime));

        // 追最近視窗的上下界
        float decay = Mathf.Exp(-Time.deltaTime / Mathf.Max(0.01f, trackWindowSec));
        hi = Mathf.Max(smoothedRawY + guardMargin, Mathf.Lerp(smoothedRawY, hi, decay));
        lo = Mathf.Min(smoothedRawY - guardMargin, Mathf.Lerp(smoothedRawY, lo, decay));
        float range = Mathf.Max(minRange, hi - lo);

        // 0..1 正規化 -> 對應到你設定的 Y 區間（只改 Y；X 固定在 viewX）
        float norm = Mathf.Clamp01((smoothedRawY - lo) / range);
        float vy   = Mathf.Lerp(viewMinY, viewMaxY, norm);
        Vector3 target = cam.ViewportToWorldPoint(new Vector3(viewX, vy, depth));

        // 位置平滑
        transform.position = Vector3.Lerp(transform.position, target,
                          1f - Mathf.Exp(-outputSmooth * Time.deltaTime));
                          normalized01 = norm;

    }

    float GetRaw() => sourceRaw.position.y;

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!cam) return;
        Vector3 a = cam.ViewportToWorldPoint(new Vector3(viewX - 0.04f, viewMinY, depth));
        Vector3 b = cam.ViewportToWorldPoint(new Vector3(viewX + 0.04f, viewMinY, depth));
        Vector3 c = cam.ViewportToWorldPoint(new Vector3(viewX + 0.04f, viewMaxY, depth));
        Vector3 d = cam.ViewportToWorldPoint(new Vector3(viewX - 0.04f, viewMaxY, depth));
        Gizmos.color = new Color(0,1,1,0.25f);
        Gizmos.DrawLine(a,b); Gizmos.DrawLine(b,c); Gizmos.DrawLine(c,d); Gizmos.DrawLine(d,a);
    }
#endif
}
