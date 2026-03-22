using UnityEngine;
using UnityEngine.VFX;
using System.Collections;

public class BossShieldController : MonoBehaviour
{
    [Header("護盾物件")]
    public GameObject shieldObject;
    public Collider shieldCollider;
    public VisualEffect shieldVFX;

    [Header("破盾特效（可選）")]
    public GameObject shieldBreakEffectPrefab;

    [Header("受擊變色設定")]
    public int maxHitsForFullRed = 20;
    public int currentHits = 0;

    [Header("時間設定")]
    public float lifeTime = 17f;
    public float fadeDuration = 1f;

    [Header("VFX 參數名稱")]
    public string damageRatioProperty = "DamageRatio";
    public string shieldAlphaProperty = "ShieldAlpha";

    [Header("狀態")]
    public bool shieldBroken = false;

    private float timer = 0f;
    private bool isFading = false;

    private void Start()
    {
        UpdateShieldVisual();

        if (shieldVFX != null)
        {
            shieldVFX.SetFloat(shieldAlphaProperty, 1f);
        }
    }

    private void Update()
    {
        if (shieldBroken || isFading) return;

        timer += Time.deltaTime;

        if (timer >= lifeTime)
        {
            StartCoroutine(FadeAndBreakShield());
        }
    }

    public void TakeHit(int damage = 1)
    {
        if (shieldBroken || isFading) return;

        currentHits += damage;
        currentHits = Mathf.Clamp(currentHits, 0, maxHitsForFullRed);

        UpdateShieldVisual();
    }

    private void UpdateShieldVisual()
    {
        float damageRatio = 0f;

        if (maxHitsForFullRed > 0)
            damageRatio = (float)currentHits / maxHitsForFullRed;

        damageRatio = Mathf.Clamp01(damageRatio);

        if (shieldVFX != null)
        {
            shieldVFX.SetFloat(damageRatioProperty, damageRatio);
        }
    }

    private IEnumerator FadeAndBreakShield()
    {
        isFading = true;

        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;
            float alpha = Mathf.Lerp(1f, 0f, t);

            if (shieldVFX != null)
            {
                shieldVFX.SetFloat(shieldAlphaProperty, alpha);
            }

            yield return null;
        }

        BreakShield();
    }

    public void BreakShield()
    {
        if (shieldBroken) return;

        shieldBroken = true;

        if (shieldCollider != null)
            shieldCollider.enabled = false;

        if (shieldBreakEffectPrefab != null)
        {
            Vector3 spawnPos = shieldObject != null ? shieldObject.transform.position : transform.position;
            Instantiate(shieldBreakEffectPrefab, spawnPos, Quaternion.identity);
        }

        if (shieldObject != null)
            shieldObject.SetActive(false);
    }

    public void ResetShield()
    {
        StopAllCoroutines();

        shieldBroken = false;
        isFading = false;
        currentHits = 0;
        timer = 0f;

        if (shieldCollider != null)
            shieldCollider.enabled = true;

        if (shieldObject != null)
            shieldObject.SetActive(true);

        if (shieldVFX != null)
        {
            shieldVFX.SetFloat(shieldAlphaProperty, 1f);
        }

        UpdateShieldVisual();
    }
}