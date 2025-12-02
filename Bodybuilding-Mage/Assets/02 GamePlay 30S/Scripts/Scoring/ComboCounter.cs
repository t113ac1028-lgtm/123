using UnityEngine;
using TMPro;
using System.Collections;

public class ComboCounter : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI comboText;             // 連到畫面上的 COMBO 文字
    public Color baseColor = Color.white;
    public Color hitFlashColor = new Color(1f, 0.23f, 0.19f); // #FF3B30

    [Header("Timing")]
    [Tooltip("從最後一次命中起算，若超過這個秒數沒有新命中，Combo 直接歸 0。")]
    public float resetWindowSec = 1.2f;           // 取代原本的逐步遞減：改為逾時直接歸零

    [Header("Scoring")]
    public int   slamBonus   = 1;                 // slam 額外 +1（單手 +1、slam 再 +slamBonus）
    public float punchScaleSlash = 1.15f;
    public float punchScaleSlam  = 1.28f;
    public float punchTime       = 0.14f;

    int combo;
    int maxCombo;
    float lastHitTime;

    void Start()
    {
        UpdateText();
    }

    void Update()
    {
        // 逾時直接歸零（不再逐步 -1）
        if (combo > 0 && Time.time - lastHitTime > resetWindowSec)
        {
            combo = 0;
            UpdateText();
        }
    }

    /// <summary>
    /// 一般命中：由外部帶入是否為 slam 與強度（強度目前僅用於演出）
    /// </summary>
    public void RegisterHit(bool isSlam, float strength01)
    {
        combo += 1 + (isSlam ? slamBonus : 0);
        maxCombo = Mathf.Max(maxCombo, combo);
        lastHitTime = Time.time;
        UpdateText();
        Punch(isSlam);
    }

    /// <summary>
    /// 統一的「+1 命中」介面，給飛彈命中或一般 slash 使用
    /// </summary>
    public void AddHit()
    {
        RegisterHit(false, 1f); // 視為一般揮擊
    }

    /// <summary>
    /// 清除 Combo（如需手動歸零）
    /// </summary>
    public void Clear()
    {
        combo = 0;
        UpdateText();
    }

    public int Current => combo;
    public int Max => maxCombo;

    /// <summary>
    /// 依照 step（預設 10）算出 Combo 階級。
    /// 之後 DamageCalculator 會用 Tier(5) 來算 5 hit 一階。
    /// </summary>
    public int Tier(int step = 10) => Mathf.FloorToInt(combo / (float)step);

    void UpdateText()
    {
        if (!comboText) return;
        comboText.text = $"COMBO  x  {combo}";
    }

    void Punch(bool isSlam)
    {
        if (!comboText) return;
        if (!gameObject.activeInHierarchy) return;
        StartCoroutine(PunchCR(isSlam));
    }

    IEnumerator PunchCR(bool isSlam)
    {
        var t = comboText.rectTransform;
        var col = comboText.color;
        float target = isSlam ? punchScaleSlam : punchScaleSlash;
        float half = punchTime * 0.5f;
        float timer = 0f;

        // 進
        comboText.color = hitFlashColor;
        while (timer < half)
        {
            timer += Time.deltaTime;
            float k = timer / half;
            float s = Mathf.Lerp(1f, target, k * k * (3 - 2 * k));
            t.localScale = Vector3.one * s;
            yield return null;
        }
        // 出
        timer = 0f;
        while (timer < half)
        {
            timer += Time.deltaTime;
            float k = timer / half;
            float s = Mathf.Lerp(target, 1f, k * k * (3 - 2 * k));
            t.localScale = Vector3.one * s;
            comboText.color = Color.Lerp(hitFlashColor, baseColor, k);
            yield return null;
        }
        comboText.color = baseColor;
        t.localScale = Vector3.one;
    }
}
