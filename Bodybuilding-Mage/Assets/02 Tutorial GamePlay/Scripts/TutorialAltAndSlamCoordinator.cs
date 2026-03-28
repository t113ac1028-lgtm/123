using UnityEngine;
using UnityEngine.Events;

public class TutorialAltAndSlamCoordinator : MonoBehaviour
{
    [Header("Inputs")]
    public DownSwingDetector leftDetector;
    public DownSwingDetector rightDetector;

    [Header("Timer / Phase Gate")]
    public GameTimer timer;
    public float slamPhaseThreshold = 16.5f;

    [Header("Phase Transition (NEW)")]
    [Tooltip("進入 Slam 階段後，強制鎖定幾秒(真實時間)不准觸發重擊，讓玩家有時間欣賞慢動作")]
    public float slamPhaseEntryDelay = 0.8f;

    [Header("Slam Window & Strength")]
    public float slamWindow = 0.12f;
    public float slamMinDownSpeed = 3.4f;

    [Header("Slam Robustness (Angle & Dominance)")]
    [Range(5f, 89f)] public float slamMaxAngleFromDown = 30f;
    public float slamVerticalDominance = 2.0f;

    [Header("Exclusions / Cooldowns")]
    public float slamExclusionAfterSolo = 0.14f;
    public float slamGlobalCooldown = 0.20f;
    public float warmupTime = 0.4f;

    [Header("Events")]
    public UnityEvent<string, float> OnAlternateSwing = new UnityEvent<string, float>();
    public UnityEvent<float> OnHeavySlam = new UnityEvent<float>();

    public bool isSlamPhase = false;

    private bool pendingL, pendingR;
    private float pendingTimeL, pendingTimeR;
    private float pendingStrengthL, pendingStrengthR;
    private Vector3 snapshotVelL, snapshotVelR;

    private float lastSoloLeftTime = -999f;
    private float lastSoloRightTime = -999f;
    private float lastSlamTime = -999f;
    private float startTime;
    private bool lastIsSlamPhase;

    // ★ 新增：紀錄剛進入慢動作的「真實時間」
    private float slamPhaseStartRealTime = -999f;

    void Awake() => startTime = Time.time;

    void OnEnable()
    {
        if (leftDetector != null) leftDetector.OnDownSwing.AddListener(OnLeftSwing);
        if (rightDetector != null) rightDetector.OnDownSwing.AddListener(OnRightSwing);
    }

    void OnDisable()
    {
        if (leftDetector != null) leftDetector.OnDownSwing.RemoveListener(OnLeftSwing);
        if (rightDetector != null) rightDetector.OnDownSwing.RemoveListener(OnRightSwing);
    }

    void Update()
    {
        if (TransitionGuard.IsSwitchingScene) return;

        float now = Time.time;
        bool isSlamPhase = GetIsSlamPhase();
        
        if (isSlamPhase != lastIsSlamPhase)
        {
            pendingL = pendingR = false;
            
            // ★ 剛切換到 Slam 階段時，記錄真實時間
            if (isSlamPhase)
            {
                slamPhaseStartRealTime = Time.unscaledTime;
            }
            
            lastIsSlamPhase = isSlamPhase;
        }

        if (!isSlamPhase)
        {
            if (pendingL && now - pendingTimeL > slamWindow)
            {
                OnAlternateSwing.Invoke("Left", pendingStrengthL);
                lastSoloLeftTime = now;
                pendingL = false;
            }
            if (pendingR && now - pendingTimeR > slamWindow)
            {
                OnAlternateSwing.Invoke("Right", pendingStrengthR);
                lastSoloRightTime = now;
                pendingR = false;
            }
        }
        else
        {
            if (pendingL && now - pendingTimeL > slamWindow) pendingL = false;
            if (pendingR && now - pendingTimeR > slamWindow) pendingR = false;
        }
    }

    bool GetIsSlamPhase()
{
    return isSlamPhase;
}

