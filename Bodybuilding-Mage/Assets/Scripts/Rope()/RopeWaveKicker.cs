using UnityEngine;

/// 讀手的速度，在特定條件下注入一個「脈衝」到 HandDriver，
/// RopeVerlet 的 StartPoint 指到 HandDriver，就能把波傳進繩子。
public class RopeWaveKicker : MonoBehaviour
{
    [Header("Refs")]
    public Transform hand;             // 真正的手（RightHand 物件）
    public HandDriverOffset driver;    // 上面那個 HandDriverOffset
    public RopePatternDetector detector; // 可選：用你的姿勢判斷

    [Header("Kick thresholds")]
    public float kickCooldown = 0.10f;   // 去抖
    public float downSpeedThreshold = -1.2f; // 往下速度門檻（m/s）

    [Header("Kick strengths")]
    public float kickY = 0.25f;          // 垂直波強度（Alternating / 一般）
    public float kickY_Slam = 0.50f;     // 重摔時更強
    public float kickX = 0.25f;          // 水平波強度（Horizontal）

    [Header("水平甩的判準（避免誤觸）")]
    public float horizDominance = 0.8f;  // |Vx| 要比 |Vy|、|Vz| 大這比例才算「主要是水平」

    Vector3 _lastHandPos;
    float _lastKick;

    void Start()
    {
        _lastHandPos = hand ? hand.position : Vector3.zero;
    }

    void Update()
    {
        if (!hand || !driver) return;

        float dt = Mathf.Max(Time.deltaTime, 1e-4f);
        Vector3 v = (hand.position - _lastHandPos) / dt;
        _lastHandPos = hand.position;

        if (Time.time - _lastKick < kickCooldown) return;

        // 預設往下甩產生垂直波；若偵測到水平甩，改踢水平方向
        Vector3 dir = Vector3.zero;
        float str = kickY;

        var mode = detector ? detector.Current : RopePatternDetector.Pattern.Idle;

        if (mode == RopePatternDetector.Pattern.HorizontalWave)
        {
            // 只有當水平速度「明顯佔優」才觸發，避免把垂直甩誤判進來
            if (Mathf.Abs(v.x) > Mathf.Abs(v.y) * horizDominance &&
                Mathf.Abs(v.x) > Mathf.Abs(v.z) * horizDominance)
            {
                dir = new Vector3(Mathf.Sign(v.x), 0f, 0f); // 往左右「同向」踢
                str = kickX * Mathf.Abs(v.x) * 0.02f;       // 速度越快，波越大
            }
        }
        else
        {
            // 垂直：手往下穿越時（v.y 為負且超過門檻）踢一下
            if (v.y < downSpeedThreshold)
            {
                dir = Vector3.down;
                str = (mode == RopePatternDetector.Pattern.VerticalSlam) ? kickY_Slam : kickY;
                str *= Mathf.Clamp01(Mathf.Abs(v.y) * 0.02f); // 稍微跟速度掛勾
            }
        }

        if (dir != Vector3.zero && str > 0f)
        {
            driver.Kick(dir * str);
            _lastKick = Time.time;
        }
    }
}
