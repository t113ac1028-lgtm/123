using UnityEngine;

public class EffectSpawner : MonoBehaviour
{
    [Header("Refs")]
    public Transform leftHand;
    public Transform rightHand;
    public Transform bossTarget;
    public Camera cam;

    [Header("Slash / Slam Phase Lock")]
    [Tooltip("把 GameTimer 拖進來，用來判斷現在是 Slash 段還是 Slam 段")]
    public GameTimer timer;
    [Tooltip("剩餘秒數 <= 這個值時，進入 Slam 段（例如 15 秒）")]
    public float slamPhaseThreshold = 15f;

    [Header("Muzzles (可選，優先使用)")]
    public Transform leftMuzzle;
    public Transform rightMuzzle;
    [Tooltip("Slam 用（可選）：若不指定，就用左右手的中點")]
    public Transform slamMuzzle;

    [Header("Spawn Offsets（當沒有 Muzzle 時才用）")]
    public Vector3 spawnOffsetLeft  = new Vector3(-0.05f, -0.02f, 0.25f);
    public Vector3 spawnOffsetRight = new Vector3( 0.05f, -0.02f, 0.25f);
    public Vector3 spawnOffsetSlam  = new Vector3( 0.00f, -0.02f, 0.30f); // 中點稍前

    [Header("Prefabs")]
    public GameObject slashProjectilePrefab;
    public GameObject slamProjectilePrefab;   // ← 新增：雙手特效
    public GameObject hitEffectPrefab;

    [Header("Slash Flight")]
    public float slashSpeed    = 12f;
    public float slashMaxLife  = 2.0f;
    public float slashUpBias   = 0.10f;       // 微拋

    [Header("Slam Flight")]
    public float slamSpeed     = 14f;         // slam 可以稍快
    public float slamMaxLife   = 2.0f;
    public float slamUpBias    = 0.06f;       // 拋得更少、更直感
    public Vector2 slamScaleRange = new Vector2(1.1f, 1.6f); // slam 大一點
    public Vector2 slashScaleRange = new Vector2(0.85f, 1.05f);

    [Header("Scoring")]
    public DamageCalculator damage;
    public ComboCounter combo;

        bool InSlamPhase()
    {
        if (timer == null) return false;   // 沒有 Timer 就當作「一直是 Slash 段」
        return timer.TimeLeft <= slamPhaseThreshold;
    }

        // Alt Swing 用：依時間段決定要生 Slash 還是 Slam 特效
        // Alt Swing 用：只有在 Slash 段才會生 Slash 特效

    // --- 這段是新增的測試代碼 ---
    void Update()
    {
        // 按下【空白鍵】模擬「右手揮擊」
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // 呼叫原本的發射函式 (模擬右手, 力道100%)
            SpawnSlashProjectile("Right", 1.0f);
            
            // 如果你想測那種會自動變 Slam 的邏輯，也可以改用這行：
            // SpawnSwingByPhase("Right", 1.0f);
        }

        // 按下【左邊 Ctrl】模擬「雙手重擊 (Slam)」
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            SpawnSlamProjectile(1.0f);
        }
    }
    // -------------------------
    public void SpawnSwingByPhase(string who, float strength)
    {
        // 後半段（Slam Phase）就不生任何特效
        if (InSlamPhase())
            return;

        // 前半段：正常 Slash 特效
        SpawnSlashProjectile(who, strength);
    }

    // Heavy Slam 用：只有在 Slam 段才會生 Slam 特效
    public void SpawnSlamByPhase(float strength)
    {
        // 前半段（Slash Phase）直接忽略這個 Slam
        if (!InSlamPhase())
            return;

        // 後半段：正常 Slam 特效
        SpawnSlamProjectile(strength);
    }


    // ===== Slash（單手） =====
    public void SpawnSlashProjectile(string who, float strength)
    {
        if (!slashProjectilePrefab || !bossTarget) return;

        bool isLeft = (who == "Left");
        Transform hand   = isLeft ? leftHand   : rightHand;
        Transform muzzle = isLeft ? leftMuzzle : rightMuzzle;
        if (!hand) return;

        Vector3 spawnPos = muzzle ? muzzle.position
                                  : hand.TransformPoint(isLeft ? spawnOffsetLeft : spawnOffsetRight);

        Quaternion rot = slashProjectilePrefab.transform.rotation;

        GameObject go = Instantiate(slashProjectilePrefab, spawnPos, rot);
        float k = Mathf.Clamp01(strength);
        go.transform.localScale = Vector3.one * Mathf.Lerp(slashScaleRange.x, slashScaleRange.y, k);

        var homing = go.GetComponent<ProjectileHoming>();
        if (homing)
        {
            homing.Launch(
                tgt:    bossTarget,
                spd:    slashSpeed,
                life:   slashMaxLife,
                upBias: slashUpBias,
                hitFx:  hitEffectPrefab,
                dmg:    damage,
                cmb:    combo,
                slam:   false,       // 單手
                str01:  k
            );
        }
    }

    // ===== Slam（雙手） =====
    public void SpawnSlamProjectile(float strength)
    {
        if (!slamProjectilePrefab || !bossTarget) return;

        // 出生點：優先用 slamMuzzle；否則用左右手的中點 + 偏移
        Vector3 spawnPos;
        if (slamMuzzle)
        {
            spawnPos = slamMuzzle.position;
        }
        else
        {
            if (!leftHand || !rightHand) return;
            Vector3 mid = 0.5f * (leftHand.position + rightHand.position);
            // 用「左手」當參考做 TransformPoint 比較直覺：往玩家前方一點
            spawnPos = leftHand ? leftHand.TransformPoint(leftHand.InverseTransformPoint(mid) + spawnOffsetSlam)
                                : mid + spawnOffsetSlam;
        }

        Quaternion rot = slamProjectilePrefab.transform.rotation;

        GameObject go = Instantiate(slamProjectilePrefab, spawnPos, rot);
        float k = Mathf.Clamp01(strength);
        go.transform.localScale = Vector3.one * Mathf.Lerp(slamScaleRange.x, slamScaleRange.y, k);

        var homing = go.GetComponent<ProjectileHoming>();
        if (homing)
        {
            homing.Launch(
                tgt:    bossTarget,
                spd:    slamSpeed,
                life:   slamMaxLife,
                upBias: slamUpBias,
                hitFx:  hitEffectPrefab,
                dmg:    damage,
                cmb:    combo,
                slam:   true,        // ← 告知是 slam
                str01:  k
            );
        }
    }
}
