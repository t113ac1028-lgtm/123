using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections; // 為了使用協程
using MaskTransitions;    // 引用遮罩插件

public class GamePlayController : MonoBehaviour
{
    [Header("Debug / Reset")]
    [SerializeField] private bool enableKeyboardReset = true;
    [SerializeField] private KeyCode resetKey = KeyCode.R;

    public static GamePlayController Instance { get; private set; }

    [Header("Core Systems")]
    [SerializeField] private DamageCalculator damage;
    [SerializeField] private ComboCounter combo;

    [Header("Full Screen Effect")]
    [Tooltip("如果沒有這個腳本請把這行刪掉")]
    [SerializeField] private BreathFXController breathFX; 

    [Header("Camera Shake")]
    public HandheldCameraEffect cameraShake;

    [Header("Ending Director")]
    public GameEndingDirector endingDirector;

    // ★★★ UI 流程設定 ★★★
    [Header("UI Flow - 1. 結算")]
    [Tooltip("程式會自動抓取場景中的 Result UI")]
    public GameResultUI resultUI;

    [Header("UI Flow - 2. 排行榜")]
    [Tooltip("程式會自動抓取場景中的 Ranking UI")]
    public GameObject rankingUIPanel;
    [Tooltip("程式會自動抓取")]
    public RankingListPopulator rankingPopulator;

    [Header("UI Flow - 3. 離開")]
    [Tooltip("看完排行榜後，按 Enter 要回到的場景名稱 (例如 Main Menu)")]
    public string mainMenuSceneName = "Main Menu";

    [Header("Match Settings")]
    [SerializeField] private bool autoStartOnSceneLoad = false;

    private bool playing;
    private bool isWaitForRank = false; // 狀態：正在看結算
    private bool isViewingRank = false; // 狀態：正在看排行榜

    public static bool IsPlaying => Instance != null && Instance.playing;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        Time.timeScale = 1.0f;

        // 自動抓取
        if (resultUI == null) 
            resultUI = FindObjectOfType<GameResultUI>(true);

        if (rankingPopulator == null) 
            rankingPopulator = FindObjectOfType<RankingListPopulator>(true);

        if (rankingUIPanel == null && rankingPopulator != null) 
            rankingUIPanel = rankingPopulator.gameObject;

        if (cameraShake == null)
            cameraShake = FindObjectOfType<HandheldCameraEffect>();

        if (endingDirector == null)
            endingDirector = FindObjectOfType<GameEndingDirector>();

        // 初始隱藏 UI
        if (resultUI != null) resultUI.gameObject.SetActive(false);
        if (rankingUIPanel != null) rankingUIPanel.SetActive(false);

