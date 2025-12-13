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

    [Header("GO Pop")]
    public float goPopDuration = 0.25f;     // 彈出時間
    public float goHoldSeconds = 0.8f;      // GO 停留多久
    public float goStartScale = 0.6f;
    public float goEndScale = 1.0f;

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

        // pop scale
        Transform goT = goGroup.transform;
        goT.localScale = Vector3.one * goStartScale;

        float t = 0f;
        while (t < goPopDuration)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / goPopDuration);
            // easeOutBack 小彈一下
            float eased = EaseOutBack(u);
            float s = Mathf.Lerp(goStartScale, goEndScale, eased);
            goT.localScale = Vector3.one * s;
            yield return null;
        }

        goT.localScale = Vector3.one * goEndScale;

        yield return new WaitForSeconds(goHoldSeconds);

        SetGroup(goGroup, false);
    }

    static float EaseOutBack(float x)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return 1 + c3 * Mathf.Pow(x - 1, 3) + c1 * Mathf.Pow(x - 1, 2);
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
