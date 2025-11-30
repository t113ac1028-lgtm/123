using UnityEngine;
using UnityEngine.Events;

public class AltAndSlamCoordinator : MonoBehaviour
{
    [Header("Inputs")]
    public DownSwingDetector leftDetector;
    public DownSwingDetector rightDetector;

    [Header("Slam Window & Strength")]
    [Tooltip("兩手下揮合併為重摔的時間視窗（秒）")]
    public float slamWindow = 0.12f;
    [Tooltip("兩手向下速度皆需達到此門檻（m/s）")]
    public float slamMinDownSpeed = 3.4f;

    [Header("Slam Robustness (Angle & Dominance)")]
    [Tooltip("兩手速度方向距離世界向下的最大夾角（度）")]
    [Range(5f,89f)] public float slamMaxAngleFromDown = 30f;
    [Tooltip("向下速度必須是水平速度的幾倍以上")]
    public float slamVerticalDominance = 2.0f;

    [Header("Exclusions / Cooldowns")]
    [Tooltip("釋放單手後，在此時間內不允許與另一手合成 Slam（秒）")]
    public float slamExclusionAfterSolo = 0.14f;
    [Tooltip("Slam 觸發後的全域冷卻（秒）")]
    public float slamGlobalCooldown = 0.20f;
    [Tooltip("進入 Play 初期忽略判定（秒）")]
    public float warmupTime = 0.4f;

    [Header("Events")]
    public UnityEvent<string, float> OnAlternateSwing = new UnityEvent<string, float>(); // "Left"/"Right", strength
    public UnityEvent<float> OnHeavySlam = new UnityEvent<float>(); // 0~1

    // 延遲決策暫存
    private bool   pendingL, pendingR;
    private float  pendingTimeL, pendingTimeR;
    private float  pendingStrengthL, pendingStrengthR;
    private Vector3 snapshotVelL, snapshotVelR;

    // 單手釋放時間（用來做排他窗）
    private float lastSoloLeftTime  = -999f;
    private float lastSoloRightTime = -999f;

    // 全域 Slam 冷卻
    private float lastSlamTime = -999f;

    private float startTime;

    void Awake() => startTime = Time.time;

    void OnEnable()
    {
        if (leftDetector  != null) leftDetector.OnDownSwing.AddListener(OnLeftSwing);
        if (rightDetector != null) rightDetector.OnDownSwing.AddListener(OnRightSwing);
    }

    void OnDisable()
    {
        if (leftDetector  != null) leftDetector.OnDownSwing.RemoveListener(OnLeftSwing);
        if (rightDetector != null) rightDetector.OnDownSwing.RemoveListener(OnRightSwing);
    }

    void Update()
    {
        float now = Time.time;

        // 左手掛起逾時 → 放單手（並記錄單手釋放時間）
        if (pendingL && now - pendingTimeL > slamWindow)
        {
            OnAlternateSwing.Invoke("Left", pendingStrengthL);
            lastSoloLeftTime = now;
            pendingL = false;
        }

        // 右手掛起逾時 → 放單手
        if (pendingR && now - pendingTimeR > slamWindow)
        {
            OnAlternateSwing.Invoke("Right", pendingStrengthR);
            lastSoloRightTime = now;
            pendingR = false;
        }
    }

    void OnLeftSwing(float strength)
    {
        float now = Time.time;
        if (now - startTime < warmupTime) { return; } // 暖機：初期不判

        // 若右手已掛起且在窗內，嘗試判 Slam
        if (pendingR && now - pendingTimeR <= slamWindow)
        {
            snapshotVelL = leftDetector != null ? leftDetector.Velocity : Vector3.zero;
            if (CanSlam(snapshotVelL, snapshotVelR, now)) { FireSlam(strength, pendingStrengthR); return; }
        }

        // 否則掛起左手，等窗內另一手
        pendingL = true;
        pendingTimeL = now;
        pendingStrengthL = strength;
        snapshotVelL = leftDetector != null ? leftDetector.Velocity : Vector3.zero;
    }

    void OnRightSwing(float strength)
    {
        float now = Time.time;
        if (now - startTime < warmupTime) { return; }

        if (pendingL && now - pendingTimeL <= slamWindow)
        {
            snapshotVelR = rightDetector != null ? rightDetector.Velocity : Vector3.zero;
            if (CanSlam(snapshotVelR, snapshotVelL, now)) { FireSlam(strength, pendingStrengthL); return; }
        }

        pendingR = true;
        pendingTimeR = now;
        pendingStrengthR = strength;
        snapshotVelR = rightDetector != null ? rightDetector.Velocity : Vector3.zero;
    }

    bool CanSlam(Vector3 velNew, Vector3 velOld, float now)
    {
        // 全域 Slam 冷卻
        if (now - lastSlamTime < slamGlobalCooldown) return false;

        // 單手剛釋放的排他窗：避免交替時重疊被合成
        if (now - lastSoloLeftTime  < slamExclusionAfterSolo) return false;
        if (now - lastSoloRightTime < slamExclusionAfterSolo) return false;

        // 兩手皆需「向下且同向下」（垂直分量為負）
        float downNew = Mathf.Max(0f, -velNew.y);
        float downOld = Mathf.Max(0f, -velOld.y);
        if (downNew < slamMinDownSpeed || downOld < slamMinDownSpeed) return false;

        // 角度條件：接近世界 Vector3.down
        if (!AnglePass(velNew) || !AnglePass(velOld)) return false;

        // 垂直優勢：向下速度遠大於水平
        if (!DominancePass(velNew) || !DominancePass(velOld)) return false;

        return true;
    }

    bool AnglePass(Vector3 v)
    {
        float mag = v.magnitude;
        if (mag < 1e-3f) return false;
        float cosA   = Vector3.Dot(v / mag, Vector3.down);
        float cosMax = Mathf.Cos(slamMaxAngleFromDown * Mathf.Deg2Rad);
        return cosA >= cosMax;
    }

    bool DominancePass(Vector3 v)
    {
        float down  = Mathf.Max(0f, -v.y);
        float horiz = new Vector2(v.x, v.z).magnitude;
        return down / Mathf.Max(1e-3f, horiz) >= slamVerticalDominance;
    }

    void FireSlam(float strengthNew, float strengthOld)
    {
        // 平均或取較大皆可；這裡用平均
        float s = Mathf.Clamp01((strengthNew + strengthOld) * 0.5f);
        OnHeavySlam.Invoke(s);

        // 清掉 pending，記錄冷卻
        pendingL = pendingR = false;
        lastSlamTime = Time.time;
    }
}
