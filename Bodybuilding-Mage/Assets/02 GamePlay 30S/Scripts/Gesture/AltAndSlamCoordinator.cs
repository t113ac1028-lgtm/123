using UnityEngine;
using UnityEngine.Events;

public class AltAndSlamCoordinator : MonoBehaviour
{
    [Header("Inputs")]
    public DownSwingDetector leftDetector;
    public DownSwingDetector rightDetector;

    [Header("Timer / Phase Gate")]
    [Tooltip("拖進你的 GameTimer（有 TimeLeft 的那個）")]
    public GameTimer timer;

    [Tooltip("當 TimeLeft <= 這個值（例如 15）就進入 Slam Phase；TimeLeft > 15 則是 Slash Phase")]
    public float slamPhaseThreshold = 15f;

    [Header("Slam Window & Strength")]
    [Tooltip("兩手下揮合併為重摔的時間視窗（秒）")]
    public float slamWindow = 0.12f;

    [Tooltip("兩手向下速度皆需達到此門檻（m/s）")]
    public float slamMinDownSpeed = 3.4f;

    [Header("Slam Robustness (Angle & Dominance)")]
    [Tooltip("兩手速度方向距離世界向下的最大夾角（度）")]
    [Range(5f, 89f)] public float slamMaxAngleFromDown = 30f;

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
    public UnityEvent<float> OnHeavySlam = new UnityEvent<float>();                      // 0~1

    // 延遲決策暫存
    private bool pendingL, pendingR;
    private float pendingTimeL, pendingTimeR;
    private float pendingStrengthL, pendingStrengthR;
    private Vector3 snapshotVelL, snapshotVelR;

    // 單手釋放時間（用來做排他窗）
    private float lastSoloLeftTime = -999f;
    private float lastSoloRightTime = -999f;

    // 全域 Slam 冷卻
    private float lastSlamTime = -999f;

    private float startTime;

    // 用來偵測 phase 切換（15 秒那刻）
    private bool lastIsSlamPhase;

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
        float now = Time.time;

        bool isSlamPhase = GetIsSlamPhase();
        if (isSlamPhase != lastIsSlamPhase)
        {
            // phase 切換那一刻：清空 pending，避免前半掛起的 Slash/後半掛起的 Slam 混到
            pendingL = pendingR = false;
            lastIsSlamPhase = isSlamPhase;
        }

        // 前半：只 Slash（逾時就放出 Slash）
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
            // 後半：只 Slam（逾時就丟掉，不出 Slash）
            if (pendingL && now - pendingTimeL > slamWindow) pendingL = false;
            if (pendingR && now - pendingTimeR > slamWindow) pendingR = false;
        }
    }

    bool GetIsSlamPhase()
    {
        // 以 Timer 的 TimeLeft 為準（你 GameTimer 有提供）:contentReference[oaicite:1]{index=1}
        if (timer != null)
            return timer.TimeLeft <= slamPhaseThreshold;

        // 沒掛 timer 時的保險：用經過時間粗估（假設 30 秒一局）
        float elapsed = Time.time - startTime;
        return elapsed >= (30f - slamPhaseThreshold);
    }

    void OnLeftSwing(float strength)
    {
        float now = Time.time;
        if (now - startTime < warmupTime) return;

        bool isSlamPhase = GetIsSlamPhase();

        // 前半：只 Slash（不等待合成 Slam）
        if (!isSlamPhase)
        {
            // 左手如果已 pending，代表高手狂揮：先把上一筆 Slash 放掉，避免被 slamWindow 一直延後
            if (pendingL)
            {
                OnAlternateSwing.Invoke("Left", pendingStrengthL);
                lastSoloLeftTime = now;
                pendingL = false;
            }

            // 掛起本次（讓它也能吃到 slamWindow 的「稍微延遲合成」感，但不會拖死）
            pendingL = true;
            pendingTimeL = now;
            pendingStrengthL = strength;
            snapshotVelL = leftDetector != null ? leftDetector.Velocity : Vector3.zero;
            return;
        }

        // 後半：只 Slam（嘗試與右手合成）
        if (pendingR && now - pendingTimeR <= slamWindow)
        {
            snapshotVelL = leftDetector != null ? leftDetector.Velocity : Vector3.zero;
            if (CanSlam(snapshotVelL, snapshotVelR, now))
            {
                FireSlam(strength, pendingStrengthR);
                return;
            }
        }

        // 後半單手就先掛起（等另一手）
        pendingL = true;
        pendingTimeL = now;
        pendingStrengthL = strength;
        snapshotVelL = leftDetector != null ? leftDetector.Velocity : Vector3.zero;
    }

    void OnRightSwing(float strength)
    {
        float now = Time.time;
        if (now - startTime < warmupTime) return;

        bool isSlamPhase = GetIsSlamPhase();

        // 前半：只 Slash
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

        // 後半：只 Slam
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