        if (cameraShake != null) cameraShake.SetShaking(true);
    }

    private void Start()
    {
        if (autoStartOnSceneLoad)
        {
            StartMatch();
        }
    }

    private void Update()
    {
        // 1. 遊戲未開始
        if (!playing && !autoStartOnSceneLoad && !isWaitForRank && !isViewingRank)
        {
            if (Countdown.gameStarted)
            {
                StartMatch();
            }
        }

        // 2. 測試用重置
        if (enableKeyboardReset && Input.GetKeyDown(resetKey))
        {
            ResetGameplay();
        }

        // ★ 3. 結算階段 (isWaitForRank)：等待按 Enter -> 淡出內容，顯示排行榜
        if (isWaitForRank)
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                Debug.Log("[UI] Enter pressed. Switching to Ranking...");
                StartCoroutine(AnimateSwitchToRanking());
            }
        }

        // ★ 4. 排行榜階段 (isViewingRank)：等待按 Enter -> 回主選單
        if (isViewingRank)
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                Debug.Log("[UI] Enter pressed. Going to Main Menu...");
                GoToMainMenu();
            }
        }
    }

    private void StartMatch()
    {
        if (playing) return;
        playing = true;
        isWaitForRank = false;
        isViewingRank = false;
        
        Time.timeScale = 1.0f;

        if (combo != null) combo.Clear();
        if (breathFX != null) breathFX.StartEffect();

        if (rankingUIPanel != null) rankingUIPanel.SetActive(false);
        if (resultUI != null) resultUI.gameObject.SetActive(false);

        Debug.Log("[GamePlay] Match started");
    }

    public void OnTimerFinished()
    {
        if (!playing) return;

        Debug.Log("[GamePlay] Timer finished.");
        
        playing = false;
        
        if (breathFX != null) breathFX.StopEffect();
        if (cameraShake != null) cameraShake.SetShaking(false);

        if (endingDirector != null)
        {
            endingDirector.PlayEnding(EndMatch);
        }
        else
        {
            EndMatch();
        }
    }

    public void ResetGameplay()
    {
        Time.timeScale = 1.0f;
        Time.fixedDeltaTime = 0.02f;
        var scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.name);
    }

    private void GoToMainMenu()
    {
        Time.timeScale = 1.0f;
        if (TransitionManager.Instance != null)
        {
            TransitionManager.Instance.LoadLevel(mainMenuSceneName);
        }
        else
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }

    // ★ 切換邏輯：保留背景，只淡出內容，然後淡入排行榜
    IEnumerator AnimateSwitchToRanking()
    {
        isWaitForRank = false; // 鎖定輸入

        float duration = 0.5f; 

        // 1. 呼叫 ResultUI 裡面的淡出功能
        // 這會只淡出你設定的 contentToFade (結算圖案)，不會動到 backgroundGroup (黑底)
        if (resultUI != null)
        {
            yield return StartCoroutine(resultUI.FadeOutBoardRoutine(duration));
        }

        // 2. 開啟排行榜 (Fade In Ranking UI)
        if (rankingUIPanel != null)
        {
            rankingUIPanel.SetActive(true);
            
            if (rankingPopulator != null) rankingPopulator.RefreshRanking();

            // 讓排行榜慢慢浮現 (它會疊在原本的黑底之上)
            CanvasGroup rankCG = rankingUIPanel.GetComponent<CanvasGroup>();
            if (rankCG == null) rankCG = rankingUIPanel.AddComponent<CanvasGroup>();
            
            rankCG.alpha = 0f; 
            float t = 0;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / duration;
                rankCG.alpha = t;
                yield return null;
            }
            rankCG.alpha = 1f;

            isViewingRank = true; // 開放輸入，允許回主選單
        }
        else
        {
            Debug.LogWarning("沒有設定 Ranking UI Panel，直接回主選單");
            GoToMainMenu();
        }
    }

    private void EndMatch()
    {
        playing = false; 

        int finalScore = damage != null ? damage.Total() : 0;
        int maxCombo   = combo  != null ? combo.Max  : 0;

        ResultData.lastScore    = finalScore;
        ResultData.lastMaxCombo = maxCombo;

        PlayerDataStore.UpdateBestForCurrentRun(); 
        
        int bestScore = ResultData.bestScore;
        int bestCombo = ResultData.bestMaxCombo;
        string currentId = ResultData.playerId;

        int myRank = 1;
        if (!string.IsNullOrEmpty(currentId))
        {
            string[] allIds = PlayerDataStore.GetAllPlayerIds();
            foreach (var id in allIds)
            {
                if (id == currentId) continue;
                PlayerDataStore.LoadBestStats(id, out int pScore, out int pCombo);
                if (pScore > bestScore) myRank++;
            }
        }

        try
        {
            if (GoogleSheetDataHandler.Instance != null)
                GoogleSheetDataHandler.Instance.UploadScore(finalScore);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[Score] UploadScore failed: " + e.Message);
        }

        if (resultUI != null)
        {
            resultUI.ShowResult(finalScore, maxCombo, bestScore, bestCombo, currentId, myRank);
            isWaitForRank = true; 
        }
        else
        {
            // 備案
            StartCoroutine(AnimateSwitchToRanking());
        }
    }
}