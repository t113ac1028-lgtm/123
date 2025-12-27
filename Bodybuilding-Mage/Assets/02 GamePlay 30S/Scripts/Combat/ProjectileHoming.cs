using UnityEngine;

/// <summary>
/// 控制飛彈的飛行與追蹤邏輯。
/// 包含：結算攔截、打擊特效隨機化、縮放與顏色同步。
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

    // 儲存從 Spawner 傳來的顏色 (由外部直接設定屬性)
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

    /// <summary>
    /// 由 Spawner 呼叫：初始化並啟動。
    /// 維持 9 個參數版本以解決編譯衝突。
    /// </summary>
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

        // 獲取碰撞器並防呆設定
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
            DoHit();
            return;
        }

        // 飛彈本身的縮放動畫 (視覺點綴，非力量縮放)
        float s = Mathf.Max(0.001f, scaleOverLife.Evaluate(t01));
        if (visualRoot) visualRoot.localScale = _baseScale * s;
        else transform.localScale = _baseScale * s;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other != null && other.CompareTag("Boss"))
        {
            DoHit();
        }
    }

    void DoHit()
    {
        // ★★★ 核心門禁：如果遊戲已結束 (Playing=false)，飛彈直接消失 ★★★
        // 同時攔截計分、音效、特效，解決 0 秒後的雜音問題。
        if (!GamePlayController.IsPlaying)
        {
            Destroy(gameObject);
            return;
        }

        // 1. 傷害與 Combo 計算
        if (damage != null)
        {
            if (isSlam) damage.AddSlam(strength01, transform.position);
            else damage.AddSlash(strength01, transform.position);
        }

        // 2. BOSS 受擊反應
        if (target != null)
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

        // ★★★ 3. 強化版打擊特效 (隨機角度、縮放、顏色注入) ★★★
        if (hitEffect)
        {
            // A. 生成隨機 360 度旋轉的角度
            Quaternion randomRot = Quaternion.Euler(
                Random.Range(0f, 360f), 
                Random.Range(0f, 360f), 
                Random.Range(0f, 360f)
            );

            GameObject vfx = Instantiate(hitEffect, transform.position, randomRot);
            
            // B. 計算最終縮放：基礎倍率 + (揮劍強度 * 加成權重)
            // 如果是 Slam 重擊，特效基礎尺寸再放大 1.5 倍
            float typeBonus = isSlam ? 1.5f : 1.0f;
            float finalScale = (baseHitEffectScale + (strength01 * strengthImpactScale)) * typeBonus;
            
            vfx.transform.localScale = Vector3.one * finalScale;

            // C. 注入顏色到特效內部的粒子系統中
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