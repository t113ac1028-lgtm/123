using UnityEngine;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 計分邏輯（力量．Combo．穩定度）
///  - 外部只要呼叫 AddSlash / AddSlam 並給 strength01（0~1）即可。
///  - 分數顯示、HitNumber、LastHit 都在這支裡一起處理。
///  - 新增：依照 GameTimer 剩餘秒數，前半段只認 Slash、後半段只認 Slam。
/// </summary>
public class DamageCalculator : MonoBehaviour
{
    [Header("音效")]
    public AudioSource audioSource;   // 拖一個 AudioSource 進來
    public AudioClip slashSfx;        // 一般揮砍音效
    public AudioClip slamSfx;         // 重擊音效

    [Header("UI")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI lastHitText;
    public TextMeshProUGUI debugStrengthText;

    [Header("Hit Number (可選)")]
    public HitNumberManager hitNumbers;
    public Camera cam;

    [Header("Base & Move Type")]
    [Tooltip("所有攻擊的基礎分數")]
    public float baseDamage = 1000f;
    [Tooltip("Slash 乘數（1 = 就是 baseDamage）")]
    public float slashMul = 1f;
    [Tooltip("Slam 乘數（>1 代表比 slash 更吃力，分數比較高）")]
    public float slamMul = 2.2f;

    [Header("力量 STR")]
    [Tooltip("低於這個強度視為太小力，只拿到 veryLowStrengthMul 的倍率")]
    [Range(0f, 1f)] public float minStrength = 0.30f;
    [Tooltip("力量滿格時能拿到的最高倍率（在基礎分上再乘以這個）")]
    public float strengthMaxMul = 1.6f;
    [Tooltip("低於 minStrength 時給的保底倍率")]
    public float veryLowStrengthMul = 0.3f;

    [Header("Combo")]
    [Tooltip("每 N Combo 算一階（用來給加成）")]
    public int comboStep = 5;
    [Tooltip("每升一階 Combo 額外 +x 倍，例如 0.2 = +20%")]
    public float comboTierBonus = 0.20f;
    [Tooltip("最多吃到幾階 Combo 加成")]
    public int maxComboTier = 4;
    public ComboCounter combo;     // 記得在 Inspector 連進來

    [Header("穩定度（節奏與平穩度）")]
    public float windowSec = 4f;
    public float targetHzMin = 1.3f;
    public float targetHzMax = 2.2f;
    public float cvGood       = 0.15f;
    public float cvBad        = 0.45f;
    public float stabilityMinMul = 0.85f;
    public float stabilityMaxMul = 1.30f;

    [HideInInspector] public float lastHz;           // 最近頻率
    [HideInInspector] public float lastStability01;  // 0~1 穩定度

    [Header("Slash / Slam 階段切換")]
    [Tooltip("把 GameTimer 拖進來，用來判斷目前剩餘秒數")]
    public GameTimer timer;
    [Tooltip("剩餘秒數 <= 這個值時，進入 Slam 階段（例如 15 秒）")]
    public float slamPhaseThreshold = 15f;

    int total;
    readonly Queue<float> hitTimes = new Queue<float>();

    // ---------- 對外介面 ----------

    public void ResetScore()
    {
        total = 0;
        if (scoreText)    scoreText.text = $"{total:000000}";
        if (lastHitText)  lastHitText.text = string.Empty;
        hitTimes.Clear();
    }

    /// <summary>一般 Slash 命中（由 AltAndSlamCoordinator 呼叫）</summary>
    public void AddSlash(float strength01, Vector3 worldFrom)
    {
        HandleAttack(strength01, worldFrom, isSlamRequested: false);
    }

    /// <summary>Slam 命中（由 AltAndSlamCoordinator 呼叫）</summary>
    public void AddSlam(float strength01, Vector3 worldFrom)
    {
        HandleAttack(strength01, worldFrom, isSlamRequested: true);
    }

    // 統一進入口：在這裡依時間段「最後決定」是 Slash 還是 Slam
    void HandleAttack(float strength01, Vector3 worldFrom, bool isSlamRequested)
    {
        if (debugStrengthText)
            debugStrengthText.text = $"STR {strength01:0.00}";

        bool isSlamActual = DecideSlamByPhase(isSlamRequested);

        // 1. 先更新 Combo（這裡 slam 才會真的多 +1）
        if (combo != null)
        {
            combo.RegisterHit(isSlamActual, strength01);
        }

        // 2. 算傷害
        int dmg = Mathf.RoundToInt(ComputeDamage(strength01, isSlamActual));

        // 3. 播音效
        PlaySfx(isSlamActual ? slamSfx : slashSfx);

        // 4. 加進總分 & 顯示
        ApplyScore(dmg, worldFrom);
    }

    // 根據 GameTimer 決定「這一下」到底當 Slash 還是 Slam
    bool DecideSlamByPhase(bool isSlamRequested)
    {
        // 沒有 Timer 就保持原本行為
        if (timer == null)
            return isSlamRequested;

        float t = timer.TimeLeft;

        // 前半段（30~16秒）：一律視為 Slash
        if (t > slamPhaseThreshold)
            return false;

        // 後半段（15~0秒）：一律視為 Slam
        return true;
    }

    // 播音效
    void PlaySfx(AudioClip clip)
    {
        if (audioSource == null || clip == null) return;
        audioSource.PlayOneShot(clip);
    }

    // ---------- 核心計算 ----------

    float ComputeDamage(float strength01, bool isSlam)
    {
        float now = Time.time;
        RegisterHitTime(now);

        // 1) 動作種類倍率
        float typeMul = isSlam ? slamMul : slashMul;

        // 2) 力量 STR 乘數
        float strengthMul = StrengthMultiplier(strength01);

        // 3) Combo 乘數（Combo 已在 HandleAttack 先更新）
        int tier = combo ? combo.Tier(comboStep) : 0;
        tier = Mathf.Clamp(tier, 0, maxComboTier);
        float comboMul = 1f + comboTierBonus * tier;

        // 4) 穩定度乘數
        float stabilityMul = StabilityMultiplier();

        float dmg = baseDamage * typeMul * strengthMul * comboMul * stabilityMul;
        return dmg;
    }

    float StrengthMultiplier(float strength01)
    {
        strength01 = Mathf.Clamp01(strength01);

        if (strength01 < minStrength)
            return veryLowStrengthMul;

        float t = Mathf.InverseLerp(minStrength, 1f, strength01);
        return Mathf.Lerp(1f, strengthMaxMul, t);
    }

    float StabilityMultiplier()
    {
        if (hitTimes.Count < 3)
            return 1f;

        int n = hitTimes.Count;
        float[] times = hitTimes.ToArray();
        float sum = 0f;
        int intervalCount = n - 1;
        float[] intervals = new float[intervalCount];

        for (int i = 1; i < n; i++)
        {
            float dt = Mathf.Max(0.0001f, times[i] - times[i - 1]);
            intervals[i - 1] = dt;
            sum += dt;
        }

        float mean = sum / intervalCount;
        float hz = 1f / mean;

        float var = 0f;
        for (int i = 0; i < intervalCount; i++)
        {
            float d = intervals[i] - mean;
            var += d * d;
        }
        var /= intervalCount;
        float std = Mathf.Sqrt(var);
        float cv = (mean > 0f) ? (std / mean) : 1f;

        float stableScore = Mathf.InverseLerp(cvBad, cvGood, Mathf.Clamp(cv, 0f, 10f));
        stableScore = Mathf.Clamp01(stableScore);

        float freqScore = 1f;
        float mid = 0.5f * (targetHzMin + targetHzMax);
        float halfRange = 0.5f * (targetHzMax - targetHzMin);
        if (halfRange > 0f)
        {
            float dist = Mathf.Abs(hz - mid);
            if (dist <= halfRange)
            {
                freqScore = 1f;
            }
            else
            {
                float extra = dist - halfRange;
                float maxExtra = halfRange;
                float t = Mathf.Clamp01(extra / Mathf.Max(0.0001f, maxExtra));
                freqScore = 1f - t;
            }
        }

        float overall = stableScore * (0.5f + 0.5f * freqScore);
        lastHz = hz;
        lastStability01 = overall;

        return Mathf.Lerp(stabilityMinMul, stabilityMaxMul, overall);
    }

    // ---------- Hit 註冊與分數套用 ----------

    void RegisterHitTime(float t)
    {
        hitTimes.Enqueue(t);
        while (hitTimes.Count > 0 && (t - hitTimes.Peek()) > windowSec)
            hitTimes.Dequeue();
    }

    void ApplyScore(int dmg, Vector3 worldFrom)
    {
        total += dmg;

        if (scoreText)   scoreText.text = $"{total:000000}";
        if (lastHitText) lastHitText.GetComponent<LastHitFade>()?.Show($"+{dmg}");
        if (hitNumbers && cam) hitNumbers.Spawn(worldFrom, dmg, cam);
    }

    public int Total() => total;   // 給 GamePlayController 用（原本就有的）
}
