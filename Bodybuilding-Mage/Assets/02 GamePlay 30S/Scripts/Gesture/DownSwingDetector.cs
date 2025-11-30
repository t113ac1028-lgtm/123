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

    [Header("Speed Gates (already stable)")]
    public float minDownSpeed = 3.2f;
    public float minTotalSpeed = 2.0f;
    public float verticalDominance = 1.8f;
    [Range(0f,89f)] public float maxAngleFromDown = 35f;
    public float resetDownSpeed = 1.6f;
    public float cooldown = 0.30f;
    [Range(0f,1f)] public float velocitySmoothing = 0.30f;

    [Header("Warmup / Baseline")]
    public float warmupTime = 0.6f;
    public float baselineCalibTime = 0.6f;
    public float baselineMargin = 1.8f;

    [Header("NEW: Displacement Gates")]
    [Tooltip("一次下揮至少要往下移動的距離（公尺）")]
    public float minDownDisplacement = 0.12f;         // 12 公分
    [Tooltip("重新上膛前，必須先往上移動的距離（公尺）")]
    public float minUpPrimeDisplacement = 0.10f;      // 10 公分
    [Tooltip("往上預備動作的有效時間窗（秒），超過就重置計算")]
    public float upPrimeTimeout = 0.8f;

    [Header("Events")]
    public UnityEvent<float> OnDownSwing = new UnityEvent<float>(); // strength 0~1

    // internal
    private Vector3 _prevPos;
    private Vector3 _vel;
    private float _lastFireTime = -999f;
    private float _startTime;
    private bool _armed = true;

    // baseline
    private float _sumSqDown;
    private int   _samples;
    private float _dynamicDownFloor;

    // displacement accumulators
    private float _downDisp;       // 累積向下位移（本次下揮）
    private float _upDisp;         // 累積向上位移（預備抬手）
    private float _lastUpTime;     // 最近一次有明顯向上位移的時間
    private bool  _primed;         // 是否已完成「先抬」預備

    public Vector3 Velocity => _vel;

    void Awake()
    {
        if (hand == null) hand = transform;
        _prevPos = hand.position;
        _startTime = Time.time;
        _lastUpTime = -999f;
    }

    void Update()
    {
        

        float dt = Mathf.Max(Time.deltaTime, 1e-5f);

        // 估速度
        Vector3 rawVel = (hand.position - _prevPos) / dt;
        _prevPos = hand.position;
        _vel = Vector3.Lerp(rawVel, _vel, velocitySmoothing);

        float downSpeed = Mathf.Max(0f, -_vel.y);
        float upSpeed   = Mathf.Max(0f,  _vel.y);
        float horizSpd  = new Vector2(_vel.x, _vel.z).magnitude;
        float totalSpd  = _vel.magnitude;

        // 暖機 / 基線估計
        float t = Time.time - _startTime;
        if (t <= baselineCalibTime)
        {
            _sumSqDown += downSpeed * downSpeed;
            _samples++;
        }
        if (_samples > 0) _dynamicDownFloor = Mathf.Sqrt(_sumSqDown / _samples) * baselineMargin;
        if (t < warmupTime) return;

        // 角度條件（接近世界向下）
        bool anglePass = true;
        if (totalSpd > 1e-3f)
        {
            float cosA = Vector3.Dot(_vel.normalized, Vector3.down);
            float cosMax = Mathf.Cos(maxAngleFromDown * Mathf.Deg2Rad);
            anglePass = (cosA >= cosMax);
        }

        // —— 位移累積（用位置積分，對抗手腕小抖）——
        // 只在該方向速度大於小雜訊門檻時才累計，避免微抖積分
        float tiny = 0.2f; // 20cm/s 以下視為雜訊，不計入位移
        if (upSpeed > tiny)
        {
            _upDisp   += upSpeed * dt;
            _lastUpTime = Time.time;
        }
        if (downSpeed > tiny)
        {
            _downDisp += downSpeed * dt;
        }

        // 上揮預備（Prime）：在時間窗內上移超過門檻才算完成預備
        if (!_primed)
        {
            bool upWindowValid = (Time.time - _lastUpTime) <= upPrimeTimeout;
            if (upWindowValid && _upDisp >= minUpPrimeDisplacement)
                _primed = true;
            else if (!upWindowValid)
                _upDisp = 0f; // 超時就重算上揮預備
        }

        // 觸發條件
        bool cooled       = (Time.time - _lastFireTime) >= cooldown;
        bool strongDown   = downSpeed >= Mathf.Max(minDownSpeed, _dynamicDownFloor);
        bool notJitter    = totalSpd  >= minTotalSpeed;
        bool verticalLead = (horizSpd <= 1e-3f) ? true : (downSpeed / Mathf.Max(1e-3f, horizSpd) >= verticalDominance);
        bool enoughTravel = _downDisp >= minDownDisplacement; // ★ 新增：必須真的往下走過一段距離
        bool primedReady  = _primed;                          // ★ 新增：必須先有上揮預備

        if (_armed && cooled && anglePass && strongDown && notJitter && verticalLead && enoughTravel && primedReady)
        {
            // 決定 0 與 1 對應的速度範圍
float sMin = (strengthSpeedMin <= 0f) ? minDownSpeed : strengthSpeedMin;
float sMax = (strengthSpeedMax <= sMin + 0.1f) ? sMin + 0.1f : strengthSpeedMax;

// 把實際向下速度映射成 0~1 的強度
float strength = Mathf.InverseLerp(sMin, sMax, downSpeed);
strength = Mathf.Clamp01(strength);

OnDownSwing.Invoke(strength);

            _lastFireTime = Time.time;
            _armed  = false;
            _primed = false;

            // 發射後重置此次位移累計
            _downDisp = 0f;
            _upDisp   = 0f;
        }

        // 重新上膛條件：速度回落，且水平不大；同時重置「下揮位移」
        if (!_armed && downSpeed <= resetDownSpeed && horizSpd <= resetDownSpeed)
        {
            _armed = true;
            _downDisp = 0f;
            // 不重置 _upDisp，讓使用者可以接著往上抬來達成下一次「Prime」
        }

        // 若一直沒有再上抬，過了時間窗則清掉上揮預備的位移
        if (_upDisp > 0f && (Time.time - _lastUpTime) > upPrimeTimeout)
        {
            _upDisp = 0f;
            _primed = false;
        }
        if (Time.frameCount % 10 == 0)
    {
        Debug.Log($"downSpeed={downSpeed:F2}, totalSpd={totalSpd:F2}, " +
                  $"downDisp={_downDisp:F3}, upDisp={_upDisp:F3}");
    }
        
    }
}