    void OnLeftSwing(float strength)
    {
        if (TransitionGuard.IsSwitchingScene) return;

        float now = Time.time;
        if (now - startTime < warmupTime) return;

        bool isSlamPhase = GetIsSlamPhase();

        if (!isSlamPhase)
        {
            if (pendingL)
            {
                OnAlternateSwing.Invoke("Left", pendingStrengthL);
                lastSoloLeftTime = now;
                pendingL = false;
            }
            pendingL = true;
            pendingTimeL = now;
            pendingStrengthL = strength;
            snapshotVelL = leftDetector != null ? leftDetector.Velocity : Vector3.zero;
            return;
        }

        if (pendingR && now - pendingTimeR <= slamWindow)
        {
            snapshotVelL = leftDetector != null ? leftDetector.Velocity : Vector3.zero;
            if (CanSlam(snapshotVelL, snapshotVelR, now))
            {
                FireSlam(strength, pendingStrengthR);
                return;
            }
        }

        pendingL = true;
        pendingTimeL = now;
        pendingStrengthL = strength;
        snapshotVelL = leftDetector != null ? leftDetector.Velocity : Vector3.zero;
    }

    void OnRightSwing(float strength)
    {
        if (TransitionGuard.IsSwitchingScene) return;
        
        float now = Time.time;
        if (now - startTime < warmupTime) return;

        bool isSlamPhase = GetIsSlamPhase();

        if (!isSlamPhase)
        {
            if (pendingR)
            {
                OnAlternateSwing.Invoke("Right", pendingStrengthR);
                lastSoloRightTime = now;
                pendingR = false;
            }
            pendingR = true;
            pendingTimeR = now;
            pendingStrengthR = strength;
            snapshotVelR = rightDetector != null ? rightDetector.Velocity : Vector3.zero;
            return;
        }

        if (pendingL && now - pendingTimeL <= slamWindow)
        {
            snapshotVelR = rightDetector != null ? rightDetector.Velocity : Vector3.zero;
            if (CanSlam(snapshotVelR, snapshotVelL, now))
            {
                FireSlam(strength, pendingStrengthL);
                return;
            }
        }

        pendingR = true;
        pendingTimeR = now;
        pendingStrengthR = strength;
        snapshotVelR = rightDetector != null ? rightDetector.Velocity : Vector3.zero;
    }

    bool CanSlam(Vector3 velNew, Vector3 velOld, float now)
    {
        // ★ 新增的強制鎖定檢查：使用真實時間，不被慢動作影響
        if (Time.unscaledTime - slamPhaseStartRealTime < slamPhaseEntryDelay) 
        {
            return false; 
        }

        if (now - lastSlamTime < slamGlobalCooldown) return false;
        if (now - lastSoloLeftTime < slamExclusionAfterSolo) return false;
        if (now - lastSoloRightTime < slamExclusionAfterSolo) return false;

        float downNew = Mathf.Max(0f, -velNew.y);
        float downOld = Mathf.Max(0f, -velOld.y);
        if (downNew < slamMinDownSpeed || downOld < slamMinDownSpeed) return false;

        if (!AnglePass(velNew) || !AnglePass(velOld)) return false;
        if (!DominancePass(velNew) || !DominancePass(velOld)) return false;

        return true;
    }

    bool AnglePass(Vector3 v)
    {
        float mag = v.magnitude;
        if (mag < 1e-3f) return false;
        float cosA = Vector3.Dot(v / mag, Vector3.down);
        float cosMax = Mathf.Cos(slamMaxAngleFromDown * Mathf.Deg2Rad);
        return cosA >= cosMax;
    }

    bool DominancePass(Vector3 v)
    {
        float down = Mathf.Max(0f, -v.y);
        float horiz = new Vector2(v.x, v.z).magnitude;
        return down / Mathf.Max(1e-3f, horiz) >= slamVerticalDominance;
    }

    void FireSlam(float strengthNew, float strengthOld)
    {
        float s = Mathf.Clamp01((strengthNew + strengthOld) * 0.5f);
        OnHeavySlam.Invoke(s);

        pendingL = pendingR = false;
        lastSlamTime = Time.time;
    }
}