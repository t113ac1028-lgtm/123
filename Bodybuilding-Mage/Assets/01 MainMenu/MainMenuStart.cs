using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using MaskTransitions;   // ★ 新增：引用 MaskTransitions 才能用 TransitionManager

public class MainMenuStart : MonoBehaviour
{
    [Header("UI")]
    public GameObject playerIdRoot;      // 整個 ID 框（PlayerInput 那個物件）
    public TMP_InputField playerIdInput; // 輸入欄本體

    [Header("Scene")]
    public string gameplaySceneName = "GamePlay 30S program DEMO";
    // 這個改成你要去的「Story 場景名稱」就好，例如 "StoryScene"

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
            // ★ 這裡直接 return，完全不會進場景，也不會播動畫
        }

        // 第二次按：檢查有沒有輸入 ID，有的話才繼續
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

        // ★★ 這裡改成「有轉場的載入方式」★★
        if (TransitionManager.Instance != null)
        {
            // 用圓形/方形遮罩動畫載入目標場景（Story 或 Gameplay）
            TransitionManager.Instance.LoadLevel(gameplaySceneName);
        }
        else
        {
            // 安全保險：如果場景裡不小心沒放 TransitionManager，就用原本的方式
            SceneManager.LoadScene(gameplaySceneName);
        }
    }
}
