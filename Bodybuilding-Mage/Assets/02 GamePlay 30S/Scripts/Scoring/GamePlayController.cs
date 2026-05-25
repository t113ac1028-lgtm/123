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

    [Header("Result Flow")]
    [SerializeField] private bool showRankingAfterResult = false;
    [SerializeField] private float autoReturnToMainMenuDelay = 15f;

    private bool playing;
    private bool matchHandled = false; // ★ 核心修正：標記這局是否已經啟動過
    private bool isWaitForRank = false; 
    private bool isViewingRank = false; 
    private bool isReturningToMainMenu = false;
    private Coroutine autoReturnCoroutine;

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
            HandleResultAdvance();

        // 排行榜按 Enter 返回主選單
        if (isViewingRank && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
            GoToMainMenu();
    }

    private void StartMatch()
    {
        TransitionGuard.End();
        
        if (playing) return;
        playing = true;
        matchHandled = true; // ★ 鎖定，這局遊戲不會再重複呼叫 StartMatch
        
        isWaitForRank = false;
        isViewingRank = false;
        isReturningToMainMenu = false;
        StopAutoReturnCountdown();
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

        // 2. 清除空中飛彈（歸還 pool，避免 pool 殘留 dangling reference）
        ProjectileHoming[] remainingProjectiles = FindObjectsOfType<ProjectileHoming>();
        foreach (var p in remainingProjectiles)
        {
            if (p != null) p.ForceReturn();
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

    if (GoogleSheetDataHandler.Instance != null)
    {
        GoogleSheetDataHandler.Instance.UploadScore(finalScore);
    }
    else
    {
        Debug.LogWarning("[GamePlayController] GoogleSheetDataHandler.Instance 為 null，跳過上傳，但仍顯示結算畫面");
    }

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
        StartAutoReturnCountdown();
    }
    else
    {
        if (showRankingAfterResult)
            StartCoroutine(AnimateSwitchToRanking());
        else
            GoToMainMenu();
    }
}

    private void HandleResultAdvance()
    {
        if (showRankingAfterResult)
        {
            StopAutoReturnCountdown();
            StartCoroutine(AnimateSwitchToRanking());
        }
        else
        {
            GoToMainMenu();
        }
    }

    private void StartAutoReturnCountdown()
    {
        StopAutoReturnCountdown();

        if (autoReturnToMainMenuDelay <= 0f)
            return;

        autoReturnCoroutine = StartCoroutine(AutoReturnToMainMenuRoutine());
    }

    private void StopAutoReturnCountdown()
    {
        if (autoReturnCoroutine == null)
            return;

        StopCoroutine(autoReturnCoroutine);
        autoReturnCoroutine = null;
    }

    private IEnumerator AutoReturnToMainMenuRoutine()
    {
        yield return new WaitForSecondsRealtime(autoReturnToMainMenuDelay);
        autoReturnCoroutine = null;

        if (isWaitForRank && !isReturningToMainMenu)
            GoToMainMenu();
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
    if (!TransitionGuard.TryBegin()) return;

    // 停掉本物件所有協程（含 AnimateSwitchToRanking），防止和 scene reload 競爭
    StopAllCoroutines();
    autoReturnCoroutine = null;
    isWaitForRank  = false;
    isViewingRank  = false;
    isReturningToMainMenu = false;

    // 停掉 ending director（在另一個物件上跑，上面的 StopAllCoroutines 不會停它）
    if (endingDirector != null) endingDirector.CancelEnding();

    if (breathFX != null) breathFX.StopEffect();

    StartCoroutine(ResetGameplayRoutine());
}

private IEnumerator ResetGameplayRoutine()
{
    playing = false;

    // 先關閉高頻判定腳本，避免切場景瞬間還在吃 Joy-Con 資料
    EffectSpawner spawner = FindObjectOfType<EffectSpawner>();
    if (spawner != null) spawner.enabled = false;

    AltAndSlamCoordinator coordinator = FindObjectOfType<AltAndSlamCoordinator>();
    if (coordinator != null) coordinator.enabled = false;

    DownSwingDetector[] detectors = FindObjectsOfType<DownSwingDetector>();
    foreach (var d in detectors)
    {
        if (d != null) d.enabled = false;
    }

    JoyconHands[] joyHands = FindObjectsOfType<JoyconHands>();
    foreach (var jh in joyHands)
    {
        if (jh != null) jh.enabled = false;
    }

    // 清除在途飛彈（歸還 pool，避免 pool 在 OnDestroy 時還有 dangling active objects）
    ProjectileHoming[] inFlight = FindObjectsOfType<ProjectileHoming>();
    foreach (var p in inFlight)
    {
        if (p != null) p.ForceReturn();
    }

    Time.timeScale = 1.0f;
    Time.fixedDeltaTime = 0.02f;

    // 給系統一點點緩衝時間
    yield return new WaitForSecondsRealtime(0.15f);

    AsyncOperation op = SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().name);
    while (!op.isDone)
        yield return null;
}

    private void GoToMainMenu()
    {
        if (isReturningToMainMenu) return;
        if (!TransitionGuard.TryBegin()) return;

        isReturningToMainMenu = true;
        StopAutoReturnCountdown();

        if (breathFX != null) breathFX.StopEffect();
        
        Time.timeScale = 1.0f;
        if (TransitionManager.Instance != null)
            TransitionManager.Instance.LoadLevel(mainMenuSceneName);
        else
            SceneManager.LoadScene(mainMenuSceneName);
    }

    IEnumerator AnimateSwitchToRanking()
    {
        if (!showRankingAfterResult)
        {
            GoToMainMenu();
            yield break;
        }

        StopAutoReturnCountdown();
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
