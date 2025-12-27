using UnityEngine;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 計分邏輯（力量．Combo．穩定度）
///  - 外部只要呼叫 AddSlash / AddSlam 並給 strength01（0~1）即可。
///  - 分數顯示、HitNumber、LastHit 都在這支裡一起處理。
///  - 修改：在遊戲結束 (GamePlayController.IsPlaying == false) 時，拒絕任何計分與音效。
/// </summary>
public class DamageCalculator : MonoBehaviour
{
    [Header("音效")]
    public AudioSource audioSource;   
    public AudioClip slashSfx;        
    public AudioClip slamSfx;         

    [Header("UI")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI lastHitText;
    public TextMeshProUGUI debugStrengthText;

    [Header("Hit Number (可選)")]
    public HitNumberManager hitNumbers;
    public Camera cam;

    [Header("Base & Move Type")]
    public float baseDamage = 1000f;
    public float slashMul = 1f;
    public float slamMul = 2.2f;

    [Header("力量 STR")]
    [Range(0f, 1f)] public float minStrength = 0.30f;
    public float strengthMaxMul = 1.6f;
    public float veryLowStrengthMul = 0.3f;

    [Header("Safety (防呆)")]
    public float minAttackInterval = 0.10f;   

    [Header("Combo")]
    public int comboStep = 5;
    public float comboTierBonus = 0.20f;
    public int maxComboTier = 4;
    public ComboCounter combo;     

    [Header("穩定度（節奏與平穩度）")]
    public float windowSec = 4f;
    public float targetHzMin = 1.3f;
    public float targetHzMax = 2.2f;
    public float cvGood       = 0.15f;
    public float cvBad        = 0.45f;
    public float stabilityMinMul = 0.85f;
    public float stabilityMaxMul = 1.30f;

    [HideInInspector] public float lastHz;           
    [HideInInspector] public float lastStability01;  

    [Header("Slash / Slam 階段切換")]
    public GameTimer timer;
    public float slamPhaseThreshold = 15f;

    [Header("傷害數值爆炸特效")]
    public GameScoreExplode scoreExploder;

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

    public void AddSlash(float strength01, Vector3 worldFrom)
    {
        HandleAttack(strength01, worldFrom, isSlamRequested: false);
    }

    public void AddSlam(float strength01, Vector3 worldFrom)
    {
        HandleAttack(strength01, worldFrom, isSlamRequested: true);
    }

    void HandleAttack(float strength01, Vector3 worldFrom, bool isSlamRequested)
    {
        // ★★★ 核心修正：如果遊戲已經結束 (Time Left = 0)，直接拒絕處理 ★★★
        // 這樣可以防止半空中的飛彈在 0 秒後撞到 Boss 產生聲音或分數
        if (!GamePlayController.IsPlaying)
        {
            // Debug.Log("[Damage] 遊戲已結束，無視此攻擊。");
            return;
        }

        // 0. 檢查時間段是否允許 (前半段不給 Slam，後半段不給 Slash)
        if (!IsAttackAllowed(isSlamRequested))
            return;

        if (debugStrengthText)
            debugStrengthText.text = $"STR {strength01:0.00}";

        bool isSlamActual = isSlamRequested;

        // 1. 更新 Combo
        if (combo != null)
        {
            combo.RegisterHit(isSlamActual, strength01);
        }

        // 2. 算傷害
        int dmg = Mathf.RoundToInt(ComputeDamage(strength01, isSlamActual));

        // 3. 播音效 (因為前面的 IsPlaying 擋住了，所以這裡絕對不會播聲)
        PlaySfx(isSlamActual ? slamSfx : slashSfx);

        // 4. 加進總分 & 顯示
        ApplyScore(dmg, worldFrom);
    }

    void PlaySfx(AudioClip clip)
    {
        if (audioSource == null || clip == null) return;
        audioSource.PlayOneShot(clip);
    }

    bool IsAttackAllowed(bool isSlamRequested)
    {
        if (timer == null) return true;

        float t = timer.TimeLeft;
        bool inSlamPhase = t <= slamPhaseThreshold;

        if (!inSlamPhase && isSlamRequested) return false;
        if (inSlamPhase && !isSlamRequested) return false;

        return true;
    }

    // ---------- 核心計算 ----------

    float ComputeDamage(float strength01, bool isSlam)
    {
        float now = Time.time;
        RegisterHitTime(now);

        float typeMul = isSlam ? slamMul : slashMul;
        float strengthMul = StrengthMultiplier(strength01);

        int tier = combo ? combo.Tier(comboStep) : 0;
        tier = Mathf.Clamp(tier, 0, maxComboTier);
        float comboMul = 1f + comboTierBonus * tier;

        float stabilityMul = StabilityMultiplier();

        return baseDamage * typeMul * strengthMul * comboMul * stabilityMul;
    }

    float StrengthMultiplier(float strength01)
    {
        strength01 = Mathf.Clamp01(strength01);
        if (strength01 < minStrength) return veryLowStrengthMul;
        float t = Mathf.InverseLerp(minStrength, 1f, strength01);
        return Mathf.Lerp(1f, strengthMaxMul, t);
    }

    float StabilityMultiplier()
    {
        if (hitTimes.Count < 3) return 1f;

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
        float freqScore = 1f;
        float mid = 0.5f * (targetHzMin + targetHzMax);
        float halfRange = 0.5f * (targetHzMax - targetHzMin);
        if (halfRange > 0f)
        {
            float dist = Mathf.Abs(hz - mid);
            if (dist > halfRange)
            {
                float extra = dist - halfRange;
                freqScore = 1f - Mathf.Clamp01(extra / halfRange);
            }
        }

        float overall = stableScore * (0.5f + 0.5f * freqScore);
        lastHz = hz;
        lastStability01 = overall;

        return Mathf.Lerp(stabilityMinMul, stabilityMaxMul, overall);
    }

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

        if (scoreExploder != null)
        {
            scoreExploder.ExplodeScore(dmg);
        }
    }

    public int Total() => total;   
}