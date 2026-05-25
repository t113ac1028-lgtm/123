using UnityEngine;
using UnityEngine.SceneManagement;
using MaskTransitions;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private string storySceneName = "Story";

    public void StartGame()
    {
        if (!TransitionGuard.TryBegin()) return;

        if (TransitionManager.Instance != null)
            TransitionManager.Instance.LoadLevel(storySceneName);
        else
            SceneManager.LoadScene(storySceneName);
    }
}
