using UnityEngine;
using UnityEngine.Events;

public class DownSwingDetector : MonoBehaviour
{
    [Header("Strength Mapping")]
    [Tooltip("向下速度 <= 這個值時，strength ≈ 0")]
    public float strengthSpeedMin = 3.0f;

    [Tooltip("向下速度 >= 這個值時，strength ≈ 1")]
    public float strengthSpeedMax = 8.0f;

    [Header("Source")]
    public Transform hand; // 留空用自身

    [Header("Reference Down Axis (重要)")]
    [Tooltip("拖 Main Camera 進來。用玩家視角的「下」當判定基準，Joy-Con 歪一點也不會死。")]
    public Transform referenceDown; // Main Camera

    [Header("Speed Gates (already stable)")]
    public float minDownSpeed = 3.2f;
    public float minTotalSpeed = 2.0f;
    public float verticalDominance = 1.8f;
    [Range(0f, 89f)] public float maxAngleFromDown = 35f;

    [Header("Reset / Cooldown")]
    public float resetDownSpeed = 2.4f;
    public float cooldown = 0.2f;
    [Range(0f, 1f)] public float velocitySmoothing = 0.3f;

    [Header("Warmup / Baseline")]
    public float warmupTime = 0.2f;
    public float baselineCalibTime = 0.6f;
    public float baselineMargin = 1.6f;

    [Header("NEW: Displacement Gates")]
    [Tooltip("本次下揮累積的向下位移需達到此值才允許觸發（單位：公尺）")]
    public float minDownDisplacement = 0.06f;

    [Tooltip("預備抬手（向上）累積位移達到此值，才算完成 primed（單位：公尺）")]
    public float minUpPrimeDisplacement = 0.05f;

    [Tooltip("多久沒再抬手就清掉 primed（秒）")]
    public float upPrimeTimeout = 1.2f;

    [Header("Events")]
    public UnityEvent<float> OnDownSwing = new UnityEvent<float>(); // strength 0~1

    // internal
    private Vector3 _prevPos;
    private Vector3 _vel;
    private float _lastFireTime = -999f;
    private float _startTime;

    // displacement accumulators
    private float _downDisp;       // 累積向下位移（本次下揮）
    private float _upDisp;         // 累積向上位移（預備抬手）
    private float _lastUpTime;     // 最近一次有明顯向上位移的時間
    private bool _primed;          // 是否已完成「先抬」預備

    public Vector3 Velocity => _vel;

    void Awake()
    {
        if (hand == null) hand = transform;
        _prevPos = hand.position;
        _startTime = Time.time;

        // 沒填 referenceDown 時，預設抓主相機（避免忘記拖）
        if (referenceDown == null && Camera.main != null)
            referenceDown = Camera.main.transform;
    }

    private Vector3 GetDownAxis()
    {
        // 玩家視角的「下」：-camera.up
        if (referenceDown != null) return (-referenceDown.up).normalized;
        return Vector3.down;
    }

    void Update()
    {
        // ⛔ 比賽尚未開始（倒數中），完全不判定
        if (!Countdown.gameStarted)
        return;

        float now = Time.time;
        Vector3 downAxis = GetDownAxis();

        // --- velocity (world) ---
        Vector3 pos = hand.position;
        Vector3 rawVel = (pos - _prevPos) / Mathf.Max(Time.deltaTime, 1e-5f);
        _prevPos = pos;

        // smoothing：t 越小越跟手（越不延遲）
        float t = Mathf.Clamp01(velocitySmoothing);
        _vel = Vector3.Lerp(rawVel, _vel, t);

        // 早期暖機不判定（避免啟動抖動）
        if (now - _startTime < warmupTime)
            return;

        // --- speeds relative to downAxis ---
        float downSpeed = Mathf.Max(0f, Vector3.Dot(_vel, downAxis));     // 往「玩家視角下」的速度
        float upSpeed   = Mathf.Max(0f, Vector3.Dot(_vel, -downAxis));    // 往「玩家視角上」的速度

        // lateral = 去掉 downAxis 分量後的速度大小
        Vector3 lateral = _vel - downAxis * Vector3.Dot(_vel, downAxis);
        float horizSpd = lateral.magnitude;

        float totalSpd = _vel.magnitude;

        // --- displacement accumulation (also relative to downAxis) ---
        Vector3 delta = rawVel * Time.deltaTime; // 用 rawVel 比較敏感
        float downDispStep = Mathf.Max(0f, Vector3.Dot(delta, downAxis));
        float upDispStep   = Mathf.Max(0f, Vector3.Dot(delta, -downAxis));

        // reset downDisp：當動作停止/反向時，讓下一次更容易重新累積
        if (downSpeed <= resetDownSpeed && downDispStep <= 1e-4f)
        {
            _downDisp = Mathf.Max(0f, _downDisp - 0.5f * Time.deltaTime);
        }

        _downDisp += downDispStep;

        if (upDispStep > 1e-4f)
        {
            _upDisp += upDispStep;
            _lastUpTime = now;

            if (_upDisp >= minUpPrimeDisplacement)
                _primed = true;
        }

        // 若一直沒有再上抬，過了時間窗則清掉上揮預備
        if (_upDisp > 0f && (now - _lastUpTime) > upPrimeTimeout)
        {
            _upDisp = 0f;
            _primed = false;
        }

        // cooldown
        if (now - _lastFireTime < cooldown)
            return;

        // angle gate（相對於 downAxis，不再用 Vector3.down）
        bool anglePass = false;
        if (totalSpd > 1e-3f)
        {
            float cosA = Vector3.Dot(_vel / totalSpd, downAxis);
            float cosMax = Mathf.Cos(maxAngleFromDown * Mathf.Deg2Rad);
            anglePass = (cosA >= cosMax);
        }

        // dominance gate：down 相對 lateral
        bool dominancePass = (downSpeed >= verticalDominance * Mathf.Max(1e-3f, horizSpd));

        // baseline margin（可保留你原本風格：讓門檻更穩）
        float effectiveMinDown = minDownSpeed; // 若你有 baseline 校正可在此加成
        float effectiveMinTotal = minTotalSpeed;

        // final gates
        bool speedPass = (downSpeed >= effectiveMinDown) && (totalSpd >= effectiveMinTotal);
        bool dispPass = (_downDisp >= minDownDisplacement);
        bool primedPass = _primed;

        if (speedPass && dominancePass && anglePass && dispPass && primedPass)
        {
            float strength = Mathf.InverseLerp(strengthSpeedMin, strengthSpeedMax, downSpeed);
            strength = Mathf.Clamp01(strength);

            OnDownSwing.Invoke(strength);

            _lastFireTime = now;
            _downDisp = 0f;

            // 觸發後，重置 primed（需要再抬一次才能再出）
            _upDisp = 0f;
            _primed = false;
        }

        // Debug（需要的話打開）
        // if (Time.frameCount % 10 == 0)
        //     Debug.Log($"downSpeed={downSpeed:F2}, totalSpd={totalSpd:F2}, downDisp={_downDisp:F3}, upDisp={_upDisp:F3}, primed={_primed}");
    }
}
