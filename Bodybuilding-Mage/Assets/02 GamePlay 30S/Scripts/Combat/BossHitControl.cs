using UnityEngine;
using System.Collections;

public class BossHitControl : MonoBehaviour
{
    public Animator anim;

    [Header("設定")]
    [Tooltip("最多連續抽搐幾下 (例如 3 下)，超過後就會強迫完整播完一次")]
    public int maxTwitchCount = 3;     

    [Tooltip("強制播完的保護時間 (填你的受擊動畫長度，例如 0.8 或 1.0)")]
    public float protectTime = 1.0f;   

    [Header("隨機受擊動作 (新增)")]
    [Tooltip("你有幾個受擊動作？例如填 3，就會隨機骰 0, 1, 2 給 Animator")]
    public int hitAnimCount = 1;       

    private int currentCount = 0;      // 目前累積次數
    private bool isProtected = false;  // 是否處於保護狀態

    // 讓飛彈來呼叫這個功能
    public void TryHit()
    {
        // ★ 關鍵修正：先檢查自己有沒有被「停用」
        // 當結局導演把這個腳本 enabled = false 時，這行會擋下所有飛彈的呼叫
        if (!this.enabled) return;

        // 1. 如果正在保護狀態 (動畫強制播放中)，直接無視這次觸發
        if (isProtected) return;

        // ------------------------------------------------------------
        // 2. 隨機切換動作邏輯
        // ------------------------------------------------------------
        if (hitAnimCount > 1)
        {
            int randomIndex = Random.Range(0, hitAnimCount);
            anim.SetInteger("HitIndex", randomIndex);
        }

        // 3. 觸發受擊 Trigger
        anim.SetTrigger("Hit");
        currentCount++;

        // 4. 檢查是否達到上限
        if (currentCount >= maxTwitchCount)
        {
            StartCoroutine(ProtectRoutine());
        }
    }

    IEnumerator ProtectRoutine()
    {
        isProtected = true;  // 開啟金鐘罩
        currentCount = 0;    // 歸零計數器

        yield return new WaitForSeconds(protectTime);

        isProtected = false; // 解除金鐘罩
    }
}