using System.Collections;
using UnityEngine;

public class PhasePromptUI : MonoBehaviour
{
    [Header("Refs")]
    public GameTimer timer;                 // 拖你的 GameTimer
    public CanvasGroup readyGroup;          // READY 的 CanvasGroup
    public CanvasGroup goGroup;             // GO 的 CanvasGroup

    [Header("Trigger Time (TimeLeft seconds)")]
    public float readyAtTimeLeft = 17f;     // 剩 17 秒出現 READY
    public float goAtTimeLeft = 15f;        // 剩 15 秒出現 GO

    [Header("READY Blink")]
    public float blinkSpeed = 6f;           // 越大閃越快
    public float readyShowSeconds = 2.0f;   // READY 顯示多久（通常 17->15 = 2 秒）

    [Header("GO Pop (Base)")]
    public float goHoldSeconds = 0.8f;      // GO 停留多久
    public float goStartScale = 0.6f;       // 進場初始大小
    public float goEndScale = 1.0f;         // 最終穩定大小（通常 1）

    [Header("GO Jelly (Recommended)")]
    [Tooltip("第一段：彈到超過 1 的最大值，越大越Q")]
    public float goOvershootScale = 1.15f;

    [Tooltip("第二段：回縮到這個比例（略小於 1），越小越像果凍")]
    public float goSettleScale = 0.95f;

    [Tooltip("彈到 overshoot 的時間（越小越有『啪』一下的感覺）")]
    public float goOvershootTime = 0.12f;

    [Tooltip("回縮/穩定的時間（越小越有彈性）")]
    public float goSettleTime = 0.08f;

    bool readyPlayed = false;
    bool goPlayed = false;

    void Awake()
    {
        SetGroup(readyGroup, false);
        SetGroup(goGroup, false);

        if (timer == null) timer = FindObjectOfType<GameTimer>();
    }

    void Update()
    {
        if (timer == null) return;

        float t = timer.TimeLeft; // ✅ 用 TimeLeft，不會吃到倒數3秒

        // READY：剩 17 秒
        if (!readyPlayed && t <= readyAtTimeLeft)
        {
            readyPlayed = true;
            StartCoroutine(PlayReady());
        }

        // GO：剩 15 秒
        if (!goPlayed && t <= goAtTimeLeft)
        {
            goPlayed = true;
            StopAllCoroutines(); // 停掉 READY 的閃爍
            StartCoroutine(PlayGo());
        }
    }

    IEnumerator PlayReady()
    {
        SetGroup(readyGroup, true);

        float start = Time.time;
        while (Time.time - start < readyShowSeconds)
        {
            // 閃爍：alpha 在 0.2~1 之間來回
            float a = 0.2f + 0.8f * (0.5f + 0.5f * Mathf.Sin(Time.time * blinkSpeed));
            readyGroup.alpha = a;
            yield return null;
        }

        SetGroup(readyGroup, false);
    }

    IEnumerator PlayGo()
    {
        SetGroup(readyGroup, false);

        SetGroup(goGroup, true);
        goGroup.alpha = 1f;

        Transform goT = goGroup.transform;
        goT.localScale = Vector3.one * goStartScale;

        // ✅ 果凍三段回彈：
        // ① 快速彈大到 overshoot（>1）
        yield return ScaleTo(goT, goStartScale, goOvershootScale, goOvershootTime);

        // ② 回縮到 settle（<1）
        yield return ScaleTo(goT, goOvershootScale, goSettleScale, goSettleTime);

        // ③ 穩定到 end（通常 1）
        yield return ScaleTo(goT, goSettleScale, goEndScale, goSettleTime);

        goT.localScale = Vector3.one * goEndScale;

        yield return new WaitForSeconds(goHoldSeconds);

        SetGroup(goGroup, false);
    }

    IEnumerator ScaleTo(Transform t, float from, float to, float dur)
    {
        // 避免 dur=0 造成除以 0
        if (dur <= 0.0001f)
        {
            t.localScale = Vector3.one * to;
            yield break;
        }

        float time = 0f;
        while (time < dur)
        {
            time += Time.deltaTime;
            float u = Mathf.Clamp01(time / dur);

            // SmoothStep 比較「軟」，果凍感更好
            float eased = Mathf.SmoothStep(0f, 1f, u);

            float s = Mathf.Lerp(from, to, eased);
            t.localScale = Vector3.one * s;
            yield return null;
        }

        t.localScale = Vector3.one * to;
    }

    void SetGroup(CanvasGroup g, bool on)
    {
        if (g == null) return;
        g.alpha = on ? 1f : 0f;
        g.interactable = on;
        g.blocksRaycasts = on;
        g.gameObject.SetActive(on);
    }
}
