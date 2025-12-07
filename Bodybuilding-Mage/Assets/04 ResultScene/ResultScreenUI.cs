using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using MaskTransitions;

public class ResultScreenUI : MonoBehaviour
{
    [Header("Texts")]
    public TMP_Text playerIdText;
    public TMP_Text lastScoreText;
    public TMP_Text lastComboText;
    public TMP_Text bestScoreText;
    public TMP_Text bestComboText;

    [Header("Scenes")]
    public string mainMenuSceneName = "Main Menu";
    public string rankingSceneName = "RankingList";   // ★ 新增：排行榜場景名稱

    void Start()
    {
        if (playerIdText != null)
            playerIdText.text = string.IsNullOrEmpty(ResultData.playerId)
                ? "Player: Unknown"
                : $"Player: {ResultData.playerId}";

        if (lastScoreText != null)
            lastScoreText.text = $"Score: {ResultData.lastScore}";

        if (lastComboText != null)
            lastComboText.text = $"Max Combo: {ResultData.lastMaxCombo}";

        if (bestScoreText != null)
            bestScoreText.text = $"Best Score: {ResultData.bestScore}";

        if (bestComboText != null)
            bestComboText.text = $"Best Combo: {ResultData.bestMaxCombo}";
    }

    void Update()
    {
        // ★★★ 這裡：按 Enter → 進排行榜 ★★★
        if (Input.GetKeyDown(KeyCode.Return))
        {
            GoToRanking();
        }
    }

    // ★ 新增：進排行榜的函式
    // ★ 進排行榜，用 TransitionManager 轉場
public void GoToRanking()
{
    if (TransitionManager.Instance != null)
    {
        // 用遮罩動畫切去排行榜場景
        TransitionManager.Instance.LoadLevel(rankingSceneName);
    }
    else
    {
        // 萬一沒掛 TransitionManager，就直接換場景
        SceneManager.LoadScene(rankingSceneName);
    }
}

}
