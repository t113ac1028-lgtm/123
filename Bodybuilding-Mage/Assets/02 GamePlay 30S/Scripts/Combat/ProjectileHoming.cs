using UnityEngine;

/// <summary>
/// 控制飛彈的飛行與追蹤邏輯。
/// 包含：結算攔截、打擊特效隨機化、縮放與顏色同步、以及護盾辨識。
/// </summary>
public class ProjectileHoming : MonoBehaviour
{
    [Header("Targeting / Motion")]
    public Transform target;                 
    public float speed = 12f;
    public float maxLife = 2.0f;             
    public float upwardBias = 0.25f;         
    [Range(0f, 1f)] public float homingStrength = 0.85f;

    [Header("Hit Settings")]
    public float hitDistance = 0.45f;        
    public LayerMask hitMask;                
    public GameObject hitEffect;
    
    [Header("打擊視覺強化")]
    [Tooltip("基礎特效大小倍率 (建議 2.5)")]
    public float baseHitEffectScale = 2.5f;
    [Tooltip("力量強度對特效大小的影響權重 (建議 2.0)")]
    public float strengthImpactScale = 2.0f;

    [HideInInspector] public Color impactColor = Color.white;

    [Header("Scoring")]
    public DamageCalculator damage;
    public ComboCounter combo;
    public bool isSlam = false;
    [Range(0f, 1f)] public float strength01 = 1f;

    [Header("Curves")]
    public AnimationCurve loftOverLife = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
    public AnimationCurve homingGain = new AnimationCurve(
        new Keyframe(0f, 0.15f),
        new Keyframe(0.35f, 0.35f),
        new Keyframe(0.7f, 0.8f),
        new Keyframe(1f, 1f)
    );
    public AnimationCurve scaleOverLife = new AnimationCurve(
        new Keyframe(0f, 0.75f),
        new Keyframe(0.25f, 1.0f),
        new Keyframe(1f, 0.85f)
    );

    [Header("Visual (optional)")]
    public Transform visualRoot;             

    Vector3 _vel;
    float _life;
    Vector3 _baseScale;

    public void Launch(Transform tgt, float spd, float life, float upBias,
                       GameObject hitFx, DamageCalculator dmg, ComboCounter cmb,
                       bool slam, float str01) 
    {
        target = tgt;
        speed = spd;
        maxLife = life;
        upwardBias = upBias;
        hitEffect = hitFx;
        damage = dmg;
        combo = cmb;
        isSlam = slam;
        strength01 = str01;

        _vel = transform.forward * speed;   
        _life = 0f;
        _baseScale = visualRoot ? visualRoot.localScale : transform.localScale;

        var projectileCol = GetComponent<Collider>();
        if (!projectileCol) projectileCol = gameObject.AddComponent<SphereCollider>();
        projectileCol.isTrigger = true;
        
        if (projectileCol is SphereCollider sc && sc.radius < 0.12f) 
            sc.radius = 0.18f;

        var rb = GetComponent<Rigidbody>();
        if (!rb) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        _life += dt;
        if (_life > maxLife || target == null) { Destroy(gameObject); return; }

        float t01 = Mathf.Clamp01(_life / Mathf.Max(0.0001f, maxLife));

        Vector3 desiredDir = transform.forward;
        Vector3 to = (target.position - transform.position);
        if (to.sqrMagnitude > 1e-6f)
        {
            float loft = Mathf.Max(0f, loftOverLife.Evaluate(t01));           
            Vector3 loftVec = Vector3.up * upwardBias * loft;                 
            desiredDir = (to.normalized + loftVec).normalized;
        }

        float gain = Mathf.Clamp01(homingGain.Evaluate(t01));                  
        Vector3 desiredVel = desiredDir * speed;
        _vel = Vector3.Lerp(_vel, desiredVel, gain * dt * 5f);                 

        transform.position += _vel * dt;

        if (to.sqrMagnitude <= hitDistance * hitDistance)
        {
            // 如果因為距離太近直接命中目標，預設當作打在 Boss 身上 (false)
            DoHit(transform.position, false);
            return;
        }

        float s = Mathf.Max(0.001f, scaleOverLife.Evaluate(t01));
        if (visualRoot) visualRoot.localScale = _baseScale * s;
        else transform.localScale = _baseScale * s;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other == null) return;

        // ★ 1. 檢查是否撞到護盾 ★
        ShieldHitVFX shield = other.GetComponent<ShieldHitVFX>();
        if (shield != null)
        {
            Vector3 hitPoint = other.ClosestPoint(transform.position);
            shield.TriggerShieldHit(hitPoint);

            // ★ 告訴系統：這次是打在護盾上 (傳入 true)
            DoHit(hitPoint, true);
            return;
        }

        // 2. 檢查是否撞到 Boss 本體
        if (other.CompareTag("Boss"))
        {
            // ★ 告訴系統：這次是直接打在 Boss 肉體上 (傳入 false)
            DoHit(transform.position, false);
        }
    }

    // ★ 加入參數 isHittingShield 來控制 Boss 反應
    void DoHit(Vector3 hitPosition, bool isHittingShield)
    {
        if (!GamePlayController.IsPlaying)
        {
            Destroy(gameObject);
            return;
        }

        // 1. 傷害與 Combo 計算 (打護盾還是會算分！)
        if (damage != null)
        {
            if (isSlam) damage.AddSlam(strength01, hitPosition);
            else damage.AddSlash(strength01, hitPosition);
        }

        // ★ 2. BOSS 受擊反應 (如果是打在護盾上，就跳過這段不執行)
        if (target != null && !isHittingShield)
        {
            BossHitControl hitCtrl = target.GetComponentInParent<BossHitControl>();
            if (hitCtrl != null)
            {
                hitCtrl.TryHit();
            }
            else
            {
                Animator bossAnim = target.GetComponentInParent<Animator>();
                if (bossAnim != null)
                {
                    if (isSlam || Random.value < 0.2f) bossAnim.SetTrigger("Hit");
                }
            }
        }

        // 3. 強化版打擊特效 (位置依然會出現在命中點)
        if (hitEffect)
        {
            Quaternion randomRot = Quaternion.Euler(
                Random.Range(0f, 360f), 
                Random.Range(0f, 360f), 
                Random.Range(0f, 360f)
            );

            GameObject vfx = Instantiate(hitEffect, hitPosition, randomRot);
            
            float typeBonus = isSlam ? 1.5f : 1.0f;
            float finalScale = (baseHitEffectScale + (strength01 * strengthImpactScale)) * typeBonus;
            
            vfx.transform.localScale = Vector3.one * finalScale;

            ParticleSystem[] psList = vfx.GetComponentsInChildren<ParticleSystem>();
            foreach (var ps in psList)
            {
                var main = ps.main;
                main.startColor = impactColor;
            }

            Destroy(vfx, 0.8f);
        }

        Destroy(gameObject);
    }
}