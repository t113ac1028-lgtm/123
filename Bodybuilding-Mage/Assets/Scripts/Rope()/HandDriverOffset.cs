using UnityEngine;

/// 跟隨 hand（或任何目標），並接受 Kick 產生短暫的附加位移/速度。
public class HandDriverOffset : MonoBehaviour
{
    [Header("Follow")]
    public Transform follow;          // 真的手（RightHand）
    public float followLerp = 20f;    // 越大越緊跟

    [Header("Impulse (kick)")]
    public float impulseDamping = 12f; // 脈衝阻尼（越大越快消失）

    Vector3 _off;     // 代理與手的附加位移
    Vector3 _offVel;  // 位移的速度（受到 Kick 影響）

    void LateUpdate()
    {
        float dt = Mathf.Max(Time.deltaTime, 1e-4f);

        // 脈衝的簡單阻尼積分
        _off += _offVel * dt;
        _offVel = Vector3.Lerp(_offVel, Vector3.zero, 1f - Mathf.Exp(-impulseDamping * dt));

        // 讓代理跟到 hand 的同時加上附加位移
        if (follow)
        {
            Vector3 target = follow.position + _off;
            transform.position = Vector3.Lerp(
                transform.position, target,
                1f - Mathf.Exp(-followLerp * dt)
            );
        }
    }

    /// 給代理一個「速度脈衝」
    public void Kick(Vector3 impulse)
    {
        _offVel += impulse;
    }

    /// 直接做瞬間位移（很少用）
    public void KickInstant(Vector3 delta)
    {
        _off += delta;
    }

    /// 把代理重置回 hand 位置（可在場景 reset 時呼叫）
    public void SnapToHand()
    {
        if (follow) transform.position = follow.position;
        _off = Vector3.zero;
        _offVel = Vector3.zero;
    }
}
