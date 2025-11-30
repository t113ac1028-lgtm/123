using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class ResultScreenUI : MonoBehaviour
{
    [Header("Texts")]
    public TMP_Text playerIdText;
    public TMP_Text lastScoreText;
    public TMP_Text lastComboText;
    public TMP_Text bestScoreText;
    public TMP_Text bestComboText;

    [Header("Scenes")]
    public string mainMenuSceneName = "Main Menu"; // 你的主選單場景名稱

    void Start()
    {
        // 顯示玩家 ID（可能沒填就顯示 Unknown）
        if (playerIdText != null)
            playerIdText.text = string.IsNullOrEmpty(ResultData.playerId)
                ? "Player: Unknown"
                : $"Player: {ResultData.playerId}";

        // 這一局成績
        if (lastScoreText != null)
            lastScoreText.text = $"Score: {ResultData.lastScore}";

        if (lastComboText != null)
            lastComboText.text = $"Max Combo: {ResultData.lastMaxCombo}";

        // 歷史最佳（PlayerDataStore.UpdateBestForCurrentRun 已經幫你寫進 ResultData 了）
        if (bestScoreText != null)
            bestScoreText.text = $"Best Score: {ResultData.bestScore}";

        if (bestComboText != null)
            bestComboText.text = $"Best Combo: {ResultData.bestMaxCombo}";
    }

    // 給「回主選單」按鈕用
    public void OnClickBackToMenu()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }

    // 如果你想要「再玩一次」，也可以多做一個
    public void OnClickReplay()
    {
        SceneManager.LoadScene("GamePlay 30S program DEMO"); // 換成你的 gameplay 場景名字
    }
}
