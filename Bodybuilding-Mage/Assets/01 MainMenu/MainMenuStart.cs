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
        idShown = false;
        ResolveUI();
        if (playerIdRoot != null) playerIdRoot.SetActive(false);
    }

    private void Update()
    {
        // ★ 修正 1：同步狀態。如果 UI 在外部被關掉（SetActive(false)），idShown 必須同步回 false
        if (playerIdRoot != null && !playerIdRoot.activeInHierarchy)
        {
            idShown = false;
        }

        // ★ 修正 2：按 Enter 開啟輸入框的邏輯
        // 必須同時滿足：1. UI 還沒顯示、2. 按下 Enter、3. 當前沒人在打字（避免打 ID 時誤觸）
        if (!idShown)
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                OnStartButtonPressed();
            }
        }
    }

    private void ResolveUI()
    {
        if (playerIdRoot != null && playerIdInput != null) return;

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
    }

    public void OnStartButtonPressed()
    {
        ResolveUI();

        // ★ 修正 3：移除 "if (idShown) return;"。
        // 不管 idShown 是不是 true，只要點了按鈕，我們就確保 UI 是開的並重新聚焦。
        if (playerIdRoot != null)
        {
            playerIdRoot.SetActive(true);
            idShown = true; // 標記為已顯示
        }

        if (playerIdInput != null)
        {
            // 強制聚焦。如果原本就開著但沒選中，點按鈕會幫你選回來
            playerIdInput.ActivateInputField();
            playerIdInput.Select();
            
            // 提示：如果你希望點擊 Start 時清空文字，保留下一行；如果不希望清空，請刪掉
            // playerIdInput.text = ""; 
        }
        else
        {
            Debug.LogWarning("[MainMenu] 找不到輸入框組件，請檢查 Hierarchy 設定。");
        }
    }

    public void StartGameAfterId(string id)
    {
        id = (id ?? "").Trim();
        if (string.IsNullOrEmpty(id)) return;

        ResultData.playerId = id;
        PlayerDataStore.LoadBestStats(id, out ResultData.bestScore, out ResultData.bestMaxCombo);

        if (TransitionManager.Instance != null)
            TransitionManager.Instance.LoadLevel(storySceneName);
        else
            SceneManager.LoadScene(storySceneName);
    }
}