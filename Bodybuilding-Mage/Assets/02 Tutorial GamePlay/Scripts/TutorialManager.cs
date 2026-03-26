using System.Collections;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

public class TutorialManager : MonoBehaviour
{
    public enum TutorialPhase
    {
        None,
        IntroImage,
        LightAttackIntro,
        LightAttackPractice,
        HeavyReadyHint,
        HeavyAttackIntro,
        HeavyAttackPractice,
        Finish
    }

    [Header("Refs")]
    public TutorialPanelUI panelUI;
    public TutorialAltAndSlamCoordinator coordinator;
    public TutorialEffectSpawner spawner;
    public TutorialGamePlayController gameplay;
    public TutorialShieldController shieldController;

    [Header("場景切換")]
    public TutorialSceneLoader sceneLoader;

    [Header("完成後顯示圖片")]
    public GameObject finishImageObject;      // 你已經擺好的那張圖
    public AudioSource finishImageAudioSource;
    public AudioClip finishImageSfx;
    public float finishImageStayDuration = 1.5f;

    [Header("快捷鍵")]
    public bool enableReloadHotkey = true;
    public KeyCode reloadKey = KeyCode.R;
    public bool enableSkipHotkey = true;
    public KeyCode skipToNextSceneKey = KeyCode.Return;
    public KeyCode skipToNextSceneKeyAlt = KeyCode.KeypadEnter;

    [Header("前導圖片教學")]
    [TextArea] public string introImageText = "先熟悉一下基本動作與操作方式。";
    public Sprite introImageSprite;
    public float introImageStayDuration = 4f;

    [Header("第一段：輕擊教學")]
    [TextArea] public string lightIntroText = "上下交替揮動戰繩，釋放劍氣，擊破梅林的護盾！";
    public VideoClip lightIntroClip;
    public int lightAttackTargetCount = 10;

    [Header("重擊前提示（READY）")]
    [TextArea] public string heavyReadyText = "READY！重擊！";
    public float heavyReadyStayDuration = 4f;

    [Header("第二段：重擊教學")]
    [TextArea] public string heavyIntroText = "梅林破防了！雙手同時上下揮動，使出重擊，擊敗梅林吧！";
    public VideoClip heavyIntroClip;
    public int heavyAttackTargetCount = 3;

    [Header("練習中提示文字")]
    [TextArea] public string lightPracticeText = "試著完成輕擊動作！";
    [TextArea] public string heavyPracticeText = "試著完成重擊動作！";

    [Header("完成文字")]
    [TextArea] public string finishTextLine1 = "相信你已經學會戰鬥技巧了！";
    [TextArea] public string finishTextLine2 = "打敗梅林，成為最強的健美詠者吧！";

    [Header("時間設定")]
    public float panelFadeDuration = 0.35f;
    public float introStayDuration = 4.5f;
    public float shieldBreakDelay = 0.3f;
    public float finishLineStayDuration = 2.5f;

    [Header("是否在完成後保留結束訊息")]
    public bool keepFinishPanelVisible = true;

    public TutorialPhase currentPhase { get; private set; } = TutorialPhase.None;

    private int currentLightCount = 0;
    private int currentHeavyCount = 0;
    private bool canCountInput = false;
    private bool isBusyTransitioning = false;
    private bool isSceneChanging = false;

    private void Start()
    {
        if (finishImageObject != null)
            finishImageObject.SetActive(false);

        StartCoroutine(BeginTutorialRoutine());
    }

    private void Update()
    {
        HandleHotkeys();
    }

    private void HandleHotkeys()
    {
        if (isSceneChanging) return;

        if (enableReloadHotkey && Input.GetKeyDown(reloadKey))
        {
            ReloadCurrentScene();
            return;
        }

        if (enableSkipHotkey && (Input.GetKeyDown(skipToNextSceneKey) || Input.GetKeyDown(skipToNextSceneKeyAlt)))
        {
            LoadNextSceneImmediately();
        }
    }

    private void OnEnable()
    {
        if (coordinator != null)
        {
            coordinator.OnAlternateSwing.AddListener(OnAlternateSwing);
            coordinator.OnHeavySlam.AddListener(OnHeavySlam);

            if (spawner != null)
            {
                coordinator.OnAlternateSwing.AddListener(spawner.SpawnSwingByPhase);
                coordinator.OnHeavySlam.AddListener(spawner.SpawnSlamByPhase);
            }
        }
    }

    private void OnDisable()
    {
        if (coordinator != null)
        {
            coordinator.OnAlternateSwing.RemoveListener(OnAlternateSwing);
            coordinator.OnHeavySlam.RemoveListener(OnHeavySlam);

            if (spawner != null)
            {
                coordinator.OnAlternateSwing.RemoveListener(spawner.SpawnSwingByPhase);
                coordinator.OnHeavySlam.RemoveListener(spawner.SpawnSlamByPhase);
            }
        }
    }

