using UnityEngine;
using UnityEngine.SceneManagement;

public class GamePlayController : MonoBehaviour
{

    [Header("Debug / Reset")]
    [Tooltip("是否啟用鍵盤快速重開（Play 模式方便測試用）")]
    [SerializeField] private bool enableKeyboardReset = true;

    [Tooltip("按下哪個鍵重開當前遊戲場景")]
    [SerializeField] private KeyCode resetKey = KeyCode.R;

    public static GamePlayController Instance { get; private set; }

    [Header("Core Systems")]
    [SerializeField] private DamageCalculator damage;
    [SerializeField] private ComboCounter combo;

    [Header("Full Screen Effect")]
    [SerializeField] private BreathFXController breathFX; // ← 新增：控制呼吸特效

    [Header("Match Settings")]
    [Tooltip("現在只當作參考，實際時間由 GameTimer 控制")]
    //[SerializeField] private float matchDuration = 30f;
    [SerializeField] private bool autoStartOnSceneLoad = false;

    private bool playing;

    public static bool IsPlaying => Instance != null && Instance.playing;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
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
        // 如果不是自動開始，就等 Countdown 結束後才開局
        if (!playing && !autoStartOnSceneLoad)
        {
            if (Countdown.gameStarted)
            {
                StartMatch();
            }
        }
                if (enableKeyboardReset && Input.GetKeyDown(resetKey))
        {
            ResetGameplay();
        }

    }

    private void StartMatch()
    {
        if (playing) return;

        playing = true;

        if (combo != null)
            combo.Clear();

        // ★ 開局時啟動呼吸特效的「15 秒後淡入」流程
        if (breathFX != null)
            breathFX.StartEffect();

        Debug.Log("[GamePlay] Match started");
    }

    /// <summary>
    /// 給 GameTimer 在時間到的時候呼叫
    /// </summary>
    public void OnTimerFinished()
    {
        if (!playing) return;

        Debug.Log("[GamePlay] Timer finished, ending match.");
        EndMatch();
    }

        // 重新載入目前這個 Gameplay 場景
    public void ResetGameplay()
    {
        // ✅ 這裡只 reload 場景，不碰 PlayerDataStore，
        //    所以玩家之前輸入的 ID 會留在記憶體 / PlayerPrefs 裡。
        var scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.name);
    }


    private void EndMatch()
    {
        playing = false;

        // ★ 結束時關掉特效（淡出）
        if (breathFX != null)
            breathFX.StopEffect();

        int finalScore = damage != null ? damage.Total() : 0;
        int maxCombo   = combo  != null ? combo.Max  : 0;

        // 丟給 ResultData
        ResultData.lastScore    = finalScore;
        ResultData.lastMaxCombo = maxCombo;

        // 更新這個玩家的最佳紀錄
        PlayerDataStore.UpdateBestForCurrentRun();

        try
{
    if (GoogleSheetDataHandler.Instance != null)
    {
        GoogleSheetDataHandler.Instance.UploadScore(finalScore);
    }
    else
    {
        Debug.LogWarning("[Score] GoogleSheetDataHandler.Instance is null -> skip upload.");
    }
}
catch (System.Exception e)
{
    Debug.LogWarning("[Score] UploadScore failed: " + e.Message);
}

        // ✅ 不管上傳成功與否，都要進結算畫面
        SceneManager.LoadScene("ResultScene");

    }
}
