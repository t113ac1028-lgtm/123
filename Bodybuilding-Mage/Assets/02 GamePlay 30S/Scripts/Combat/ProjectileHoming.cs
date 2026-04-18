using UnityEngine;

/// <summary>
/// 控制飛彈的飛行與追蹤邏輯。
/// 包含：結算攔截、打擊特效隨機化、縮放與顏色同步、以及護盾辨識。
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SphereCollider))]
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

    private Vector3 _vel;
    private float _life;
    private Vector3 _baseScale;
    private Vector3 _originalScale;

    // Pool回收callback，由Spawner設置；為null時改用Destroy
    private System.Action<GameObject> _returnToPool;

    // 快取Boss元件，避免每次命中都呼叫GetComponentInParent
    private BossHitControl _bossHitCtrl;
    private Animator _bossAnim;

    // 快取物理元件，避免Launch時重複GetComponent/AddComponent
    private Collider _col;
    private Rigidbody _rb;

    private void Awake()
    {
        _originalScale = visualRoot ? visualRoot.localScale : transform.localScale;

        _col = GetComponent<Collider>();
        if (!_col)
        {
            var sc = gameObject.AddComponent<SphereCollider>();
            sc.radius = 0.18f;
            _col = sc;
        }
        else if (_col is SphereCollider sphere && sphere.radius < 0.12f)
        {
            sphere.radius = 0.18f;
        }
        _col.isTrigger = true;

        _rb = GetComponent<Rigidbody>();
        if (!_rb) _rb = gameObject.AddComponent<Rigidbody>();
        _rb.isKinematic = true;
        _rb.useGravity = false;
    }

    public void Launch(Transform tgt, float spd, float life, float upBias,
                       GameObject hitFx, DamageCalculator dmg, ComboCounter cmb,
                       bool slam, float str01,
                       System.Action<GameObject> returnToPool = null)
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
        _returnToPool = returnToPool;

        _vel = transform.forward * speed;
        _life = 0f;

        // 還原到原始大小（Pool重用時scale可能已被改變）
        if (visualRoot) visualRoot.localScale = _originalScale;
        else transform.localScale = _originalScale;
        _baseScale = _originalScale;

        // 在Launch時快取Boss元件，DoHit時不再搜尋
        _bossHitCtrl = tgt != null ? tgt.GetComponentInParent<BossHitControl>() : null;
        _bossAnim = (_bossHitCtrl == null && tgt != null) ? tgt.GetComponentInParent<Animator>() : null;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        _life += dt;
        if (_life > maxLife || target == null) { ReturnToPool(); return; }

        float t01 = Mathf.Clamp01(_life / Mathf.Max(0.0001f, maxLife));

        Vector3 to = target.position - transform.position;
        Vector3 desiredDir = transform.forward;
        if (to.sqrMagnitude > 1e-6f)
        {
            float loft = Mathf.Max(0f, loftOverLife.Evaluate(t01));
            desiredDir = (to.normalized + Vector3.up * (upwardBias * loft)).normalized;
        }

        float gain = Mathf.Clamp01(homingGain.Evaluate(t01));
        _vel = Vector3.Lerp(_vel, desiredDir * speed, gain * dt * 5f);

        transform.position += _vel * dt;

        if (to.sqrMagnitude <= hitDistance * hitDistance)
        {
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

        if (other.TryGetComponent<ShieldHitVFX>(out var shield))
        {
            Vector3 hitPoint = other.ClosestPoint(transform.position);
            shield.TriggerShieldHit(hitPoint);
            DoHit(hitPoint, true);
            return;
        }

        if (other.CompareTag("Boss"))
            DoHit(transform.position, false);
    }

    void DoHit(Vector3 hitPosition, bool isHittingShield)
    {
        if (!GamePlayController.IsPlaying)
        {
            ReturnToPool();
            return;
        }

        if (damage != null)
        {
            if (isSlam) damage.AddSlam(strength01, hitPosition);
            else damage.AddSlash(strength01, hitPosition);
        }

        if (!isHittingShield)
        {
            if (_bossHitCtrl != null)
                _bossHitCtrl.TryHit();
            else if (_bossAnim != null && (Random.value < 0.2f || isSlam))
                _bossAnim.SetTrigger("Hit");
        }

        if (hitEffect)
        {
            Quaternion randomRot = Quaternion.Euler(
                Random.Range(0f, 360f),
                Random.Range(0f, 360f),
                Random.Range(0f, 360f)
            );

            GameObject vfx = Instantiate(hitEffect, hitPosition, randomRot);
            float typeBonus = isSlam ? 1.5f : 1.0f;
            vfx.transform.localScale = Vector3.one * (baseHitEffectScale + strength01 * strengthImpactScale) * typeBonus;

            ParticleSystem[] psList = vfx.GetComponentsInChildren<ParticleSystem>();
            foreach (var ps in psList)
            {
                var main = ps.main;
                main.startColor = impactColor;
            }

            Destroy(vfx, 0.8f);
        }

        ReturnToPool();
    }

    private void ReturnToPool()
    {
        // 先清空callback再呼叫，防止重入
        var callback = _returnToPool;
        _returnToPool = null;
        target = null;

        if (callback != null)
            callback(gameObject);
        else
            Destroy(gameObject);
    }
}
