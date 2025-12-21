using UnityEngine;

public class ProjectileHoming : MonoBehaviour
{
    [Header("Targeting / Motion")]
    public Transform target;                 // Boss 目標（Spawner 會帶進來）
    public float speed = 12f;
    public float maxLife = 2.0f;             // 存活秒數
    [Tooltip("基礎的上拋量，會乘上 loftOverLife 曲線。")]
    public float upwardBias = 0.25f;         // ↑ 提高一點就更拋；0.15~0.35 常用
    [Tooltip("0~1：越大越容易黏住目標，會乘上 homingGain 曲線。")]
    [Range(0f, 1f)] public float homingStrength = 0.85f;

    [Header("Hit Settings")]
    public float hitDistance = 0.45f;        // 備援：距離小於此值視為命中
    public LayerMask hitMask;                // 可留空用 Tag 判定
    public GameObject hitEffect;

    [Header("Scoring")]
    public DamageCalculator damage;
    public ComboCounter combo;
    public bool isSlam = false;
    [Range(0f, 1f)] public float strength01 = 1f;

    [Header("Curves (可在 Inspector 微調味道)")]
    [Tooltip("上拋量隨時間的曲線：0→先多、到 1 收斂回 0。")]
    public AnimationCurve loftOverLife = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
    [Tooltip("追蹤力隨時間：一開始弱、後面強，會更像『先拋後收』。")]
    public AnimationCurve homingGain = new AnimationCurve(
        new Keyframe(0f, 0.15f),
        new Keyframe(0.35f, 0.35f),
        new Keyframe(0.7f, 0.8f),
        new Keyframe(1f, 1f)
    );
    [Tooltip("視覺大小隨時間（避免一開始擋視野）：0.7→1→0.8 之類。")]
    public AnimationCurve scaleOverLife = new AnimationCurve(
        new Keyframe(0f, 0.75f),
        new Keyframe(0.25f, 1.0f),
        new Keyframe(1f, 0.85f)
    );

    [Header("Visual (optional)")]
    public Transform visualRoot;             // 若指定，僅改它的 scale；不指定就改整體 scale

    // ---- runtime ----
    Vector3 _vel;
    float _life;
    Vector3 _baseScale;

    /// <summary>
    /// 由 Spawner 呼叫：初始化並啟動
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

        _vel = transform.forward * speed;   // 以 Prefab 的朝向為初速
        _life = 0f;

        // 初始尺寸
        _baseScale = visualRoot ? visualRoot.localScale : transform.localScale;

        // 確保是 Trigger 碰撞 + Kinematic 剛體（避免穿透 & 易命中）
        var col = GetComponent<Collider>();
        if (!col) col = gameObject.AddComponent<SphereCollider>();
        col.isTrigger = true;
        if (col is SphereCollider sc && sc.radius < 0.12f) sc.radius = 0.18f;

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

        // ---- 1) 計算欲前進方向：目標方向 + 上拋量（隨時間遞減）----
        Vector3 desiredDir = transform.forward;
        Vector3 to = (target.position - transform.position);
        if (to.sqrMagnitude > 1e-6f)
        {
            float loft = Mathf.Max(0f, loftOverLife.Evaluate(t01));           // 0..1
            Vector3 loftVec = Vector3.up * upwardBias * loft;                 // 上拋向量
            desiredDir = (to.normalized + loftVec).normalized;
        }

        // ---- 2) 追蹤力隨時間增強（前期弱、後期強）----
        float gain = Mathf.Clamp01(homingGain.Evaluate(t01));                  // 0..1
        Vector3 desiredVel = desiredDir * speed;
        _vel = Vector3.Lerp(_vel, desiredVel, gain * dt * 5f);                 // *5f 讓回應更明顯

        // ---- 3) 推進（不改 rotation，保留你 Prefab 的視覺角度）----
        transform.position += _vel * dt;

        // ---- 4) 命中：Trigger 或備援距離皆可觸發 ----
        if (to.sqrMagnitude <= hitDistance * hitDistance)
        {
            DoHit();
            return;
        }

        // ---- 5) 視覺尺寸隨時間（減擋視野）----
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
        // 1. 原本的傷害計算
        if (damage != null)
        {
            if (isSlam) damage.AddSlam(strength01, transform.position);
            else damage.AddSlash(strength01, transform.position);
        }

        // ---------------------------------------------------------
        // 2. BOSS 受擊動畫觸發邏輯 (整合 BossHitControl)
        // ---------------------------------------------------------
        if (target != null)
        {
            // A計畫：優先嘗試尋找 BossHitControl (防抽搐管理器)
            // GetComponentInParent 會自動往上找，直到找到掛在 Boss 最外層的那個腳本
            BossHitControl hitCtrl = target.GetComponentInParent<BossHitControl>();

            if (hitCtrl != null)
            {
                // 如果有掛腳本，直接交給它處理 (它會自己算次數、防抽筋)
                hitCtrl.TryHit();
            }
            else
            {
                // B計畫：(備案) 如果沒掛 BossHitControl，就用舊的方法直接找 Animator
                Animator bossAnim = target.GetComponentInParent<Animator>();
                if (bossAnim != null)
                {
                    // Slam 必定觸發，普通攻擊 20% 機率觸發 (防止太過鬼畜)
                    if (isSlam || Random.value < 0.2f)
                    {
                        bossAnim.SetTrigger("Hit");
                    }
                }
            }
        }
        // ---------------------------------------------------------

        // 3. 特效生成
        if (hitEffect)
            Destroy(Instantiate(hitEffect, transform.position, Quaternion.identity), 0.8f);

        // 4. 銷毀飛彈自己
        Destroy(gameObject);
    }
}