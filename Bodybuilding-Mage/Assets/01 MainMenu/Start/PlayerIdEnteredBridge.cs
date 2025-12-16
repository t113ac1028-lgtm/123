using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerIdEnteredBridge : MonoBehaviour
{
    public void OnPlayerIdEntered()
    {
        // GoogleSheetDataHandler 是同學的腳本：我們只讀 Instance / PlayerID，不修改它
        if (GoogleSheetDataHandler.Instance == null) return;

        string id = (GoogleSheetDataHandler.Instance.PlayerID ?? "").Trim();
        if (string.IsNullOrEmpty(id)) return;

        // 只要目前場景有 MainMenuStart，就把流程交給它（Scene 來回也不會 Missing）
        var mainMenu = FindObjectOfType<MainMenuStart>();
        if (mainMenu != null)
        {
            mainMenu.StartGameAfterId(id);
        }
        else
        {
            Debug.Log($"[Bridge] PlayerID entered ({id}) but MainMenuStart not found in scene: {SceneManager.GetActiveScene().name}");
        }
    }
}
