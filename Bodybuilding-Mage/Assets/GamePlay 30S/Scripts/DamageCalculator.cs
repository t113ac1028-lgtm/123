using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class DamageCalculator : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI lastHitText;
    [Header("Hit Number (可選)")]
    public HitNumberManager hitNumbers;
    public Camera cam;

    [Header("Base & Multipliers")]
    public float baseDamage = 1000f;
    public float slashMul   = 1.0f;
    public float slamMul    = 2.2f;
    [Tooltip("每 10 連一階，每階 +20%")]
    public float comboTierBonus = 0.20f;
    public ComboCounter combo; // 記得拖進來（用來取 tier）

    [Header("Tempo Target (Hz)")]
    [Tooltip("目標頻率下限（Hz），例如 1.6 ≈ 96 BPM")]
    public float targetHzMin = 1.6f;
    [Tooltip("目標頻率上限（Hz），例如 2.2 ≈ 132 BPM")]
    public float targetHzMax = 2.2f;
    [Tooltip("頻率在目標帶中央可達到的加成（1.00~1.50）")]
    public float tempoMaxBoost = 1.35f; // 中心帶 1.35 倍，帶外降到 0.85~1.0

    [Header("Stability (最近窗口)")]
    [Tooltip("近幾秒內的節奏穩定度評估")]
    public float windowSec = 4.0f;
    [Tooltip("抖動越小越接近這個上限（1.0~1.5）")]
    public float stabilityMaxBoost = 1.25f;
    [Tooltip("變異係數 CV 超過這個值就幾乎無加成")]
    public float stabilityCvBad = 0.35f; // 35% 抖動很糟
    [Tooltip("CV 低於這個值視為極穩")]
    public float stabilityCvGood = 0.12f; // 12% 抖動極穩

    [Header("Fatigue / Endurance")]
    [Tooltip("連續維持在節奏帶內會累積，最多 +10%")]
    public float enduranceMaxBoost = 1.10f;
    [Tooltip("達到滿耐力所需的秒數（在節奏帶內）")]
    public float enduranceBuildSec = 10f;
    [Tooltip("離開節奏帶後，耐力每秒衰減比例")]
    public float enduranceDecayPerSec = 0.5f;

    int total;
    readonly Queue<float> hitTimes = new Queue<float>();
    float endurance; // 0..1：耐力蓄積

    // ---- 對外介面（你原本就有）----
    public void AddSlash(float strength01, Vector3 worldFrom){
        int dmg = Mathf.RoundToInt( ComputeDamage(strength01, isSlam:false) );
        ApplyScore(dmg, worldFrom);
    }
    public void AddSlam(float strength01, Vector3 worldFrom){
        int dmg = Mathf.RoundToInt( ComputeDamage(strength01, isSlam:true) );
        ApplyScore(dmg, worldFrom);
    }

    // ---- 核心計算 ----
    float ComputeDamage(float strength01, bool isSlam)
    {
        float now = Time.time;
        RegisterHitTime(now);

        // 1) 基礎＋強度
        float strengthMul = 1f + Mathf.Clamp01(strength01); // 1~2倍
        float typeMul = isSlam ? slamMul : slashMul;

        // 2) Combo
        int tier = combo ? combo.Tier(10) : 0;
        float comboMul = 1f + comboTierBonus * Mathf.Clamp(tier, 0, 4);

        // 3) Tempo
        float hz = EstimateHz();
        float tempoMul = TempoMultiplier(hz);

        // 4) Stability
        float cv = EstimateCV(); // 變異係數（越低越穩）
        float stabilityMul = StabilityMultiplier(cv);

        // 5) Endurance（在帶內累積，離開帶內衰減）
        UpdateEndurance(hz);
        float fatigueMul = Mathf.Lerp(1f, enduranceMaxBoost, endurance);

        float dmg = baseDamage * strengthMul * typeMul * comboMul * tempoMul * stabilityMul * fatigueMul;
        return dmg;
    }

    void ApplyScore(int dmg, Vector3 worldFrom){
        total += dmg;
        if (scoreText)   scoreText.text = $"{total:000000}";
        if (lastHitText) lastHitText.GetComponent<LastHitFade>()?.Show($"+{dmg}");
        if (hitNumbers && cam) hitNumbers.Spawn(worldFrom, dmg, cam);
    }

    // ---- 節奏資料處理 ----
    void RegisterHitTime(float t)
    {
        hitTimes.Enqueue(t);
        // 移除超出窗口的資料
        while (hitTimes.Count > 0 && (t - hitTimes.Peek()) > windowSec)
            hitTimes.Dequeue();
    }

    float EstimateHz()
    {
        // 用窗口內相鄰命中間隔的平均 → 頻率
        if (hitTimes.Count < 2) return 0f;
        float prev = -1f, sum = 0f; int n = 0;
        foreach (var tt in hitTimes){
            if (prev >= 0f){ sum += (tt - prev); n++; }
            prev = tt;
        }
        if (n <= 0) return 0f;
        float meanInterval = Mathf.Max(0.0001f, sum / n);
        return 1f / meanInterval;
    }

    float EstimateCV()
    {
        // 變異係數 CV = std / mean（窗口內）
        if (hitTimes.Count < 3) return 1f; // 資料太少視為不穩
        float prev = -1f;
        var intervals = new List<float>(hitTimes.Count);
        foreach (var tt in hitTimes){
            if (prev >= 0f) intervals.Add(tt - prev);
            prev = tt;
        }
        if (intervals.Count < 2) return 1f;
        float mean = 0f; foreach (var x in intervals) mean += x; mean /= intervals.Count;
        float var  = 0f; foreach (var x in intervals) { float d = x - mean; var += d*d; }
        var /= (intervals.Count - 1);
        float std = Mathf.Sqrt(var);
        return Mathf.Clamp(std / Mathf.Max(0.0001f, mean), 0f, 1.5f);
    }

    float TempoMultiplier(float hz)
    {
        if (hz <= 0f) return 0.9f; // 剛開始或資料不足
        // 對「帶外」做平滑衰減；帶中央給 tempoMaxBoost
        float center = 0.5f * (targetHzMin + targetHzMax);
        float half   = 0.5f * Mathf.Max(0.01f, (targetHzMax - targetHzMin));
        float x = (hz - center) / half; // -1..1 附近是帶內
        float inBand = Mathf.Clamp01(1f - Mathf.Abs(x)); // 1 = 正中央, 0 = 帶外
        float minMul = 0.85f; // 帶外最低
        return Mathf.Lerp(minMul, tempoMaxBoost, inBand);
    }

    float StabilityMultiplier(float cv)
    {
        // CV <= good → 滿加成；CV >= bad → 幾乎沒加成；中間線性
        if (cv <= stabilityCvGood) return stabilityMaxBoost;
        if (cv >= stabilityCvBad)  return 1.0f;
        float t = Mathf.InverseLerp(stabilityCvBad, stabilityCvGood, cv); // 由大到小
        return Mathf.Lerp(1.0f, stabilityMaxBoost, t);
    }

    void UpdateEndurance(float hz)
    {
        bool inBand = (hz >= targetHzMin && hz <= targetHzMax);
        if (inBand)
        {
            endurance += Time.deltaTime / Mathf.Max(0.0001f, enduranceBuildSec);
        }
        else
        {
            endurance -= enduranceDecayPerSec * Time.deltaTime;
        }
        endurance = Mathf.Clamp01(endurance);
    }

    public int Total() => total;
}
