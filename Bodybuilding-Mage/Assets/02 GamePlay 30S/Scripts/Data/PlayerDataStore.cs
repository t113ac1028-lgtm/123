using UnityEngine;

public static class PlayerDataStore
{
    static string BestScoreKey(string id)  => $"Player_{id}_BestScore";
    static string BestComboKey(string id)  => $"Player_{id}_BestCombo";

    public static void LoadBestStats(string id, out int bestScore, out int bestCombo)
    {
        bestScore = PlayerPrefs.GetInt(BestScoreKey(id), 0);
        bestCombo = PlayerPrefs.GetInt(BestComboKey(id), 0);
    }

    public static void SaveBestStats(string id, int bestScore, int bestCombo)
    {
        PlayerPrefs.SetInt(BestScoreKey(id), bestScore);
        PlayerPrefs.SetInt(BestComboKey(id), bestCombo);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 更新該玩家的最佳成績，並回寫到 ResultData
    /// </summary>
    public static void UpdateBestForCurrentRun()
    {
        if (string.IsNullOrEmpty(ResultData.playerId))
            return;

        string id = ResultData.playerId;

        // 取舊紀錄
        LoadBestStats(id, out int bestScore, out int bestCombo);

        // 這一局成績
        int curScore = ResultData.lastScore;
        int curCombo = ResultData.lastMaxCombo;

        if (curScore > bestScore) bestScore = curScore;
        if (curCombo > bestCombo) bestCombo = curCombo;

        // 存回去
        SaveBestStats(id, bestScore, bestCombo);

        // 同步到 ResultData 方便結算畫面用
        ResultData.bestScore    = bestScore;
        ResultData.bestMaxCombo = bestCombo;
    }
}
