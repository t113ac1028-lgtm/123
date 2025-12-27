using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class BossAttackAction
{
    public string actionName; // 招式名稱 (筆記用)
    
    [Header("動作設定")]
    [Tooltip("Animator 裡的 Trigger 參數名稱")]
    public string animatorTrigger;
    
    [Header("視覺與音效")]
    public GameObject vfxPrefab;
    public AudioClip sfx;
    
    [Tooltip("音效延遲幾秒後播放？(用來對準動作打擊點)")]
    public float sfxDelay = 0f; // ★ 新增功能
}

public class BossHitControl : MonoBehaviour
{
    [Header("組件參考")]
    public Animator anim;
    public AudioSource audioSource;

    [Header("1. 常規受擊設定")]
    public int maxTwitchCount = 3;     
    public float protectTime = 1.0f;   
    public int hitAnimCount = 1;       

    [Header("2. 階段切換時間 (倒數秒數)")]
    public float preChargeTime = 18f;
    public float chargeStartTime = 17f;
    public float burstStartTime = 15f;
    public float phase2StartTime = 14f; // 二階瘋狂攻擊開始時間

    [Header("3. 偽隨機攻擊設定")]
    [Tooltip("第一階段 (30-18s) 攻擊間隔 (秒)")]
    public Vector2 phase1Interval = new Vector2(4f, 7f);
    [Tooltip("第二階段 (14-1s) 攻擊間隔 (秒)")]
    public Vector2 phase2Interval = new Vector2(2f, 4f);
    [Tooltip("招式與招式之間的「最小」安全空隙 (秒)")]
    public float minAttackGap = 1.0f;
    [Tooltip("招式池：Boss 會從這裡面隨機選招式")]
    public List<BossAttackAction> attackPool = new List<BossAttackAction>();

    [Header("4. 蓄力節奏與速度")]
    [Range(0.1f, 1.0f)]
    public float chargeAnimSpeed = 0.2f;
    public float chargeSlowDuration = 1.5f;

    [Header("5. 燈光漸強設定")]
    public Light bossAuraLight;
    public float targetIntensity = 120f;

    [Header("6. 常規特效與音效")]
    public GameObject preChargeVFXPrefab;
    public GameObject burstVFXPrefab;
    public Transform vfxSpawnPoint;
    public float vfxDestroyDelay = 7.0f; 
    [Space]
    public AudioClip chargeSFX;
    public AudioClip burstSFX;

    // 內部狀態
    private int currentHitCount = 0;
    private bool isProtected = false;
    private bool isImmuneToHit = false;  
    private bool hasTriggeredPreCharge = false;
    private bool hasStartedChargeAnim = false;
    private bool hasTriggeredBurst = false;
    private bool isLightFading = false;
    private float chargingTimer = 0f;
    
    private float nextAttackTriggerTime = -1f; 
    private int lastAttackIndex = -1;          
    private GameTimer gameTimer;

    private void Awake()
    {
        if (anim == null) anim = GetComponent<Animator>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (vfxSpawnPoint == null) vfxSpawnPoint = this.transform;
        gameTimer = FindObjectOfType<GameTimer>();
        if (bossAuraLight != null) bossAuraLight.intensity = 0f;
    }

    private void Update()
    {
        if (isLightFading) return; 
        if (!Countdown.gameStarted || gameTimer == null) return;

        float timeLeft = gameTimer.TimeLeft;

        // --- A. 處理偽隨機攻擊邏輯 ---
        HandleRandomAttacks(timeLeft);

        // --- B. 原有的固定轉場邏輯 ---
        if (timeLeft <= preChargeTime && !hasTriggeredPreCharge) TriggerPreCharge();

        if (timeLeft <= chargeStartTime && timeLeft > burstStartTime && !hasTriggeredBurst)
        {
            float totalDuration = chargeStartTime - burstStartTime;
            float elapsed = chargeStartTime - timeLeft; 
            float progress = Mathf.Clamp01(elapsed / totalDuration);
            if (bossAuraLight != null) bossAuraLight.intensity = progress * targetIntensity;
        }

        if (timeLeft <= chargeStartTime && !hasStartedChargeAnim) StartChargeAnim();

        if (hasStartedChargeAnim && !hasTriggeredBurst)
        {
            chargingTimer += Time.deltaTime;
            if (chargingTimer >= chargeSlowDuration && anim.speed != 1.0f) anim.speed = 1.0f; 
        }

        if (timeLeft <= burstStartTime && !hasTriggeredBurst) TriggerBurst();
    }

    private void HandleRandomAttacks(float timeLeft)
    {
        if ((timeLeft <= preChargeTime && timeLeft >= burstStartTime) || timeLeft <= 1.0f) return;

        bool isAnimatingAttack = anim.GetCurrentAnimatorStateInfo(0).IsTag("Attack") || 
                                 (anim.IsInTransition(0) && anim.GetNextAnimatorStateInfo(0).IsTag("Attack"));

        if (isAnimatingAttack)
        {
            nextAttackTriggerTime = timeLeft - minAttackGap;
            return;
        }

        if (nextAttackTriggerTime < 0) SetNextAttackTime(timeLeft);

        if (timeLeft <= nextAttackTriggerTime)
        {
            DoRandomAttack();
            SetNextAttackTime(timeLeft);
        }
    }

