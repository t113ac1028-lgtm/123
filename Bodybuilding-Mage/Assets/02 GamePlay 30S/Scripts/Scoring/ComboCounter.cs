using UnityEngine;
using TMPro;
using System.Collections;

public class ComboCounter : MonoBehaviour
{
    [Header("UI")]
    [Tooltip("整個 Combo UI 根節點（圖 + 數字），用來控制顯示/隱藏 & 縮放")]
    public GameObject comboRoot;

    [Tooltip("只顯示數字的 TMP 文字元件")]
    public TextMeshProUGUI comboNumberText;

    [Tooltip("要做 Q 彈縮放動畫的目標，通常設成 ComboRoot 的 RectTransform")]
    public RectTransform punchTarget;

    public Color baseColor     = Color.white;
    public Color hitFlashColor = new Color(1f, 0.23f, 0.19f); // #FF3B30

    [Header("Timing")]
    [Tooltip("從最後一次命中起算，若超過這個秒數沒有新命中，Combo 直接歸 0 並隱藏")]
    public float resetWindowSec = 1.2f;

    [Header("Scoring")]
    [Tooltip("slam 額外 +1（單手 +1、slam 再 +slamBonus）")]
    public int   slamBonus       = 1;
    [Tooltip("一般 slash 命中時放大的倍率")]
    public float punchScaleSlash = 1.2f;
    [Tooltip("slam 命中時放大的倍率")]
    public float punchScaleSlam  = 1.35f;
    [Tooltip("整個跳動動畫總時間（秒）")]
    public float punchTime       = 0.18f;

    int combo;
    int maxCombo;
    float lastHitTime;

    // ===== 給外部讀取用的屬性 =====
    public int Current  => combo;
    public int Max      => maxCombo;   // GamePlayController 用這個
    public int MaxCombo => maxCombo;   // 你之後要用這個名字也OK

    void Start()
    {
        combo = 0;
        UpdateText(); // 會順便隱藏 UI
    }

    void Update()
    {
        // 逾時直接歸零
        if (combo > 0 && Time.time - lastHitTime > resetWindowSec)
        {
            combo = 0;
            UpdateText();
        }
    }

    /// <summary>一般命中（由 DamageCalculator 呼叫）：帶入是否為 slam 與強度</summary>
    public void RegisterHit(bool isSlam, float strength01)
    {
        combo += 1 + (isSlam ? slamBonus : 0);
        maxCombo = Mathf.Max(maxCombo, combo);
        lastHitTime = Time.time;

        UpdateText();
        Punch(isSlam);
    }

    /// <summary>若有地方只想單純 +1，也可以用這個</summary>
    public void AddHit()
    {
        RegisterHit(false, 1f);
    }

    /// <summary>手動清除 Combo</summary>
    public void Clear()
    {
        combo = 0;
        UpdateText();
    }

    /// <summary>依照 step（預設 10）算出 Combo 階級。</summary>
    public int Tier(int step = 10) => Mathf.FloorToInt(combo / (float)step);

    void UpdateText()
    {
        // 沒指定就不動
        if (comboNumberText == null && comboRoot == null)
            return;

        if (combo <= 0)
        {
            // 0 combo：整組 UI 不顯示
            if (comboRoot != null)
                comboRoot.SetActive(false);
            else if (comboNumberText != null)
                comboNumberText.gameObject.SetActive(false);

            // 文字還是塞個 0，雖然看不到
            if (comboNumberText != null)
                comboNumberText.text = "0";
        }
        else
        {
            // 有 combo：整組顯示，數字改成 combo
            if (comboRoot != null && !comboRoot.activeSelf)
                comboRoot.SetActive(true);
            else if (comboRoot == null && comboNumberText != null && !comboNumberText.gameObject.activeSelf)
                comboNumberText.gameObject.SetActive(true);

            if (comboNumberText != null)
                comboNumberText.text = combo.ToString();   // ★ 只顯示數字
        }
    }

    void Punch(bool isSlam)
    {
        if (!gameObject.activeInHierarchy)
            return;

        // 沒指定 punchTarget 的話，優先用 comboRoot，其次用文字本身
        if (punchTarget == null)
        {
            if (comboRoot != null)
                punchTarget = comboRoot.GetComponent<RectTransform>();
            else if (comboNumberText != null)
                punchTarget = comboNumberText.rectTransform;
        }

        if (punchTarget == null || comboNumberText == null)
            return;

        StartCoroutine(PunchCR(isSlam));
    }

    IEnumerator PunchCR(bool isSlam)
    {
        RectTransform t = punchTarget;
        Color originalColor = comboNumberText.color;

        float peakScale       = isSlam ? punchScaleSlam : punchScaleSlash;
        float undershootScale = 0.9f;                 // 回彈時比 1 小一點，增加「Q」感
        float duration        = Mathf.Max(0.01f, punchTime);

        float timer = 0f;
        comboNumberText.color = hitFlashColor;        // 先閃一下

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float u = Mathf.Clamp01(timer / duration);

            // 三段式彈跳：1 -> peak -> undershoot -> 1
            float s;
            if (u < 0.25f)
            {
                // 快速衝到 peak
                float k = u / 0.25f;
                s = Mathf.Lerp(1f, peakScale, k * k);
            }
            else if (u < 0.7f)
            {
                // 從 peak 掉到略小於 1
                float k = (u - 0.25f) / 0.45f;
                s = Mathf.Lerp(peakScale, undershootScale, k);
            }
            else
            {
                // 從 undershoot 再彈回 1
                float k = (u - 0.7f) / 0.3f;
                k = Mathf.Clamp01(k);
                s = Mathf.Lerp(undershootScale, 1f, k * k * (3 - 2 * k));
            }

            t.localScale = Vector3.one * s;

            // 顏色在後半段漸回原色
            float colorT = Mathf.InverseLerp(0.3f, 1f, u);
            comboNumberText.color = Color.Lerp(hitFlashColor, originalColor, colorT);

            yield return null;
        }

        comboNumberText.color = originalColor;
        t.localScale = Vector3.one;
    }
}
