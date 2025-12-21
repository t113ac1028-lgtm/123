using UnityEngine;

public class DebugDataClearer : MonoBehaviour
{
    [Header("設定")]
    [Tooltip("按下這顆鍵來清空所有存檔")]
    public KeyCode clearKey = KeyCode.F12;

    void Update()
    {
        // 偵測按鍵
        if (Input.GetKeyDown(clearKey))
        {
            PerformClear();
        }
    }

    public void PerformClear()
    {
        Debug.LogWarning("準備清空數據...");

        // 1. 清除硬碟存檔 (PlayerPrefs)
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        // 2. 清除記憶體中的暫存資料 (ResultData)
        // 這樣確保下一次進入遊戲時，不會讀到舊的資料
        ResultData.lastScore = 0;
        ResultData.lastMaxCombo = 0;
        ResultData.bestScore = 0;
        ResultData.bestMaxCombo = 0;
        
        // (選擇性) 如果你希望連輸入過的玩家 ID 都忘記，把下面這行取消註解
        // ResultData.playerId = ""; 

        // 3. 提示已完成
        Debug.Log("數據已清空... 已完成");
    }
}