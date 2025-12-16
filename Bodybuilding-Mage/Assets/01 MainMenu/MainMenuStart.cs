using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using MaskTransitions;

public class MainMenuStart : MonoBehaviour
{
    [Header("UI (可不填，會自動找)")]
    public GameObject playerIdRoot;
    public TMP_InputField playerIdInput;

    [Header("Scene")]
    public string storySceneName = "Story";
    public string gameplaySceneName = "GamePlay 30S program DEMO";

    private bool idShown = false;

    private void OnEnable()
    {
        // 回到 MainMenu 時，確保可以重新按 Start
        idShown = false;

        // 回到 MainMenu 時，先把輸入框藏起來（如果找得到）
        ResolveUI();
        if (playerIdRoot != null) playerIdRoot.SetActive(false);
    }

    /// <summary>
    /// 自動找到 DontDestroyOnLoad 裡的 PlayerIdInput（避免 Missing）
    /// </summary>
    private void ResolveUI()
    {
        if (playerIdRoot != null && playerIdInput != null) return;

        // ✅ 這個可以找「包含 inactive」的所有物件
        var all = Object.FindObjectsByType<Transform>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (var t in all)
        {
            if (t != null && t.name == "PlayerIDInput")
            {
                playerIdRoot = t.gameObject;
                playerIdInput = playerIdRoot.GetComponentInChildren<TMP_InputField>(true);
                break;
            }
        }

        if (playerIdRoot == null)
            Debug.LogWarning("[MainMenuStart] 找不到 PlayerIdInput（請確認物件名稱真的叫 PlayerIDInput）");

        if (playerIdRoot != null && playerIdInput == null)
            Debug.LogWarning("[MainMenuStart] 找到了 PlayerIdInput，但底下找不到 TMP_InputField");
    }
    public void OnStartButtonPressed()
    {
        ResolveUI();

        if (idShown) return;
        idShown = true;

        if (playerIdRoot != null)
            playerIdRoot.SetActive(true);

        if (playerIdInput != null)
        {
            playerIdInput.text = "";
            playerIdInput.ActivateInputField();
            playerIdInput.Select();
        }
        else
        {
            Debug.LogWarning("找不到 PlayerIdInput / TMP_InputField，請確認 DontDestroy 裡的物件名稱是 PlayerIdInput。");
        }
    }

    // 給 GoogleSheetDataHandler / Bridge 呼叫（你現在的流程）
    public void StartGameAfterId(string id)
    {
        id = (id ?? "").Trim();
        if (string.IsNullOrEmpty(id))
        {
            Debug.LogWarning("Player ID 尚未輸入，無法開始遊戲。");
            return;
        }

        ResultData.playerId = id;
        PlayerDataStore.LoadBestStats(id, out ResultData.bestScore, out ResultData.bestMaxCombo);

        // 進 Story（不是 Gameplay）
        if (TransitionManager.Instance != null)
            TransitionManager.Instance.LoadLevel(storySceneName);
        else
            SceneManager.LoadScene(storySceneName);
    }
}
