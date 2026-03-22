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
        isLoading = true;

        if (useDelay)
            StartCoroutine(LoadNextSceneRoutine());
        else
            SceneManager.LoadScene(nextSceneName);
    }

    private IEnumerator LoadNextSceneRoutine()
    {
        yield return new WaitForSecondsRealtime(delayBeforeLoad);
        SceneManager.LoadScene(nextSceneName);
    }
}