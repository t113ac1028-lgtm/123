using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class PhaseTransitionSlowMo : MonoBehaviour
{
    [Header("觸發設定")]
    public GameTimer timer;
    public float slowMoStartTime = 16.5f;
    [Range(0.05f, 1f)] public float slowMoScale = 0.3f;

    [Header("音效設定 (BGM)")]
    [Tooltip("請把負責播放 BGM 的 AudioSource 拖進來")]
    public AudioSource bgmSource;
    [Tooltip("進入慢動作時，BGM 的音調/速度 (預設 0.6，數字越小越沉悶)")]
    [Range(0.1f, 1f)] public float slowMoPitch = 0.6f;

    [Header("過渡時間 (秒) - 使用真實時間")]
    public float enterTransitionTime = 0.5f; 
    public float hitStopDelay = 0.3f;        
    public float exitTransitionTime = 0.6f;  

    [Header("防呆機制")]
    public float maxRealTimeDuration = 4.0f;

    [Header("事件 (後續可接 UI 或特效)")]
    public UnityEvent OnSlowMoStart;
    public UnityEvent OnSlowMoEnd;

    private bool hasTriggered = false;
    private bool isCurrentlySlow = false;
    private Coroutine timeCoroutine;

    void Update()
    {
        if (!GamePlayController.IsPlaying) return;

        if (!hasTriggered && timer != null && timer.TimeLeft <= slowMoStartTime)
        {
            hasTriggered = true;
            isCurrentlySlow = true;
            OnSlowMoStart.Invoke();
            
            if (timeCoroutine != null) StopCoroutine(timeCoroutine);
            // 同步啟動畫面變慢與聲音變沉悶
            timeCoroutine = StartCoroutine(SmoothTimeScale(1.0f, slowMoScale, 1.0f, slowMoPitch, enterTransitionTime));
            
            StartCoroutine(AutoExitFailSafe());
        }
    }

    IEnumerator AutoExitFailSafe()
    {
        yield return new WaitForSecondsRealtime(maxRealTimeDuration);
        if (isCurrentlySlow)
        {
            ExitSlowMo();
        }
    }

    public void ExitSlowMoWithStrength(float strength)
    {
        if (!isCurrentlySlow) return;
        isCurrentlySlow = false;
        
        if (timeCoroutine != null) StopCoroutine(timeCoroutine);
        timeCoroutine = StartCoroutine(HitStopAndRecoverRoutine());
    }

    public void ExitSlowMo()
    {
        if (!isCurrentlySlow) return;
        isCurrentlySlow = false;

        float currentPitch = bgmSource != null ? bgmSource.pitch : 1.0f;
        if (timeCoroutine != null) StopCoroutine(timeCoroutine);
        timeCoroutine = StartCoroutine(SmoothTimeScale(Time.timeScale, 1.0f, currentPitch, 1.0f, exitTransitionTime));
    }

    IEnumerator HitStopAndRecoverRoutine()
    {
        Time.timeScale = 0.05f; 
        Time.fixedDeltaTime = Mathf.Clamp(0.02f * Time.timeScale, 0.005f, 0.02f);
        
        // ★ 打擊瞬間：讓 BGM 的音調降到極低 (0.3)，配合畫面的卡肉感
        if (bgmSource != null) bgmSource.pitch = 0.3f;
        
        yield return new WaitForSecondsRealtime(hitStopDelay);

        // 漸漸恢復正常速度與正常音調 (1.0)
        yield return StartCoroutine(SmoothTimeScale(Time.timeScale, 1.0f, 0.3f, 1.0f, exitTransitionTime));
    }

    IEnumerator SmoothTimeScale(float startScale, float targetScale, float startPitch, float targetPitch, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = t * t * (3f - 2f * t); 
            
            Time.timeScale = Mathf.Lerp(startScale, targetScale, t);
            Time.fixedDeltaTime = Mathf.Clamp(0.02f * Time.timeScale, 0.005f, 0.02f);
            
            // ★ 同步平滑調整 BGM 的音調
            if (bgmSource != null)
            {
                bgmSource.pitch = Mathf.Lerp(startPitch, targetPitch, t);
            }
            
            yield return null;
        }
        
        Time.timeScale = targetScale;
        Time.fixedDeltaTime = Mathf.Clamp(0.02f * Time.timeScale, 0.005f, 0.02f);
        
        if (bgmSource != null)
        {
            bgmSource.pitch = targetPitch;
        }
        
        if (targetScale >= 1.0f)
        {
            OnSlowMoEnd.Invoke();
        }
    }
}