    IEnumerator BeginTutorialRoutine()
    {
        canCountInput = false;
        isBusyTransitioning = true;

        SetSlamPhase(false);

        if (shieldController != null)
            shieldController.ResetShield();

        if (gameplay != null)
            gameplay.StartTutorialMatch();

        yield return StartCoroutine(PlayImageIntroRoutine());

        yield return StartCoroutine(PlayVideoIntroRoutine(
            TutorialPhase.LightAttackIntro,
            lightIntroText,
            lightIntroClip
        ));

        currentPhase = TutorialPhase.LightAttackPractice;
        currentLightCount = 0;
        canCountInput = true;
        isBusyTransitioning = false;

        if (panelUI != null)
        {
            panelUI.ShowPracticeMessage($"{lightPracticeText} ({currentLightCount}/{lightAttackTargetCount})");
            yield return StartCoroutine(panelUI.PopMessage());
        }

        Debug.Log("[TutorialManager] 進入第一段：輕擊練習");
    }

    IEnumerator PlayImageIntroRoutine()
    {
        currentPhase = TutorialPhase.IntroImage;

        if (gameplay != null)
            gameplay.StopTutorialMatch();

        if (panelUI != null)
        {
            panelUI.ShowInstant();
            panelUI.SetDimVisible(true);
            panelUI.SetVideoVisible(false);
            panelUI.SetReadyVisible(false);
            panelUI.SetImageVisible(true, true);

            if (introImageSprite != null)
                panelUI.SetIntroImage(introImageSprite);

            yield return StartCoroutine(panelUI.FadeIn(panelFadeDuration));
            panelUI.PlayTypewriterText(introImageText);
        }

        yield return new WaitForSecondsRealtime(introImageStayDuration);

        if (panelUI != null)
        {
            yield return StartCoroutine(panelUI.FadeOut(panelFadeDuration));
        }

        if (gameplay != null)
            gameplay.ResumeTutorialMatch();
    }

    IEnumerator PlayVideoIntroRoutine(TutorialPhase introPhase, string introText, VideoClip clip)
    {
        currentPhase = introPhase;

        if (gameplay != null)
            gameplay.StopTutorialMatch();

        if (panelUI != null)
        {
            panelUI.ShowInstant();
            panelUI.SetDimVisible(true);
            panelUI.SetReadyVisible(false);
            panelUI.SetImageVisible(false);
            panelUI.SetVideoVisible(true);

            if (clip != null)
                panelUI.SetVideo(clip, true);

            yield return StartCoroutine(panelUI.FadeIn(panelFadeDuration));
            panelUI.PlayTypewriterText(introText);
        }

        yield return new WaitForSecondsRealtime(introStayDuration);

        if (panelUI != null)
        {
            yield return StartCoroutine(panelUI.FadeOut(panelFadeDuration));
            panelUI.StopVideo();
        }

        if (gameplay != null)
            gameplay.ResumeTutorialMatch();
    }

    IEnumerator PlayHeavyReadyRoutine()
    {
        currentPhase = TutorialPhase.HeavyReadyHint;

        if (gameplay != null)
            gameplay.StopTutorialMatch();

        if (panelUI != null)
        {
            panelUI.ShowInstant();
            panelUI.SetDimVisible(true);
            panelUI.SetVideoVisible(false);
            panelUI.SetImageVisible(false);
            panelUI.SetReadyVisible(true);

            yield return StartCoroutine(panelUI.FadeIn(panelFadeDuration));
            panelUI.PlayTypewriterText(heavyReadyText);
        }

        yield return new WaitForSecondsRealtime(heavyReadyStayDuration);

        if (panelUI != null)
        {
            yield return StartCoroutine(panelUI.FadeOut(panelFadeDuration));
            panelUI.SetReadyVisible(false);
        }

        if (gameplay != null)
            gameplay.ResumeTutorialMatch();
    }

    IEnumerator PlayHeavyIntroRoutine()
    {
        canCountInput = false;
        isBusyTransitioning = true;

        SetSlamPhase(true);

        yield return StartCoroutine(PlayHeavyReadyRoutine());

        yield return StartCoroutine(PlayVideoIntroRoutine(
            TutorialPhase.HeavyAttackIntro,
            heavyIntroText,
            heavyIntroClip
        ));

        currentPhase = TutorialPhase.HeavyAttackPractice;
        currentHeavyCount = 0;
        canCountInput = true;
        isBusyTransitioning = false;

        if (panelUI != null)
        {
            panelUI.ShowPracticeMessage($"{heavyPracticeText} ({currentHeavyCount}/{heavyAttackTargetCount})");
            yield return StartCoroutine(panelUI.PopMessage());
        }

        Debug.Log("[TutorialManager] 進入第二段：重擊練習");
    }

    IEnumerator LightPartCompleteRoutine()
    {
        canCountInput = false;
        isBusyTransitioning = true;

        Debug.Log("[TutorialManager] 第一段完成，準備破盾");

        yield return new WaitForSecondsRealtime(shieldBreakDelay);

        if (shieldController != null)
            shieldController.BreakShield();

        yield return StartCoroutine(PlayHeavyIntroRoutine());
    }

