using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 負責 VS 對決畫面的進場演出。
/// 修正版：徹底解決「跳兩次」問題。包含腳本衝突自動偵測、重複觸發鎖定。
/// </summary>
public class VSIntroductionEffect : MonoBehaviour
{
    [Header("自動化設定")]
    public bool playOnEnable = true;

    [Header("UI 參考 (RectTransform)")]
    public RectTransform topPanel;    // 上方圖片
    public RectTransform bottomPanel; // 下方圖片
    public RectTransform vsImage;     // 中間的 VS 圖片

    [Header("上方圖片座標設定")]
    public Vector2 topStartPos = new Vector2(0, 1000);   
    public Vector2 topTargetPos = new Vector2(0, 250);   

    [Header("下方圖片座標設定")]
    public Vector2 bottomStartPos = new Vector2(0, -1000); 
    public Vector2 bottomTargetPos = new Vector2(0, -250); 

    [Header("視覺效果設定")]
    public bool useAlphaFade = true;

    [Header("時間與節奏")]
    public float slideDuration = 0.5f;   
    public float vsSlamDelay = 0.2f;    
    public float vsSlamDuration = 0.12f; // 砸下來的速度要極快才有力量
    [Tooltip("VS 剛出現時的大小倍率")]
    public float vsStartScale = 5.0f;

    [Header("音效與震動")]
    public AudioSource audioSource;
    public AudioClip slideSfx;      
    public AudioClip vsSlamSfx;     
    public float shakeIntensity = 35f; // 落地時的震動強度
    public float shakeTime = 0.25f;

    [Header("動畫曲線")]
    [Tooltip("滑入曲線")]
    public AnimationCurve slideCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 1f));
    
    [Tooltip("VS 砸地曲線：請確保是從 0 到 1 的直線，不要有波浪")]
    public AnimationCurve vsSlamCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 1f));

    private CanvasGroup _mainCG;
    private CanvasGroup _vsCG;
    private bool _isAnimating = false;
    private float _lastEnableTime;
    private Coroutine _mainSequence;

    private void Awake()
    {
        _mainCG = GetComponent<CanvasGroup>();
        if (vsImage != null)
        {
            _vsCG = vsImage.GetComponent<CanvasGroup>();
            if (_vsCG == null) _vsCG = vsImage.gameObject.AddComponent<CanvasGroup>();
            
            // ★ 核心檢查：如果 VS 圖片上有掛其他的循環彈跳腳本，必須關掉，否則會跳兩次
            var otherBounce = vsImage.GetComponent("PopUpBounceLoop");
            if (otherBounce != null) 
            {
                (otherBounce as MonoBehaviour).enabled = false;
                Debug.Log("[VS Effect] 偵測到衝突腳本 PopUpBounceLoop，已自動停用以防止跳動兩次。");
            }
        }
    }

    private void OnEnable()
    {
        // 增加時間閘門，防止 ComicSequence 翻頁過快重複觸發
        if (Time.time - _lastEnableTime < 0.1f) return;
        _lastEnableTime = Time.time;

        ResetToStart();

        if (playOnEnable)
        {
            PlayVSEffect();
        }
    }

    public void ResetToStart()
    {
        _isAnimating = false;
        if (_mainSequence != null) StopCoroutine(_mainSequence);
        StopAllCoroutines();

        if (topPanel != null) topPanel.anchoredPosition = topStartPos;
        if (bottomPanel != null) bottomPanel.anchoredPosition = bottomStartPos;
        
        if (vsImage != null)
        {
            vsImage.localScale = Vector3.one * vsStartScale;
            if (_vsCG != null) _vsCG.alpha = 0f; 
        }
        
        if (_mainCG != null && useAlphaFade) _mainCG.alpha = 0f;
        transform.localPosition = Vector3.zero;
    }

    [ContextMenu("測試：開始 VS 演出")]
    public void PlayVSEffect()
    {
        // 如果已經在播了，先重置再播
        if (_isAnimating) ResetToStart();
        
        _mainSequence = StartCoroutine(VSSequence());
    }

    private IEnumerator VSSequence()
    {
        _isAnimating = true;

        float safeSlideDuration = Mathf.Max(0.01f, slideDuration);
        float safeSlamDuration = Mathf.Max(0.01f, vsSlamDuration);

        // --- Phase 1: 圖片滑入 ---
        if (audioSource != null && slideSfx != null) 
            audioSource.PlayOneShot(slideSfx);
        
        float t = 0;
        while (t < 1.0f)
        {
            t += Time.deltaTime / safeSlideDuration;
            float p = (slideCurve != null) ? slideCurve.Evaluate(t) : t;
            
            if (topPanel) topPanel.anchoredPosition = Vector2.Lerp(topStartPos, topTargetPos, p);
            if (bottomPanel) bottomPanel.anchoredPosition = Vector2.Lerp(bottomStartPos, bottomTargetPos, p);
            if (_mainCG != null && useAlphaFade) _mainCG.alpha = Mathf.Clamp01(p * 1.5f);

            yield return null;
        }

        if (topPanel) topPanel.anchoredPosition = topTargetPos;
        if (bottomPanel) bottomPanel.anchoredPosition = bottomTargetPos;

        yield return new WaitForSeconds(vsSlamDelay);

        // --- Phase 2: VS 圖片重砸 (只會跑一次) ---
        if (vsImage != null)
        {
            if (_vsCG != null) _vsCG.alpha = 1f;

            t = 0;
            while (t < 1.0f)
            {
                t += Time.deltaTime / safeSlamDuration;
                float progress = (vsSlamCurve != null) ? vsSlamCurve.Evaluate(Mathf.Clamp01(t)) : t;
                
                // 強制 Lerp：從 StartScale 到 1.0，絕不回彈
                float currentScale = Mathf.Lerp(vsStartScale, 1.0f, progress);
                vsImage.localScale = Vector3.one * currentScale;

                yield return null;
            }
            
            vsImage.localScale = Vector3.one;

            if (audioSource != null && vsSlamSfx != null) 
                audioSource.PlayOneShot(vsSlamSfx);

            // 啟動震動
            StartCoroutine(ShakeScreen());
        }

        _isAnimating = false;
        _mainSequence = null;
    }

    private IEnumerator ShakeScreen()
    {
        Vector3 originPos = Vector3.zero; 
        float elapsed = 0f;
        float safeShakeTime = Mathf.Max(0.01f, shakeTime);

        while (elapsed < safeShakeTime)
        {
            elapsed += Time.deltaTime;
            float decay = 1.0f - (elapsed / safeShakeTime);
            
            float x = UnityEngine.Random.Range(-1f, 1f) * shakeIntensity * decay;
            float y = UnityEngine.Random.Range(-1f, 1f) * shakeIntensity * decay;
            
            transform.localPosition = originPos + new Vector3(x, y, 0);
            yield return null;
        }
        transform.localPosition = originPos;
    }
}