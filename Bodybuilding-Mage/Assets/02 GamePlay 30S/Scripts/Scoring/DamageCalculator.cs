using UnityEngine;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 計分邏輯（新版：只有「力量．Combo．穩定度」三個模組）
///  - 外部只要呼叫 AddSlash / AddSlam 並給 strength01（0~1）即可。
///  - 分數顯示、HitNumber、LastHit 都在這支裡一起處理。
/// </summary>
public class DamageCalculator : MonoBehaviour
{
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
    [Tooltip("拿最近幾秒的揮擊來估計穩定度")]
    public float windowSec = 4f;
    [Tooltip("希望玩家維持的目標頻率下限（次 / 秒）")]
    public float targetHzMin = 1.3f;   // 大約 1.3 ~ 2.2 可依實測再調
    [Tooltip("希望玩家維持的目標頻率上限（次 / 秒）")]
    public float targetHzMax = 2.2f;
    [Tooltip("變異係數 <= 這個值視為非常穩定")]
    public float cvGood = 0.15f;
    [Tooltip("變異係數 >= 這個值視為很不穩定")]
    public float cvBad = 0.45f;
    [Tooltip("穩定度最低倍率（亂揮亂停也還是有分數，不會變 0）")]
    public float stabilityMinMul = 0.85f;
    [Tooltip("穩定度最高倍率（維持好節奏可以拿到的上限）")]
    public float stabilityMaxMul = 1.30f;
    // ---------- Debug / UI 用 ----------
    [HideInInspector] public float lastHz;           // 最近計算出的頻率(次/秒)
    [HideInInspector] public float lastStability01;  // 0~1 的穩定度分數

    int total;
    readonly Queue<float> hitTimes = new Queue<float>();

    // ---------- 對外介面 ----------

    public void ResetScore()
    {
        total = 0;
        if (scoreText)    scoreText.text = $"{total:000000}";
        if (lastHitText)  lastHitText.text = string.Empty;
    }

    /// <summary>一般 Slash 命中</summary>
    public void AddSlash(float strength01, Vector3 worldFrom)
    {
        if (debugStrengthText)
            debugStrengthText.text = $"STR {strength01:0.00}";

        int dmg = Mathf.RoundToInt(ComputeDamage(strength01, isSlam: false));
        ApplyScore(dmg, worldFrom);
    }

    /// <summary>Slam 命中</summary>
    public void AddSlam(float strength01, Vector3 worldFrom)
    {
        if (debugStrengthText)
            debugStrengthText.text = $"STR {strength01:0.00}";

        int dmg = Mathf.RoundToInt(ComputeDamage(strength01, isSlam: true));
        ApplyScore(dmg, worldFrom);
    }

    // ---------- 核心計算 ----------

    float ComputeDamage(float strength01, bool isSlam)
    {
        float now = Time.time;
        RegisterHitTime(now);

        // 1) 動作種類
        float typeMul = isSlam ? slamMul : slashMul;

        // 2) 力量 STR 乘數
        float strengthMul = StrengthMultiplier(strength01);

        // 3) Combo 乘數
        int tier = combo ? combo.Tier(comboStep) : 0;
        tier = Mathf.Clamp(tier, 0, maxComboTier);
        float comboMul = 1f + comboTierBonus * tier;

        // 4) 穩定度乘數（同時看節奏區間與平穩度）
        float stabilityMul = StabilityMultiplier();

        float dmg = baseDamage * typeMul * strengthMul * comboMul * stabilityMul;
        return dmg;
    }

    float StrengthMultiplier(float strength01)
    {
        strength01 = Mathf.Clamp01(strength01);

        if (strength01 < minStrength)
        {
            // 太小力：不給完全 0 分，給一個保底
            return veryLowStrengthMul;
        }

        // 把 [minStrength, 1] 映射到 [0,1]
        float t = Mathf.InverseLerp(minStrength, 1f, strength01);
        // 對應到 [1, strengthMaxMul]
        return Mathf.Lerp(1f, strengthMaxMul, t);
    }

    float StabilityMultiplier()
    {
        if (hitTimes.Count < 3)
        {
            // 資料太少先給中性倍率
            return 1f;
        }

        // 1) 算出各次揮擊的間隔
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
        float hz = 1f / mean;              // 平均頻率（次 / 秒）

        // 2) 計算變異係數 CV = 標準差 / 平均
        float var = 0f;
        for (int i = 0; i < intervalCount; i++)
        {
            float d = intervals[i] - mean;
            var += d * d;
        }
        var /= intervalCount;
        float std = Mathf.Sqrt(var);
        float cv = (mean > 0f) ? (std / mean) : 1f;

        // ---- 2-1) 平穩度評分：CV 越小越好 ----
        float stableScore = Mathf.InverseLerp(cvBad, cvGood, Mathf.Clamp(cv, 0f, 10f));
        stableScore = Mathf.Clamp01(stableScore);   // 0 ~ 1

        // ---- 2-2) 頻率區間評分：在帶內最好，稍微快/慢一點也還可以 ----
        float freqScore = 1f;
        float mid = 0.5f * (targetHzMin + targetHzMax);
        float halfRange = 0.5f * (targetHzMax - targetHzMin);
        if (halfRange > 0f)
        {
            float dist = Mathf.Abs(hz - mid);
            if (dist <= halfRange)
            {
                // 帶內：給滿分
                freqScore = 1f;
            }
            else
            {
                // 出帶：距離越遠扣得越多，但不會瞬間掉到 0
                float extra = dist - halfRange;
                float maxExtra = halfRange; // 再多一個 halfRange 視為最差
                float t = Mathf.Clamp01(extra / Mathf.Max(0.0001f, maxExtra));
                freqScore = 1f - t;         // 1 -> 0
            }
        }

        // ---- 2-3) 合成穩定度分數 ----
        // 讓頻率只影響一半，真正決定好壞的是「平穩度」本身
        float overall = stableScore * (0.5f + 0.5f * freqScore); // 0 ~ 1

        // 把結果存起來給 UI 用
        lastHz = hz;
        lastStability01 = overall;
        // ---- 3) 映射到倍率區間 ----
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

    public int Total() => total;
}
