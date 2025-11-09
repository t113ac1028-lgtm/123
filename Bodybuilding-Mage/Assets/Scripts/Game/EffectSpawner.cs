using UnityEngine;

public class EffectSpawner : MonoBehaviour
{
    [Header("Refs")]
    public Transform leftHand;
    public Transform rightHand;
    public Transform bossTarget;
    public Camera cam;

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
