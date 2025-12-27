using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Countdown : MonoBehaviour
{
    [Header("UI 顯示元件")]
    [Tooltip("請在 Canvas 上建立一個 Image 物件 (放在螢幕正中間)，然後拖進這裡")]
    public Image displayImage;

    [Header("倒數圖片 (請依序拖入)")]
    public Sprite count3;
    public Sprite count2;
    public Sprite count1;
    public Sprite countGo;

    [Header("倒數音效 (請依序拖入 AudioClip)")]
    public AudioClip sfx3;
    public AudioClip sfx2;
    public AudioClip sfx1;
    public AudioClip sfxGo;

    [Header("Q彈動畫設定")]
    [Tooltip("X軸是時間(0~1)，Y軸是縮放倍率。")]
    public AnimationCurve elasticCurve = new AnimationCurve(
        new Keyframe(0f, 0f), 
        new Keyframe(0.4f, 1.5f), // 彈出去
        new Keyframe(0.7f, 0.9f), // 縮回來
        new Keyframe(1f, 1f)      // 定住
    );

    [Tooltip("每一個數字顯示的總時間 (秒)")]
    public float durationPerNumber = 1.0f;

    [Tooltip("是否每次換圖都自動調整大小")]
    public bool autoSetNativeSize = true;

    public static bool gameStarted = false;  // 全域開關

    void Start()
    {
        gameStarted = false;                 
        StartCoroutine(CountdownRoutine());
    }

    IEnumerator CountdownRoutine()
    {
        if (displayImage != null)
        {
            displayImage.gameObject.SetActive(true);
        }

        // 播放 3 (圖片 + 音效)
        yield return StartCoroutine(PlayOneBeat(count3, sfx3));
        
        // 播放 2
        yield return StartCoroutine(PlayOneBeat(count2, sfx2));
        
        // 播放 1
        yield return StartCoroutine(PlayOneBeat(count1, sfx1));

        // 播放 GO
        yield return StartCoroutine(PlayOneBeat(countGo, sfxGo));

        // 結束
        if (displayImage != null)
        {
            displayImage.gameObject.SetActive(false);
        }

        // 🔥 正式開始遊戲
        gameStarted = true;
    }

    // 播放單一個節拍 (換圖 + 音效 + 動畫)
    IEnumerator PlayOneBeat(Sprite sprite, AudioClip sfx)
    {
        if (displayImage == null || sprite == null) yield break;

        // 1. 播放音效 (在攝影機位置播放，確保聽得到)
        if (sfx != null)
        {
            AudioSource.PlayClipAtPoint(sfx, Camera.main.transform.position);
        }

        // 2. 換圖
        displayImage.sprite = sprite;
        if (autoSetNativeSize) displayImage.SetNativeSize();

        // 3. 跑動畫曲線
        float timer = 0f;
        while (timer < durationPerNumber)
        {
            timer += Time.deltaTime;
            
            // 動畫只跑前 70%，後 30% 停住
            float animProgress = Mathf.Clamp01(timer / (durationPerNumber * 0.7f));
            float scale = elasticCurve.Evaluate(animProgress);
            
            displayImage.transform.localScale = Vector3.one * scale;

            yield return null;
        }
        
        displayImage.transform.localScale = Vector3.one;
    }
}