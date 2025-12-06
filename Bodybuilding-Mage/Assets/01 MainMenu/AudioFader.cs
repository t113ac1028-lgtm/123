using UnityEngine;
using System.Collections;

public class AudioFader : MonoBehaviour
{
    [Header("Target Audio Source")]
    public AudioSource audioSource;

    [Header("Volume Settings")]
    [Range(0f, 1f)]
    public float targetVolume = 1f;      // 淡入的目標音量
    public float fadeInDuration = 1f;    // 淡入秒數
    public float fadeOutDuration = 1f;   // 淡出秒數

    [Header("Auto Start")]
    public bool fadeInOnStart = false;   // 是否一開始自動淡入

    private void Start()
    {
        if (fadeInOnStart)
        {
            FadeIn();
        }
    }

    // 👉 Inspector 可以按按鈕呼叫
    [ContextMenu("Fade In")]
    public void FadeIn()
    {
        StartCoroutine(FadeInRoutine());
    }

    [ContextMenu("Fade Out")]
    public void FadeOut()
    {
        StartCoroutine(FadeOutRoutine());
    }

    private IEnumerator FadeInRoutine()
    {
        audioSource.volume = 0f;
        audioSource.Play();

        float timer = 0f;
        while (timer < fadeInDuration)
        {
            timer += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(0f, targetVolume, timer / fadeInDuration);
            yield return null;
        }

        audioSource.volume = targetVolume;
    }

    private IEnumerator FadeOutRoutine()
    {
        float startVolume = audioSource.volume;
        float timer = 0f;

        while (timer < fadeOutDuration)
        {
            timer += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, 0f, timer / fadeOutDuration);
            yield return null;
        }

        audioSource.volume = 0f;
        audioSource.Stop();
    }
}
