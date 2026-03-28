using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class TutorialSceneLoader : MonoBehaviour
{
    [Header("下一個場景名稱")]
    public string nextSceneName = "GamePlay 30S program DEMO";

    [Header("延遲多久後切場景")]
    public float delayBeforeLoad = 2f;

    [Header("是否使用延遲切換")]
    public bool useDelay = true;

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
        {
            if (d != null) d.enabled = false;
        }

        JoyconHands[] joyHands = FindObjectsOfType<JoyconHands>();
        foreach (var jh in joyHands)
        {
            if (jh != null) jh.enabled = false;
        }

        TutorialAltAndSlamCoordinator tutorialCoordinator = FindObjectOfType<TutorialAltAndSlamCoordinator>();
        if (tutorialCoordinator != null) tutorialCoordinator.enabled = false;

        TutorialEffectSpawner tutorialSpawner = FindObjectOfType<TutorialEffectSpawner>();
        if (tutorialSpawner != null) tutorialSpawner.enabled = false;

        yield return new WaitForSecondsRealtime(0.15f);

        AsyncOperation op = SceneManager.LoadSceneAsync(nextSceneName);
        while (!op.isDone)
            yield return null;
    }
}