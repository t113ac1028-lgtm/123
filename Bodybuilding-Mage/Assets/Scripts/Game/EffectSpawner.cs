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

    [Header("Spawn Offsets（當沒有 Muzzle 時才用）")]
    public Vector3 spawnOffsetLeft  = new Vector3(-0.05f, -0.02f, 0.25f);
    public Vector3 spawnOffsetRight = new Vector3( 0.05f, -0.02f, 0.25f);

    [Header("Prefabs")]
    public GameObject slashProjectilePrefab;
    public GameObject hitEffectPrefab;

    [Header("Flight")]
    public float projectileSpeed   = 12f;
    public float projectileMaxLife = 2.0f;
    public float upwardBias        = 0.10f;

    [Header("Scoring")]
    public DamageCalculator damage;
    public ComboCounter combo;

    public void SpawnSlashProjectile(string who, float strength)
    {
        if (!slashProjectilePrefab || !bossTarget) return;

        bool isLeft = (who == "Left");
        Transform hand   = isLeft ? leftHand   : rightHand;
        Transform muzzle = isLeft ? leftMuzzle : rightMuzzle;
        if (!hand) return;

        // 1) 出生位置：優先用錨點，其次用各自的本地偏移
        Vector3 spawnPos = muzzle
            ? muzzle.position
            : hand.TransformPoint(isLeft ? spawnOffsetLeft : spawnOffsetRight);

        // 2) 旋轉：完全使用 prefab 自己的角度（你已手動調好垂直）
        Quaternion rot = slashProjectilePrefab.transform.rotation;

        // 3) 生成 & 大小
        GameObject go = Instantiate(slashProjectilePrefab, spawnPos, rot);
        float k = Mathf.Clamp01(strength);
        go.transform.localScale = Vector3.one * Mathf.Lerp(0.85f, 1.05f, k);

        // 4) 啟動追蹤（只改位置，不改旋轉）
        var homing = go.GetComponent<ProjectileHoming>();
        if (homing)
        {
            homing.Launch(
                tgt:   bossTarget,
                spd:   projectileSpeed,
                life:  projectileMaxLife,
                upBias:upwardBias,
                hitFx: hitEffectPrefab,
                dmg:   damage,
                cmb:   combo,
                slam:  false,
                str01: k
            );
        }
    }
}
