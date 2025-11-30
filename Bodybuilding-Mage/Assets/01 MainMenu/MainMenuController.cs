using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    public void StartGame()
    {
        SceneManager.LoadScene("GamePlay 30S program DEMO", LoadSceneMode.Single);  // 填你的場景名稱
    }
}
