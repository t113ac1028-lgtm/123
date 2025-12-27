using UnityEngine;

/// <summary>
/// 負責產生飛彈。
/// 包含：階段限制(30-15s / 15-0s)、顏色傳遞、以及結算攔截。
/// 修改：飛彈不再隨力量縮放，保持原大小。
/// </summary>
public class EffectSpawner : MonoBehaviour
{
    [Header("Refs (程式會自動抓取)")]
    public Transform leftHand;
    public Transform rightHand;
    public Transform bossTarget;
    public Camera cam;

    [Header("1. 階段切換設定")]
    public GameTimer timer;
    public float slamPhaseThreshold = 15f;

    [Header("2. 打擊特效顏色設定")]
    public Color slashHitColor = new Color(0f, 0.8f, 1f); 
    public Color slamHitColor = new Color(1f, 0.2f, 0f);  

    [Header("3. 發射點設定 (Muzzles)")]
    public Transform leftMuzzle;
    public Transform rightMuzzle;
    public Transform slamMuzzle;

    [Header("4. 飛彈 Prefabs")]
    public GameObject slashProjectilePrefab;
    public GameObject slamProjectilePrefab;   
    public GameObject hitEffectPrefab;

    [Header("5. 飛行參數")]
    public float slashSpeed = 12f;
    public float slashMaxLife = 2.0f;
    public float slashUpBias = 0.10f;
    public float slamSpeed = 14f;
    public float slamMaxLife = 2.0f;
    public float slamUpBias = 0.06f;

    [Header("6. 系統連結")]
    public DamageCalculator damage;
    public ComboCounter combo;

    private void Awake()
    {
        if (cam == null) cam = Camera.main;
        if (timer == null) timer = FindObjectOfType<GameTimer>();
        if (damage == null) damage = FindObjectOfType<DamageCalculator>();
        if (combo == null) combo = FindObjectOfType<ComboCounter>();

        if (bossTarget == null)
        {
            GameObject bossObj = GameObject.FindGameObjectWithTag("Boss");
            if (bossObj != null) bossTarget = bossObj.transform;
        }
    }

    void Update()
    {
        if (!GamePlayController.IsPlaying) return;

        if (Input.GetKeyDown(KeyCode.Space)) SpawnSwingByPhase("Right", 1.0f);
        if (Input.GetKeyDown(KeyCode.LeftControl)) SpawnSlamByPhase(1.0f);
    }

    bool IsInSlamPhase() => timer != null && timer.TimeLeft <= slamPhaseThreshold;

    public void SpawnSwingByPhase(string who, float strength)
    {
        if (IsInSlamPhase()) return;
        SpawnSlashProjectile(who, strength);
    }

    public void SpawnSlamByPhase(float strength)
    {
        if (!IsInSlamPhase()) return;
        SpawnSlamProjectile(strength);
    }

    private void SpawnSlashProjectile(string who, float strength)
    {
        if (!GamePlayController.IsPlaying || !slashProjectilePrefab) return;

        bool isLeft = (who == "Left");
        Transform hand = isLeft ? leftHand : rightHand;
        Transform muzzle = isLeft ? leftMuzzle : rightMuzzle;
        if (!hand) return;

        Vector3 spawnPos = muzzle ? muzzle.position : hand.position;
        
        // ★ 飛彈生成，不進行 localScale 的縮放
        GameObject go = Instantiate(slashProjectilePrefab, spawnPos, slashProjectilePrefab.transform.rotation);
        
        float k = Mathf.Clamp01(strength);

        var homing = go.GetComponent<ProjectileHoming>();
        if (homing)
        {
            homing.Launch(bossTarget, slashSpeed, slashMaxLife, slashUpBias, hitEffectPrefab, damage, combo, false, k);
            homing.impactColor = slashHitColor;
        }
    }

    private void SpawnSlamProjectile(float strength)
    {
        if (!GamePlayController.IsPlaying || !slamProjectilePrefab) return;

        Vector3 spawnPos;
        if (slamMuzzle)
        {
            spawnPos = slamMuzzle.position;
        }
        else
        {
            if (!leftHand || !rightHand) return;
            spawnPos = 0.5f * (leftHand.position + rightHand.position);
        }

        // ★ 飛彈生成，不進行 localScale 的縮放
        GameObject go = Instantiate(slamProjectilePrefab, spawnPos, slamProjectilePrefab.transform.rotation);
        
        float k = Mathf.Clamp01(strength);

        var homing = go.GetComponent<ProjectileHoming>();
        if (homing)
        {
            homing.Launch(bossTarget, slamSpeed, slamMaxLife, slamUpBias, hitEffectPrefab, damage, combo, true, k);
            homing.impactColor = slamHitColor;
        }
    }
}