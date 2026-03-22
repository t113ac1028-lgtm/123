using UnityEngine;
using UnityEngine.VFX;

public class ShieldHitVFX : MonoBehaviour
{
    [Header("Ripple 特效 Prefab")]
    public GameObject ripplePrefab;

    [Header("特效存活時間")]
    public float rippleLifeTime = 0.4f;

    [Header("護盾控制器（可選）")]
    public BossShieldController shieldController;

    [Header("每次命中造成的護盾傷害")]
    public int damagePerHit = 1;

    [Header("防止過度連刷（可選）")]
    public bool useHitCooldown = false;
    public float hitCooldown = 0.05f;

    [Header("命中音效（可選）")]
    public AudioSource hitAudioSource;

    private float lastHitTime = -999f;

    /// <summary>
    /// 外部主動呼叫：傳入精準命中點
    /// </summary>
    public void TriggerShieldHit(Vector3 hitPoint)
    {
        if (useHitCooldown)
        {
            if (Time.time - lastHitTime < hitCooldown)
                return;
        }

        lastHitTime = Time.time;

        SpawnRipple(hitPoint);
        ApplyShieldDamage();
        PlayHitSound();
    }

    private void SpawnRipple(Vector3 hitPoint)
    {
        if (ripplePrefab == null)
        {
            Debug.LogWarning("沒有指定 ripplePrefab！");
            return;
        }

        GameObject ripple = Instantiate(ripplePrefab, transform);
        VisualEffect vfx = ripple.GetComponent<VisualEffect>();

        if (vfx != null)
        {
            vfx.SetVector3("SphereCenter", hitPoint);
        }
        else
        {
            Debug.LogWarning("Ripple prefab 上沒有 VisualEffect 元件");
        }

        Destroy(ripple, rippleLifeTime);
    }

    private void ApplyShieldDamage()
    {
        if (shieldController != null)
        {
            shieldController.TakeHit(damagePerHit);
        }
    }

    private void PlayHitSound()
    {
        if (hitAudioSource != null)
        {
            hitAudioSource.Play();
        }
    }
}