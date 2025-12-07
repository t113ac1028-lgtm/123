using UnityEngine;
using System.Collections.Generic;

public static class PlayerDataStore
{
    // --- Key helpers ---

    const string PlayerIdListKey = "PlayerIdList";   // 用 ; 分隔存所有玩家 ID

    static string BestScoreKey(string id) => $"Player_{id}_BestScore";
    static string BestComboKey(string id) => $"Player_{id}_BestCombo";

    // --- 讀 / 寫最佳紀錄 ---

    public static void LoadBestStats(string id, out int bestScore, out int bestCombo)
    {
        bestScore = PlayerPrefs.GetInt(BestScoreKey(id), 0);
        bestCombo = PlayerPrefs.GetInt(BestComboKey(id), 0);
    }

    public static void SaveBestStats(string id, int bestScore, int bestCombo)
    {
        PlayerPrefs.SetInt(BestScoreKey(id), bestScore);
        PlayerPrefs.SetInt(BestComboKey(id), bestCombo);

        RegisterPlayerId(id);   // ★ 確保這個 ID 被加入清單

        PlayerPrefs.Save();
    }

    /// <summary>
    /// 用傳進來的 id 以及 ResultData.lastScore / lastMaxCombo
    /// 來更新該玩家的最佳紀錄。
    /// </summary>
    public static void SaveAndUpdateFromResult(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return;

        // 目前紀錄
        LoadBestStats(id, out int bestScore, out int bestCombo);

        // 這一局成績
        int curScore = ResultData.lastScore;
        int curCombo = ResultData.lastMaxCombo;

        if (curScore > bestScore) bestScore = curScore;
        if (curCombo > bestCombo) bestCombo = curCombo;

        // 存回去（內部會順便註冊 PlayerId）
        SaveBestStats(id, bestScore, bestCombo);

        // 同步到 ResultData，讓 ResultScene 顯示用
        ResultData.bestScore    = bestScore;
        ResultData.bestMaxCombo = bestCombo;
    }

    /// <summary>
    /// 給現有程式呼叫的版本：會讀 ResultData.playerId 再去更新。
    /// （GamePlayController.EndMatch 用的就是這個）
    /// </summary>
    public static void UpdateBestForCurrentRun()
    {
        if (string.IsNullOrWhiteSpace(ResultData.playerId))
            return;

        SaveAndUpdateFromResult(ResultData.playerId.Trim());
    }

    // --- 玩家 ID 清單，用於排行榜 ---

    /// <summary> 把玩家 ID 註冊進清單（如果還沒存在）。 </summary>
    public static void RegisterPlayerId(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return;

        id = id.Trim();

        string raw = PlayerPrefs.GetString(PlayerIdListKey, string.Empty);
        List<string> ids = new List<string>();

        if (!string.IsNullOrEmpty(raw))
        {
            ids.AddRange(raw.Split(';'));
        }

        if (!ids.Contains(id))
        {
            ids.Add(id);
            string joined = string.Join(";", ids);
            PlayerPrefs.SetString(PlayerIdListKey, joined);
            PlayerPrefs.Save();
        }
    }

    /// <summary> 取得目前所有已被記錄的玩家 ID。 </summary>
    public static string[] GetAllPlayerIds()
    {
        string raw = PlayerPrefs.GetString(PlayerIdListKey, string.Empty);
        if (string.IsNullOrEmpty(raw))
            return new string[0];

        return raw.Split(';');
    }
}
