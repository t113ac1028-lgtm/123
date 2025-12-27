using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using MaskTransitions;

/// <summary>
/// 遊戲流程主控制器
/// 負責：開始遊戲、倒數結束處理、結算 UI 顯示、排行榜切換。
/// 修正：加入 matchHandled 確保一局遊戲只會觸發一次 StartMatch。
/// </summary>
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
    [SerializeField] private BreathFXController breathFX; 

    [Header("Camera Shake")]
    public HandheldCameraEffect cameraShake;

    [Header("Ending Director")]
    public GameEndingDirector endingDirector;

    [Header("UI Flow")]
    public GameResultUI resultUI;
    public GameObject rankingUIPanel;
    public RankingListPopulator rankingPopulator;
    public string mainMenuSceneName = "Main Menu";

    [Header("Match Settings")]
    [SerializeField] private bool autoStartOnSceneLoad = false;

    private bool playing;
    private bool matchHandled = false; // ★ 核心修正：標記這局是否已經啟動過
    private bool isWaitForRank = false; 
    private bool isViewingRank = false; 

    // ★ 全域存取狀態：讓飛彈、發射器、Boss 知道現在是否在比賽中
    public static bool IsPlaying => Instance != null && Instance.playing;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Time.timeScale = 1.0f;

        // 自動搜尋遺漏的組件參考
        if (resultUI == null) resultUI = FindObjectOfType<GameResultUI>(true);
        if (rankingPopulator == null) rankingPopulator = FindObjectOfType<RankingListPopulator>(true);
        if (rankingUIPanel == null && rankingPopulator != null) rankingUIPanel = rankingPopulator.gameObject;
        if (cameraShake == null) cameraShake = FindObjectOfType<HandheldCameraEffect>();
        if (endingDirector == null) endingDirector = FindObjectOfType<GameEndingDirector>();

        // 初始隱藏 UI
        if (resultUI != null) resultUI.gameObject.SetActive(false);
        if (rankingUIPanel != null) rankingUIPanel.SetActive(false);
    }

    private void Start()
    {
        if (autoStartOnSceneLoad) StartMatch();
    }

    private void Update()
    {
        // ★ 修正重點：加入 !matchHandled 檢查。
        // 當 Countdown 結束後，只有在還沒處理過 Match 的情況下才啟動，防止重複觸發 StartMatch。
        if (!playing && !matchHandled && Countdown.gameStarted)
        {
            StartMatch();
        }

        // 鍵盤快速重置
        if (enableKeyboardReset && Input.GetKeyDown(resetKey)) ResetGameplay();

        // 結算畫面按 Enter 切換到排行榜
        if (isWaitForRank && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
            StartCoroutine(AnimateSwitchToRanking());

        // 排行榜按 Enter 返回主選單
        if (isViewingRank && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
            GoToMainMenu();
    }

    private void StartMatch()
    {
        if (playing) return;
        playing = true;
        matchHandled = true; // ★ 鎖定，這局遊戲不會再重複呼叫 StartMatch
        
        isWaitForRank = false;
        isViewingRank = false;
        Time.timeScale = 1.0f;

        if (combo != null) combo.Clear();
        
        // 啟動畫面特效倒數 (15秒後出現)
        if (breathFX != null) breathFX.StartEffect();
        
        Debug.Log("[GamePlay] Match started");
    }

    /// <summary>
    /// 當 GameTimer 歸零時由外部呼叫。
    /// </summary>
    public void OnTimerFinished()
    {
        if (!playing) return;
        playing = false; // 比賽狀態結束

        Debug.Log("[GamePlay] 0秒倒數結束！啟動三重硬鎖系統...");

        // 1. 關閉發射源 (防止 0 秒後還能發射)
        EffectSpawner spawner = FindObjectOfType<EffectSpawner>();
        if (spawner != null) spawner.enabled = false;

        AltAndSlamCoordinator coordinator = FindObjectOfType<AltAndSlamCoordinator>();
        if (coordinator != null) coordinator.enabled = false;

        // 2. 清除空中飛彈
        ProjectileHoming[] remainingProjectiles = FindObjectsOfType<ProjectileHoming>();
        foreach (var p in remainingProjectiles)
        {
            Destroy(p.gameObject);
        }

        // 3. 關閉特效與 Boss
        if (breathFX != null) breathFX.StopEffect();
        
        BossHitControl boss = FindObjectOfType<BossHitControl>();
        if (boss != null)
        {
            boss.TurnOffLight(1.5f);
            boss.enabled = false; 
        }
        
        if (cameraShake != null) cameraShake.SetShaking(false);

        // 4. 啟動導演結尾演出
        if (endingDirector != null)
        {
            endingDirector.PlayEnding(EndMatch);
        }
        else
        {
            EndMatch();
        }
    }

    private void EndMatch()
    {
        int finalScore = damage != null ? damage.Total() : 0;
        int maxCombo   = combo  != null ? combo.Max  : 0;

        ResultData.lastScore    = finalScore;
        ResultData.lastMaxCombo = maxCombo;
        string currentId = ResultData.playerId;

        // 排名計算邏輯
        int oldBestScore = 0;
        if (!string.IsNullOrEmpty(currentId))
            PlayerDataStore.LoadBestStats(currentId, out oldBestScore, out int _);
        
        int oldRank = CalculateRank(currentId, oldBestScore);
        PlayerDataStore.UpdateBestForCurrentRun(); 
        
        int bestScore = ResultData.bestScore;
        int bestCombo = ResultData.bestMaxCombo;
        int newRank = CalculateRank(currentId, bestScore);
        
        bool isRankUp = (newRank < oldRank) || (oldBestScore == 0 && bestScore > 0);

        if (resultUI != null)
        {
            resultUI.ShowResult(finalScore, maxCombo, bestScore, bestCombo, currentId, newRank, isRankUp);
            isWaitForRank = true; 
        }
        else
        {
            StartCoroutine(AnimateSwitchToRanking());
        }
    }

    private int CalculateRank(string myId, int myScore)
    {
        int rank = 1;
        if (string.IsNullOrEmpty(myId)) return rank;
        
        string[] allIds = PlayerDataStore.GetAllPlayerIds();
        foreach (var id in allIds)
        {
            if (id == myId) continue;
            PlayerDataStore.LoadBestStats(id, out int pScore, out int _);
            if (pScore > myScore) rank++;
        }
        return rank;
    }

    public void ResetGameplay()
    {
        // 重置前確保特效關閉
        if (breathFX != null) breathFX.StopEffect();
        
        Time.timeScale = 1.0f;
        Time.fixedDeltaTime = 0.02f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void GoToMainMenu()
    {
        if (breathFX != null) breathFX.StopEffect();
        
        Time.timeScale = 1.0f;
        if (TransitionManager.Instance != null)
            TransitionManager.Instance.LoadLevel(mainMenuSceneName);
        else
            SceneManager.LoadScene(mainMenuSceneName);
    }

    IEnumerator AnimateSwitchToRanking()
    {
        isWaitForRank = false; 
        float duration = 0.5f; 

        if (resultUI != null)
            yield return StartCoroutine(resultUI.FadeOutBoardRoutine(duration));

        if (rankingUIPanel != null)
        {
            rankingUIPanel.SetActive(true);
            if (rankingPopulator != null) rankingPopulator.RefreshRanking();

            CanvasGroup rankCG = rankingUIPanel.GetComponent<CanvasGroup>();
            if (rankCG == null) rankCG = rankingUIPanel.AddComponent<CanvasGroup>();
            
            float t = 0;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / duration;
                rankCG.alpha = t;
                yield return null;
            }
            isViewingRank = true; 
        }
        else
        {
            GoToMainMenu();
        }
    }
}