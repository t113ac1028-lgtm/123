using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenuStart : MonoBehaviour
{
    [Header("UI")]
    public GameObject playerIdRoot;      // 整個 ID 框（PlayerInput 那個物件）
    public TMP_InputField playerIdInput; // 輸入欄本體

    [Header("Scene")]
    public string gameplaySceneName = "GamePlay 30S program DEMO"; 
    // 把這裡改成你真正的 gameplay 場景名字

    private bool idShown = false;  // 記錄「ID 框是不是已經打開過」

    public void OnStartButtonPressed()
    {
        // 第一次按：打開 ID 欄位就好，不進遊戲
        if (!idShown)
        {
            idShown = true;
            if (playerIdRoot != null)
                playerIdRoot.SetActive(true);

            if (playerIdInput != null)
                playerIdInput.ActivateInputField(); // 讓游標自動跑進去

            return;
        }

        // 第二次按：檢查有沒有輸入 ID，有的話進遊戲
        string id = playerIdInput != null ? playerIdInput.text.Trim() : "";

        if (string.IsNullOrEmpty(id))
        {
            Debug.LogWarning("請先輸入 Player ID 再開始遊戲。");
            return;
        }

        // 設定這一局的玩家 ID
        ResultData.playerId = id;

        // 先把這個玩家的舊紀錄讀進來（之後結算畫面會用）
        PlayerDataStore.LoadBestStats(id, out ResultData.bestScore, out ResultData.bestMaxCombo);

        // 進入 gameplay 場景
        SceneManager.LoadScene(gameplaySceneName);
    }
}
