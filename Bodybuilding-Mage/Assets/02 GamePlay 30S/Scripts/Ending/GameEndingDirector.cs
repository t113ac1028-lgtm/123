using UnityEngine;
using System;
using System.Collections;

public class GameEndingDirector : MonoBehaviour
{
    [Header("主角設定")]
    public Animator bossAnim;
    public BossHitControl hitControl;
    public GameObject uiCanvas;

    [Header("其他隱藏設定")]
    [Tooltip("結局時要把什麼藏起來？請把【左手】和【右手】的物件拖進來")]
    public GameObject[] objectsToHide;

    [Header("慢動作設定")]
    [Tooltip("慢動作的倍率 (1=正常, 0.2=很慢)")]
    public float slowMotionScale = 0.2f;
    [Tooltip("死亡動畫開始後，要等幾秒才進入慢動作")]
    public float impactDelay = 0.15f; 

    [Header("Phase 1: 進階運鏡設定")]
    public Camera mainCam;
    [Tooltip("請拖進場景中你設定好的「終點攝影機」空物件")]
    public Transform endCamPosition; 
    [Tooltip("主要運鏡要花幾秒 (飛到終點的時間)")]
    public float moveDuration = 2.5f;
    [Tooltip("運鏡的節奏曲線 (建議 Ease Out)")]
    public AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    // ★ 新增：Phase 2 軌道運鏡
    [Header("Phase 2: 結算時的注視與旋轉")]
    [Tooltip("鏡頭要一直看著誰？請把 Boss (梅林) 拖進來")]
    public Transform lookAtTarget;
    [Tooltip("抵達終點後，繞著 Boss 旋轉的速度 (正數往左轉，負數往右轉，建議 2 ~ 5)")]
    public float orbitSpeed = 4.0f;
    [Tooltip("旋轉時鏡頭轉向 Boss 的平滑度 (越小越慢，越大越快，建議 2)")]
    public float lookSmoothness = 2.0f;

    [Header("流程設定")]
    [Tooltip("從開始演出的那一刻起，要等幾秒才跳出結算 UI (通常比 moveDuration 長)")]
    public float totalDuration = 4.0f;

    public void PlayEnding(Action onComplete)
    {
        StartCoroutine(EndingSequence(onComplete));
    }

    IEnumerator EndingSequence(Action onComplete)
    {
        Debug.Log("[Director] Action! 結局開拍。");

        if (uiCanvas != null) uiCanvas.SetActive(false);
        if (hitControl != null) hitControl.enabled = false;

        if (objectsToHide != null)
        {
            foreach (var obj in objectsToHide)
            {
                if (obj != null) obj.SetActive(false);
            }
        }

        if (bossAnim != null) bossAnim.SetTrigger("Die");

        yield return new WaitForSecondsRealtime(impactDelay);

        // 啟動慢動作
        Time.timeScale = slowMotionScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        // 開始運鏡 (這個協程現在會一直跑，直到場景切換)
        StartCoroutine(CameraMoveRoutine());

        // 等待「演出時間」結束
        yield return new WaitForSecondsRealtime(totalDuration);

        // 恢復時間 (這樣你的結算 UI 彈出動畫才會是正常速度)
        Time.timeScale = 1.0f;
        Time.fixedDeltaTime = 0.02f;

        Debug.Log("[Director] 演出時間到，顯示 UI (鏡頭繼續動)");
        onComplete?.Invoke();
    }

    IEnumerator CameraMoveRoutine()
    {
        if (mainCam == null || endCamPosition == null) yield break;

        Vector3 startPos = mainCam.transform.position;
        Quaternion startRot = mainCam.transform.rotation;
        
        Vector3 endPos = endCamPosition.position;
        Quaternion endRot = endCamPosition.rotation;

        float timer = 0f;

        // ★ 修改：使用 while(true) 讓它永遠跑下去，直到場景被切換銷毀
        while (true)
        {
            // 使用 unscaledDeltaTime 確保鏡頭速度不受慢動作影響
            float dt = Time.unscaledDeltaTime;
            timer += dt;

            if (timer <= moveDuration)
            {
                // === Phase 1: 飛向終點 (Lerp) ===
                float linearT = Mathf.Clamp01(timer / moveDuration);
                float curveT = moveCurve.Evaluate(linearT);

                mainCam.transform.position = Vector3.Lerp(startPos, endPos, curveT);
                mainCam.transform.rotation = Quaternion.Lerp(startRot, endRot, curveT);
            }
            else
            {
                // === Phase 2: 軌道旋轉 (Orbit) ===
                // 這時候 timer 已經超過 moveDuration，進入結算畫面階段
                
                if (lookAtTarget != null)
                {
                    // 1. 繞著 Boss 旋轉 (Orbit)
                    // 使用 RotateAround 會基於「當前位置」繼續轉，所以位置絕對不會跳變！
                    // Vector3.up 代表繞著 Y 軸轉
                    mainCam.transform.RotateAround(lookAtTarget.position, Vector3.up, orbitSpeed * dt);

                    // 2. 平滑轉向 Boss (Smooth LookAt)
                    // 計算「我應該要看哪裡」
                    Vector3 directionToTarget = lookAtTarget.position - mainCam.transform.position;
                    // 保持一點高度視角 (Optional: 稍微修正一下讓它不要看地板)
                    // directionToTarget.y = 0; 
                    
                    if (directionToTarget != Vector3.zero)
                    {
                        Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
                        // 使用 Slerp 慢慢轉過去，解決「順切」造成的鏡頭抖動
                        mainCam.transform.rotation = Quaternion.Slerp(mainCam.transform.rotation, targetRotation, lookSmoothness * dt);
                    }
                }
                else
                {
                    // (備案) 如果沒設 Target，就維持原本的向前漂浮
                    mainCam.transform.Translate(Vector3.forward * 0.05f * dt);
                }
            }

            yield return null;
        }
    }
}