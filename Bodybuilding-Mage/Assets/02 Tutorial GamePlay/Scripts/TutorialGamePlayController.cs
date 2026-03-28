using UnityEngine;
using UnityEngine.Events;

public class TutorialGamePlayController : MonoBehaviour
{
    public static TutorialGamePlayController Instance { get; private set; }

    [Header("狀態")]
    [SerializeField] private bool autoStartOnAwake = true;
    [SerializeField] private bool playing = false;
    [SerializeField] private bool tutorialFinished = false;
    [SerializeField] private bool pauseGameWhenFinish = false;

    [Header("可選事件")]
    public UnityEvent onTutorialStart;
    public UnityEvent onTutorialStop;
    public UnityEvent onTutorialFinish;

    public static bool IsPlaying => Instance != null && Instance.playing;
    public static bool IsFinished => Instance != null && Instance.tutorialFinished;

    public bool Playing => playing;
    public bool TutorialFinished => tutorialFinished;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Time.timeScale = 1f;
    }

    private void Start()
    {
        // 教學場景一進來就直接算開始
        Countdown.gameStarted = true;

        if (autoStartOnAwake)
        {
            StartTutorialMatch();
        }
    }

    /// <summary>
    /// 開始教學戰鬥流程
    /// 會讓教學模式進入 Playing 狀態
    /// </summary>
    public void StartTutorialMatch()
    {
        TransitionGuard.End();

        Countdown.gameStarted = true;
        playing = true;
        tutorialFinished = false;

        Debug.Log("[TutorialGamePlayController] Tutorial Match Started");
        onTutorialStart?.Invoke();
    }

    /// <summary>
    /// 暫停/停止教學中的戰鬥判定
    /// 例如播片、過場、切階段時可用
    /// </summary>
    public void StopTutorialMatch()
    {
        playing = false;

        Debug.Log("[TutorialGamePlayController] Tutorial Match Stopped");
        onTutorialStop?.Invoke();
    }

    /// <summary>
    /// 恢復教學戰鬥判定
    /// </summary>
    public void ResumeTutorialMatch()
    {
        TransitionGuard.End();
        
        Countdown.gameStarted = true;
        playing = true;

        Debug.Log("[TutorialGamePlayController] Tutorial Match Resumed");
    }

    /// <summary>
    /// 教學正式完成
    /// </summary>
    public void FinishTutorial()
    {
        playing = false;
        tutorialFinished = true;

        Debug.Log("[TutorialGamePlayController] Tutorial Finished");
        onTutorialFinish?.Invoke();

        if (pauseGameWhenFinish)
        {
            Time.timeScale = 0f;
        }
    }

    /// <summary>
    /// 重新開始教學
    /// </summary>
    public void RestartTutorial()
    {
        Time.timeScale = 1f;
        tutorialFinished = false;
        StartTutorialMatch();

        Debug.Log("[TutorialGamePlayController] Tutorial Restarted");
    }

    /// <summary>
    /// 強制設定是否可輸入/可戰鬥
    /// 給 TutorialManager 或影片播放控制用
    /// </summary>
    public void SetPlaying(bool value)
    {
        playing = value;

        Debug.Log($"[TutorialGamePlayController] SetPlaying = {value}");
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}