using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI;

public class TutorialSceneLoader : MonoBehaviour
{

    [Header("Loading 畫面")]
    [Tooltip("Loading 時顯示的整個面板 GameObject")]
    public GameObject loadingPanel;
    [Tooltip("進度條 Slider（value 0~1）")]
    public Slider loadingBar;

    [Header("下一個場景名稱")]
    public string nextSceneName = "GamePlay 30S program DEMO";

    [Header("延遲多久後切場景")]
    public float delayBeforeLoad = 2f;

    [Header("是否使用延遲切換")]
    public bool useDelay = true;

    private static readonly WaitForSeconds WaitLoadingComplete = new(0.2f);

    private bool isLoading = false;

    public void LoadNextScene()
    {
        if (isLoading) return;
        if (TransitionGuard.IsSwitchingScene) return;

        isLoading = true;

        if (useDelay)
            StartCoroutine(LoadNextSceneRoutine());
        else
            StartCoroutine(LoadSceneAsyncRoutine());
    }

    private IEnumerator LoadNextSceneRoutine()
    {
        yield return new WaitForSecondsRealtime(delayBeforeLoad);
        yield return LoadSceneAsyncRoutine();
    }

    private IEnumerator LoadSceneAsyncRoutine()
    {
        TransitionGuard.Begin();

        DownSwingDetector[] detectors = FindObjectsOfType<DownSwingDetector>();
        foreach (var d in detectors)
            if (d != null) d.enabled = false;

        JoyconHands[] joyHands = FindObjectsOfType<JoyconHands>();
        foreach (var jh in joyHands)
            if (jh != null) jh.enabled = false;

        TutorialAltAndSlamCoordinator tutorialCoordinator = FindObjectOfType<TutorialAltAndSlamCoordinator>();
        if (tutorialCoordinator != null) tutorialCoordinator.enabled = false;

        TutorialEffectSpawner tutorialSpawner = FindObjectOfType<TutorialEffectSpawner>();
        if (tutorialSpawner != null) tutorialSpawner.enabled = false;

        yield return new WaitForSecondsRealtime(0.15f);

        if (loadingPanel != null) loadingPanel.SetActive(true);
        if (loadingBar != null) loadingBar.value = 0f;

        AsyncOperation op = SceneManager.LoadSceneAsync(nextSceneName);
        op.allowSceneActivation = false;

        while (op.progress < 0.9f)
        {
            if (loadingBar != null) loadingBar.value = op.progress / 0.9f;
            yield return null;
        }

        if (loadingBar != null) loadingBar.value = 1f;
        yield return WaitLoadingComplete;

        op.allowSceneActivation = true;
    }
}