    IEnumerator PlayFinishImageRoutine()
    {
        if (finishImageObject != null)
            finishImageObject.SetActive(true);

        if (finishImageAudioSource != null && finishImageSfx != null)
            finishImageAudioSource.PlayOneShot(finishImageSfx);

        yield return new WaitForSecondsRealtime(finishImageStayDuration);

        if (finishImageObject != null)
            finishImageObject.SetActive(false);
    }

    IEnumerator FinishTutorialRoutine()
    {
        canCountInput = false;
        isBusyTransitioning = true;
        currentPhase = TutorialPhase.Finish;

        if (gameplay != null)
            gameplay.FinishTutorial();

        if (panelUI != null)
        {
            panelUI.ShowInstant();
            panelUI.SetDimVisible(true);
            panelUI.SetReadyVisible(false);
            panelUI.SetImageVisible(false);
            panelUI.SetVideoVisible(false);

            yield return StartCoroutine(panelUI.FadeIn(panelFadeDuration));

            panelUI.PlayTypewriterText(finishTextLine1);
            while (panelUI.IsTyping)
                yield return null;

            yield return new WaitForSecondsRealtime(finishLineStayDuration);

            panelUI.PlayTypewriterText(finishTextLine2);
            while (panelUI.IsTyping)
                yield return null;

            yield return new WaitForSecondsRealtime(finishLineStayDuration);
        }

        Debug.Log("[TutorialManager] 教學完成");

        if (panelUI != null)
            panelUI.HideInstant();

        yield return StartCoroutine(PlayFinishImageRoutine());

        LoadNextSceneImmediately();
    }

    void SetSlamPhase(bool isSlam)
    {
        if (coordinator != null)
            coordinator.isSlamPhase = isSlam;

        if (spawner != null)
            spawner.isSlamPhase = isSlam;
    }

    void OnAlternateSwing(string hand, float strength)
    {
        if (!canCountInput) return;
        if (isBusyTransitioning) return;
        if (currentPhase != TutorialPhase.LightAttackPractice) return;

        currentLightCount++;
        Debug.Log($"[TutorialManager] Light Count = {currentLightCount}/{lightAttackTargetCount}");

        if (panelUI != null)
        {
            panelUI.ShowPracticeMessage($"{lightPracticeText} ({currentLightCount}/{lightAttackTargetCount})");
            StartCoroutine(panelUI.PopMessage(0.14f, 1.05f));
        }

        if (currentLightCount >= lightAttackTargetCount)
        {
            StartCoroutine(LightPartCompleteRoutine());
        }
    }

    void OnHeavySlam(float strength)
    {
        if (!canCountInput) return;
        if (isBusyTransitioning) return;
        if (currentPhase != TutorialPhase.HeavyAttackPractice) return;

        currentHeavyCount++;
        Debug.Log($"[TutorialManager] Heavy Count = {currentHeavyCount}/{heavyAttackTargetCount}");

        if (panelUI != null)
        {
            panelUI.ShowPracticeMessage($"{heavyPracticeText} ({currentHeavyCount}/{heavyAttackTargetCount})");
            StartCoroutine(panelUI.PopMessage(0.14f, 1.05f));
        }

        if (currentHeavyCount >= heavyAttackTargetCount)
        {
            StartCoroutine(FinishTutorialRoutine());
        }
    }

    public void ReloadCurrentScene()
    {
        if (isSceneChanging) return;
        isSceneChanging = true;

        Time.timeScale = 1f;
        string currentSceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentSceneName);
    }

    public void LoadNextSceneImmediately()
    {
        if (isSceneChanging) return;
        isSceneChanging = true;

        Time.timeScale = 1f;

        if (sceneLoader != null)
        {
            sceneLoader.useDelay = false;
            sceneLoader.LoadNextScene();
        }
        else
        {
            Debug.LogWarning("[TutorialManager] sceneLoader 沒有指定，無法跳到下一個場景");
        }
    }

    public int GetCurrentLightCount() => currentLightCount;
    public int GetCurrentHeavyCount() => currentHeavyCount;
    public bool CanCountInput() => canCountInput;

    public void ForceStartLightPractice()
    {
        StopAllCoroutines();

        SetSlamPhase(false);
        currentPhase = TutorialPhase.LightAttackPractice;
        currentLightCount = 0;
        canCountInput = true;
        isBusyTransitioning = false;

        if (panelUI != null)
            panelUI.ShowPracticeMessage($"{lightPracticeText} ({currentLightCount}/{lightAttackTargetCount})");

        if (gameplay != null)
            gameplay.ResumeTutorialMatch();
    }

    public void ForceStartHeavyPractice()
    {
        StopAllCoroutines();

        SetSlamPhase(true);
        currentPhase = TutorialPhase.HeavyAttackPractice;
        currentHeavyCount = 0;
        canCountInput = true;
        isBusyTransitioning = false;

        if (panelUI != null)
            panelUI.ShowPracticeMessage($"{heavyPracticeText} ({currentHeavyCount}/{heavyAttackTargetCount})");

        if (gameplay != null)
            gameplay.ResumeTutorialMatch();
    }
}