    private void SetNextAttackTime(float currentTime)
    {
        float interval = 0f;
        if (currentTime > preChargeTime)
            interval = Random.Range(phase1Interval.x, phase1Interval.y);
        else if (currentTime < phase2StartTime)
            interval = Random.Range(phase2Interval.x, phase2Interval.y);
        else
            interval = 2.0f; 

        nextAttackTriggerTime = currentTime - Mathf.Max(interval, minAttackGap);
    }

    private void DoRandomAttack()
    {
        if (attackPool == null || attackPool.Count == 0) return;

        int randomIndex = lastAttackIndex;
        int safety = 0;
        while (randomIndex == lastAttackIndex && safety < 10)
        {
            randomIndex = Random.Range(0, attackPool.Count);
            safety++;
        }
        lastAttackIndex = randomIndex;

        BossAttackAction action = attackPool[randomIndex];

        // 1. 動畫觸發
        if (anim != null && !string.IsNullOrEmpty(action.animatorTrigger))
            anim.SetTrigger(action.animatorTrigger);

        // 2. 特效生成 (目前維持即時生成，若有需要也可以加延遲)
        if (action.vfxPrefab != null)
        {
            GameObject vfx = Instantiate(action.vfxPrefab, vfxSpawnPoint.position, vfxSpawnPoint.rotation);
            Destroy(vfx, vfxDestroyDelay);
        }

        // 3. 音效播放 (支援延遲)
        if (audioSource != null && action.sfx != null)
        {
            if (action.sfxDelay > 0)
                StartCoroutine(PlaySFXDelayed(action.sfx, action.sfxDelay));
            else
                audioSource.PlayOneShot(action.sfx);
        }
        
        Debug.Log($"[BossAI] 發動招式: {action.actionName} (音效延遲: {action.sfxDelay}s)");
    }

    // ★ 新增：延遲播放音效的協程
    private IEnumerator PlaySFXDelayed(AudioClip clip, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    public void TryHit()
    {
        if (!this.enabled || isProtected || isImmuneToHit) return;

        var stateInfo = anim.GetCurrentAnimatorStateInfo(0);
        var nextStateInfo = anim.GetNextAnimatorStateInfo(0);
        bool isCurrentAttack = stateInfo.IsTag("Attack");
        bool isNextAttack = anim.IsInTransition(0) && nextStateInfo.IsTag("Attack");

        if (isCurrentAttack || isNextAttack) return;

        if (hitAnimCount > 1)
            anim.SetInteger("HitIndex", Random.Range(0, hitAnimCount));

        anim.SetTrigger("Hit");
        currentHitCount++;

        if (currentHitCount >= maxTwitchCount)
            StartCoroutine(ProtectRoutine());
    }

    public void TurnOffLight(float fadeDuration = 1.0f)
    {
        isLightFading = true; 
        StopAllCoroutines(); 
        StartCoroutine(FadeOutLightRoutine(fadeDuration));
    }

    private IEnumerator FadeOutLightRoutine(float duration)
    {
        if (bossAuraLight == null) yield break;
        float startIntensity = bossAuraLight.intensity;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime; 
            bossAuraLight.intensity = Mathf.Lerp(startIntensity, 0f, elapsed / duration);
            yield return null;
        }
        bossAuraLight.intensity = 0f;
    }

    private void TriggerPreCharge()
    {
        hasTriggeredPreCharge = true;
        isImmuneToHit = true; 
        if (preChargeVFXPrefab != null)
            Destroy(Instantiate(preChargeVFXPrefab, vfxSpawnPoint.position, vfxSpawnPoint.rotation), vfxDestroyDelay);
    }

    private void StartChargeAnim()
    {
        hasStartedChargeAnim = true;
        chargingTimer = 0f;
        if (anim != null) { anim.SetTrigger("Charge"); anim.speed = chargeAnimSpeed; }
        if (audioSource != null && chargeSFX != null) audioSource.PlayOneShot(chargeSFX);
    }

    private void TriggerBurst()
    {
        hasTriggeredBurst = true;
        isImmuneToHit = false; 
        if (anim != null) anim.speed = 1.0f;
        if (bossAuraLight != null) bossAuraLight.intensity = targetIntensity;
        if (burstVFXPrefab != null)
            Destroy(Instantiate(burstVFXPrefab, vfxSpawnPoint.position, vfxSpawnPoint.rotation), vfxDestroyDelay);
        if (audioSource != null && burstSFX != null) audioSource.PlayOneShot(burstSFX);
    }

    IEnumerator ProtectRoutine()
    {
        isProtected = true;
        currentHitCount = 0;
        yield return new WaitForSeconds(protectTime);
        isProtected = false;
    }
}