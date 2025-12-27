using System.Collections;
using UnityEngine;

/// <summary>
/// 畫面呼吸特效控制器
/// 負責：在遊戲開始 15 秒後開啟材質特效，並在 0 秒結束時強制關閉。
/// 修正：加入 OnDestroy/OnDisable 重置材質，以及在啟動前二次檢查遊戲狀態。
/// </summary>
public class BreathFXController : MonoBehaviour
{
    [Header("目標材質 (Full Screen Material)")]
    public Material effectMaterial;

    [Header("Shader 參數名稱")]
    public string freqProperty = "_BreathFreq";
    public string intensityProperty = "_BreathIntensity";

    [Header("數值設定")]
    public float freqOnValue = 2f;
    public float intensityOnValue = 0.7f;
    public float freqOffValue = 0f;
    public float intensityOffValue = 0f;

    [Header("時間設定")]
    [Tooltip("遊戲開始後幾秒開啟特效 (例如 15 秒)")]
    public float startDelay = 15f; 

    private Coroutine routine;

    void Awake()
    {
        // 腳本一喚醒就強制重設材質球，防止上一場的殘留值
        ResetMaterial();
    }

    void OnDisable()
    {
        // 當物件被隱藏或場景切換時，確保特效完全消失
        ResetMaterial();
    }

    void OnDestroy()
    {
        // 重要：當物件被銷毀時，將材質球屬性歸零 (避免影響其他場景)
        ResetMaterial();
    }

    /// <summary>
    /// 由 GamePlayController 在 StartMatch 時呼叫
    /// </summary>
    public void StartEffect()
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(DelayTurnOn());
    }

    /// <summary>
    /// 由 GamePlayController 在 OnTimerFinished 時呼叫
    /// </summary>
    public void StopEffect()
    {
        if (routine != null) StopCoroutine(routine);
        ResetMaterial();
    }

    IEnumerator DelayTurnOn()
    {
        // 1. 初始狀態確保是關閉的
        ResetMaterial();
        
        // 2. 等待設定的時間
        yield return new WaitForSeconds(startDelay);

        // ★ 核心檢查：如果等待期間遊戲已經結束 (IsPlaying 為 false)，則不執行開啟邏輯
        // 這能解決「玩家看排行榜時特效突然冒出來」的問題
        if (!GamePlayController.IsPlaying)
        {
            Debug.Log("[BreathFX] 檢測到遊戲已結束，取消開啟特效。");
            yield break;
        }

        // 3. 正式開啟特效
        SetValues(freqOnValue, intensityOnValue);
        Debug.Log("[BreathFX] 特效已啟動。");
    }

    private void ResetMaterial()
    {
        SetValues(freqOffValue, intensityOffValue);
    }

    void SetValues(float freq, float intensity)
    {
        if (effectMaterial == null) return;

        // 設定材質球參數
        effectMaterial.SetFloat(freqProperty, freq);
        effectMaterial.SetFloat(intensityProperty, intensity);
    }
}