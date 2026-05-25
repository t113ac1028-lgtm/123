using UnityEngine;
using UnityEngine.SceneManagement;

public class EnterToNextScene : MonoBehaviour
{
    public string nextScene = "GamePlay 30S program DEMO";

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Return)) return;
        if (!TransitionGuard.TryBegin()) return;

        SceneManager.LoadScene(nextScene);
    }
}
