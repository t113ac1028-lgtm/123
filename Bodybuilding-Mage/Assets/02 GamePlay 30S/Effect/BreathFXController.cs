using System.Collections;
using UnityEngine;

public class BreathFXController : MonoBehaviour
{
    [Header("目標材質 (Full Screen Material)")]
    public Material effectMaterial;

    [Header("Shader 參數名稱 (跟 Shader Graph Reference 一樣)")]
    public string freqProperty = "_BreathFreq";       // 改成你的 Reference
    public string intensityProperty = "_BreathIntensity";

    [Header("數值設定")]
    public float freqOnValue = 2f;
    public float intensityOnValue = 0.7f;
    public float freqOffValue = 0f;
    public float intensityOffValue = 0f;

    [Header("時間設定")]
    public float startDelay = 15f;  // 遊戲開始後幾秒開特效

    Coroutine routine;

    void Start()
    {
        // 一開始完全關閉
        SetValues(freqOffValue, intensityOffValue);
    }

    // 遊戲開始時呼叫
    public void StartEffect()
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(DelayTurnOn());
    }

    // 遊戲結束時呼叫
    public void StopEffect()
    {
        if (routine != null) StopCoroutine(routine);
        // 直接關掉，不淡出，避免閃爍
        SetValues(freqOffValue, intensityOffValue);
    }

    IEnumerator DelayTurnOn()
    {
        // 先關著等時間
        SetValues(freqOffValue, intensityOffValue);
        yield return new WaitForSeconds(startDelay);

        // 到時間後直接切到目標值
        SetValues(freqOnValue, intensityOnValue);
    }

    void SetValues(float freq, float intensity)
    {
        if (effectMaterial == null) return;

        effectMaterial.SetFloat(freqProperty, freq);
        effectMaterial.SetFloat(intensityProperty, intensity);
    }
}
