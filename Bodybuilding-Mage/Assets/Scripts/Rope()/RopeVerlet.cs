using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class RopeVerlet : MonoBehaviour
{
    [Header("Anchors")]
    public Transform startPoint;              // 繩頭（跟手）
    public Transform endPoint;                // 可空（自由尾）

    [Header("Length & Segments")]
    public int segments = 32;                 // 越多越硬、波更細
    public bool useTotalLength = true;
    public float totalLength = 4.0f;          // 繩總長
    public float segmentLength = 0.15f;       // 不用總長時才生效
    [Range(0,0.3f)] public float slack = 0.0f;

    [Header("Simulation")]
    public int substeps = 4;
    public int constraintIterations = 10;
    [Range(0.9f,0.999f)] public float damping = 0.985f;     // 全局黏滯
    public Vector3 gravity = new Vector3(0,-1.0f,0);        // 給一點重量感

    [Header("Heaviness / Resistance")]
    [Range(0,3)] public float airDrag = 0.8f;               // 空氣阻力（速度平方）
    [Range(0,1)] public float compressionFriction = 0.15f;  // 受壓時的“鏈內摩擦”
    [Range(0,1)] public float bendStiffness = 0.55f;        // 彎曲剛性（i 到 i+2）
    public float maxStretchRatio = 1.08f;                   // 單段最大伸長

    [Header("Hand Coupling")]
    [Range(0,1)] public float handFollowLerp = 0.35f;       // 繩頭跟手的平滑：小=更重
    public float impulseStrength = 1.6f;                    // 手速→脈衝強度
    public int influenceNodes = 6;                          // 前幾節吃到脈衝
    public float impulseMaxStep = 0.25f;                    // 防爆：單幀最大推動

    [Header("Tail Guide (no EndPoint)")]
    public bool useGuide = true;
    public Vector3 guideDir = Vector3.forward;              // 預設 +Z
    [Range(0,1)] public float guideStrength = 0.35f;        // 拉直到 +Z*總長
    public int guideAffectLastN = 10;                       // 影響尾端最後 N 節

    [Header("Ground (optional)")]
    public bool collideGround = true;
    public float groundY = 0f;
    [Range(0,1)] public float groundBounciness = 0.0f;      // 0=黏住、>0=彈

    private LineRenderer lr;
    private Vector3[] pos;
    private Vector3[] prev;
    private Vector3 lastHand;
    private bool handInit;

#if UNITY_EDITOR
    // 自動重建
    private int _ls; private float _llen; private float _seg;
#endif

    void OnValidate()
    {
        if (useTotalLength && segments > 1)
            segmentLength = Mathf.Max(0.01f, totalLength / (segments - 1f));
#if UNITY_EDITOR
        if (_ls!=segments || Mathf.Abs(_llen-totalLength)>1e-4f || Mathf.Abs(_seg-segmentLength)>1e-4f)
        {
            _ls=segments; _llen=totalLength; _seg=segmentLength;
            if (Application.isPlaying) InitRope();
        }
#endif
    }

    void Start()
    {
        lr = GetComponent<LineRenderer>();
        InitRope();
    }

    [ContextMenu("Reset Rope Now")]
    public void InitRope()
    {
        if (!startPoint) return;

        if (useTotalLength && segments > 1)
            segmentLength = Mathf.Max(0.01f, totalLength / (segments - 1f));

        pos  = new Vector3[segments];
        prev = new Vector3[segments];

        var a = startPoint.position;
        var dir = guideDir.normalized;
        float step = segmentLength * (1f + slack);

        for (int i = 0; i < segments; i++)
        {
            pos[i]  = a + dir * step * i;
            prev[i] = pos[i];
        }

        lr.positionCount = segments;
        lr.SetPositions(pos);
        handInit = false;
    }

    void Update()
    {
        if (!startPoint || pos == null) return;

        float dt = Mathf.Min(Time.deltaTime, 1f/60f);
        int n = segments;

        // 手位置/速度
        var hand = startPoint.position;
        if (!handInit) { lastHand = hand; handInit = true; }
        var handDelta = hand - lastHand; lastHand = hand;
        var handSpeed = handDelta.magnitude / Mathf.Max(dt, 1e-4f);
        if (handDelta.magnitude > impulseMaxStep) handDelta = handDelta.normalized * impulseMaxStep;

        float subDt = dt / substeps;
        for (int s = 0; s < substeps; s++)
        {
            // --- Verlet 積分 + 阻尼 + 空氣阻力 ---
            for (int i = 0; i < n; i++)
            {
                var p = pos[i];
                var v = (pos[i] - prev[i]);

                // 全域黏滯
                v *= damping;

                // 空氣阻力（速度平方反向）
                float speed = v.magnitude / Mathf.Max(subDt,1e-4f);
                if (speed > 1e-4f)
                    v += (-v.normalized) * (airDrag * speed * subDt);

                // 重力
                v += gravity * (subDt * subDt);

                prev[i] = p;
                pos[i]  = p + v;
            }

            // --- 繩頭跟手（小 Lerp = 更重） ---
            pos[0] = Vector3.Lerp(pos[0], hand, handFollowLerp);
            prev[0] = pos[0];

            // --- 手速轉成“脈衝”推動前段（創造波） ---
            if (handSpeed > 0.2f)
            {
                for (int i = 1; i < Mathf.Min(influenceNodes, n); i++)
                {
                    float w = 1f - i / (float)influenceNodes;
                    var impulse = handDelta * (impulseStrength * w);
                    pos[i] += impulse;
                }
            }

            // --- 地面碰撞（簡單 y=groundY） ---
            if (collideGround)
            {
                for (int i = 0; i < n; i++)
                {
                    if (pos[i].y < groundY)
                    {
                        float vy = (pos[i].y - prev[i].y) / Mathf.Max(subDt,1e-4f);
                        pos[i].y = groundY;
                        prev[i].y = pos[i].y + (-vy * groundBounciness * subDt); // 吃掉垂直速度
                    }
                }
            }

            // --- 反壓摩擦：避免被擠成一坨 ---
            if (compressionFriction > 0)
            {
                for (int i = 0; i < n-1; i++)
                {
                    var d = pos[i+1] - pos[i];
                    float dist = d.magnitude;
                    if (dist < segmentLength) // 受壓
                    {
                        var push = (segmentLength - dist) * compressionFriction;
                        var dir = (dist>1e-5f)? d/dist : Vector3.forward;
                        pos[i]   -= dir * push * 0.5f;
                        pos[i+1] += dir * push * 0.5f;
                    }
                }
            }

            // --- 約束：距離 + 伸長上限 ---
            for (int it = 0; it < constraintIterations; it++)
            {
                // 距離
                for (int i = 0; i < n-1; i++)
                {
                    int j = i + 1;
                    Vector3 delta = pos[j] - pos[i];
                    float dist = delta.magnitude;
                    if (dist < 1e-6f) continue;

                    float maxDist = segmentLength * maxStretchRatio;
                    dist = Mathf.Min(dist, maxDist);

                    float diff = (dist - segmentLength) / dist;
                    Vector3 corr = delta * diff * 0.5f;

                    pos[i]   += corr;
                    pos[j]   -= corr;
                }

                // 彎曲剛性（i 與 i+2）
                if (bendStiffness > 0)
                {
                    float target = 2f * segmentLength;
                    for (int i = 0; i < n-2; i++)
                    {
                        int k = i + 2;
                        Vector3 d = pos[k] - pos[i];
                        float dist = d.magnitude;
                        if (dist < 1e-6f) continue;
                        float diff = (dist - target) / dist;
                        Vector3 corr = d * diff * (0.5f * bendStiffness);
                        pos[i]   += corr;
                        pos[k]   -= corr;
                    }
                }

                // 固定兩端（有 EndPoint 時）
                pos[0] = hand;
                if (endPoint)
                {
                    pos[n-1]  = endPoint.position;
                    prev[n-1] = pos[n-1];
                }
            }

            // --- 沒 EndPoint 時用尾導向，維持張力與朝向 +Z ---
            if (!endPoint && useGuide)
            {
                Vector3 baseDir = guideDir.normalized;
                Vector3 targetTail = hand + baseDir * totalLength;
                int lastN = Mathf.Clamp(guideAffectLastN, 1, n-1);
                for (int t = 0; t < lastN; t++)
                {
                    int idx = n-1 - t;
                    float w = guideStrength * (t+1) / lastN;
                    pos[idx] = Vector3.Lerp(pos[idx], targetTail, w * subDt * 8f);
                }
            }
        }

        lr.positionCount = segments;
        lr.SetPositions(pos);
    }
}
