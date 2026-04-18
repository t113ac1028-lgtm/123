using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using MaskTransitions;    // ★ 如果你有用轉場插件請保留，沒有的話可刪除

[System.Serializable]
public class PageConfig
{
    public string pageName; 
    public GameObject panel;

    [Header("自動跳頁設定")]
    [Tooltip("如果大於 0，時間到會自動下一頁")]
    public float autoSkipDuration = 0f;

    [Tooltip("如果這頁有影片，是否在影片播完時自動下一頁？")]
    public bool autoSkipAfterVideo = false;
}

public class ComicSequence : MonoBehaviour
{
    [Header("頁面配置列表")]
    public List<PageConfig> pages = new List<PageConfig>();

    [Header("最後一頁結束後的設定")]
    [Tooltip("最後一頁結束後要進入的場景名稱")]
    public string gameplaySceneName = "GameplayScene";

    [Header("Loading 畫面")]
    [Tooltip("Loading 時顯示的整個面板 GameObject")]
    public GameObject loadingPanel;
    [Tooltip("進度條 Slider（value 0~1）")]
    public Slider loadingBar;

    [Header("鍵盤操作設定")]
    public KeyCode nextKeyMain = KeyCode.Space;
    public KeyCode nextKeyAlt = KeyCode.RightArrow;
    public KeyCode prevKey = KeyCode.LeftArrow;

    [Header("Joy-Con 晃動設定")]
    public List<int> joyconSkipPages = new List<int>();
    public float shakeThreshold = 15f;
    public float skipCooldown = 0.5f;
    public float shakeDelay = 1.0f;

    private static readonly WaitForSeconds WaitLoadingComplete = new(0.2f);

    private int index = 0;
    private float cooldownTimer = 0f;
    private float pageTimer = 0f; 
    private bool isPendingNext = false;
    private VideoPlayer currentPlayer; 

    private void Start()
    {
        if (pages == null || pages.Count == 0)
        {
            Debug.LogError("[Comic] 你的 Pages 清單是空的！請在 Inspector 重新拖入頁面。");
            return;
        }

        UpdatePageDisplay();
    }

    private void Update()
    {
        if (pages == null || pages.Count == 0) return;
        if (cooldownTimer > 0) cooldownTimer -= Time.deltaTime;
        if (isPendingNext) return;

        HandleAutoTimer();

        if (Input.GetKeyDown(nextKeyMain) || Input.GetKeyDown(nextKeyAlt))
        {
            ShowNext();
        }
        if (Input.GetKeyDown(prevKey))
        {
            ShowPrevious();
        }

        HandleJoyconShake();
    }

    private void HandleAutoTimer()
    {
        if (index >= pages.Count) return;

        float limit = pages[index].autoSkipDuration;
        if (limit > 0)
        {
            pageTimer += Time.deltaTime;
            if (pageTimer >= limit)
            {
                ShowNext();
            }
        }
    }

    private void UpdatePageDisplay()
    {
        if (index < 0 || index >= pages.Count) return;

        for (int i = 0; i < pages.Count; i++)
        {
            if (pages[i].panel != null)
                pages[i].panel.SetActive(i == index);
        }

        pageTimer = 0f; 
        SetupVideoListener();
    }

    private void SetupVideoListener()
    {
        if (currentPlayer != null)
        {
            currentPlayer.loopPointReached -= OnVideoFinished;
            currentPlayer = null;
        }

        var config = pages[index];
        if (config.panel != null)
        {
            currentPlayer = config.panel.GetComponentInChildren<VideoPlayer>();
            
            if (currentPlayer != null && config.autoSkipAfterVideo)
            {
                if (!currentPlayer.isLooping)
                {
                    currentPlayer.loopPointReached += OnVideoFinished;
                }
            }
        }
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        ShowNext();
    }

    private void HandleJoyconShake()
    {
        if (cooldownTimer > 0 || isPendingNext || !joyconSkipPages.Contains(index)) return;
        if (JoyconManager.Instance == null || JoyconManager.Instance.j == null || JoyconManager.Instance.j.Count == 0) return;

        foreach (var jc in JoyconManager.Instance.j)
        {
            if (jc.GetGyro().magnitude > shakeThreshold)
            {
                StartCoroutine(DelayedShowNext());
                cooldownTimer = skipCooldown + shakeDelay;
                break;
            }
        }
    }

    IEnumerator DelayedShowNext()
    {
        isPendingNext = true;
        yield return new WaitForSeconds(shakeDelay);
        ShowNext();
        isPendingNext = false;
    }

    public void ShowNext()
    {
        // ★ 核心邏輯：如果是最後一頁還按下一步，就進入遊戲
        if (index >= pages.Count - 1)
        {
            EnterGameplay();
            return;
        }

        index++;
        UpdatePageDisplay();
    }

    private void EnterGameplay()
    {
        Debug.Log("[Comic] 所有漫畫播放完畢，準備進入遊戲場景：" + gameplaySceneName);

        if (TransitionManager.Instance != null)
        {
            TransitionManager.Instance.LoadLevel(gameplaySceneName);
        }
        else
        {
            StartCoroutine(LoadGameScene());
        }
    }

    IEnumerator LoadGameScene()
    {
        if (loadingPanel != null) loadingPanel.SetActive(true);
        if (loadingBar != null) loadingBar.value = 0f;

        AsyncOperation op = SceneManager.LoadSceneAsync(gameplaySceneName);
        op.allowSceneActivation = false;

        while (op.progress < 0.9f)
        {
            float progress = op.progress / 0.9f;
            if (loadingBar != null) loadingBar.value = progress;
            yield return null;
        }

        if (loadingBar != null) loadingBar.value = 1f;
        yield return WaitLoadingComplete; // 讓進度條停在滿格短暫一下

        op.allowSceneActivation = true;
    }

    public void ShowPrevious()
    {
        if (index <= 0) return;
        index--;
        UpdatePageDisplay();
    }
}