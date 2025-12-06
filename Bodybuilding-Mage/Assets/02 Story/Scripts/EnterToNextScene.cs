using UnityEngine;
using UnityEngine.SceneManagement;

public class EnterToNextScene : MonoBehaviour
{
    public string nextScene = "GamePlay 30S program DEMO";

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return)) // 按 Enter
        {
            SceneManager.LoadScene(nextScene);
        }
    }
}
