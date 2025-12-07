using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using MaskTransitions;

public class LeaderboardSimple : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text leaderboardText;      // 單一 TMP 顯示全部排行
    public int maxEntries = 10;          // 顯示幾名

    [Header("Scene")]
    public string mainMenuSceneName = "Main Menu";

    void Start()
    {
        ShowLeaderboard();
    }

    void Update()
    {
        // 按 Enter / 數字鍵盤 Enter 回主選單
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            ReturnToMainMenu();
        }
    }


    private string GetOrdinal(int number)
{
    if (number <= 0) return number.ToString();

    int lastTwo = number % 100;

    if (lastTwo >= 11 && lastTwo <= 13)
        return number + "th";

    switch (number % 10)
    {
        case 1: return number + "st";
        case 2: return number + "nd";
        case 3: return number + "rd";
        default: return number + "th";
    }
}

    public void ShowLeaderboard()
    {
        if (leaderboardText == null)
        {
            Debug.LogWarning("LeaderboardSimple: leaderboardText is not assigned.");
            return;
        }

        string[] allIds = PlayerDataStore.GetAllPlayerIds();

        if (allIds == null || allIds.Length == 0)
        {
            leaderboardText.text = "No records yet.";
            return;
        }

        List<KeyValuePair<string, int>> entries = new List<KeyValuePair<string, int>>();

        foreach (string id in allIds)
        {
            if (string.IsNullOrWhiteSpace(id)) continue;

            PlayerDataStore.LoadBestStats(id, out int bestScore, out _);

            if (bestScore <= 0) continue; // 沒分數就略過

            entries.Add(new KeyValuePair<string, int>(id, bestScore));
        }

        if (entries.Count == 0)
        {
            leaderboardText.text = "No records yet.";
            return;
        }

        // 高分在前
        entries.Sort((a, b) => b.Value.CompareTo(a.Value));

        int count = Mathf.Min(maxEntries, entries.Count);
        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        for (int i = 0; i < count; i++)
{
    string id = entries[i].Key;
    int score = entries[i].Value;

    string rank = GetOrdinal(i + 1);  // 取得 1st / 2nd / 3rd / ...

    sb.AppendLine($"{rank}  {id} - Score: {score}");
}


        leaderboardText.text = sb.ToString();
    }

    public void ReturnToMainMenu()
{
    if (TransitionManager.Instance != null)
    {
        TransitionManager.Instance.LoadLevel(mainMenuSceneName);
    }
    else
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }
}

}
