using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using MaskTransitions;   // 轉場動畫用

public class MainMenuStart : MonoBehaviour
{
    [Header("UI")]
    // 整個 ID 輸入面板（就是 PlayerIDInput 那個物件）
    public GameObject playerIdRoot;
    // 面板裡的 TMP_InputField
    public TMP_InputField playerIdInput;

    [Header("Scene")]
    // 按完確認後要去的場景名稱（在 Inspector 裡改）
    public string nextSceneName = "Story";

    // 記錄 ID 面板有沒有打開過
    private bool idShown = false;

    private void Start()
    {
        // 保險：一開始先關掉 ID 面板
        if (playerIdRoot != null)
        {
            playerIdRoot.SetActive(false);
        }
    }

    /// <summary>
    /// 主畫面「開始」按鈕用：
    /// 第一次按：只打開 ID 面板，不開始遊戲。
    /// 之後的按壓就不再理它，一切交給 GoogleSheet 的確認鍵。
    /// </summary>
    public void OnStartButtonPressed()
    {
        if (idShown)
            return;

        idShown = true;

        if (playerIdRoot != null)
            playerIdRoot.SetActive(true);

        if (playerIdInput != null)
            playerIdInput.ActivateInputField();
    }

    /// <summary>
    /// 給 GoogleSheetDataHandler 的 OnPlayerIDEntered 事件用。
    /// 玩家在輸入框按下 Enter / 確認、ID 成功寫入後，就會呼叫這個。
    /// </summary>
    public void StartGameAfterId()
    {
        string id = playerIdInput != null ? playerIdInput.text.Trim() : "";

        if (string.IsNullOrEmpty(id))
        {
            Debug.LogWarning("Player ID 尚未輸入，無法開始遊戲。");
            return;
        }

        // 設定這一局的玩家 ID
        ResultData.playerId = id;

        // 讀取這位玩家的歷史最佳紀錄（結算畫面會用到）
        PlayerDataStore.LoadBestStats(id, out ResultData.bestScore, out ResultData.bestMaxCombo);

        // 用 TransitionManager 播轉場動畫載入下一個場景
        if (TransitionManager.Instance != null)
        {
            TransitionManager.Instance.LoadLevel(nextSceneName);
        }
        else
        {
            // 萬一場景裡沒放 TransitionManager，就退回用一般載入
            SceneManager.LoadScene(nextSceneName);
        }
    }
}
