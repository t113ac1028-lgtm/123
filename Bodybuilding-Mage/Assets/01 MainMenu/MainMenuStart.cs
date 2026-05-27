using System.Collections;
using System;
using System.Text;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using MaskTransitions;

public class MainMenuStart : MonoBehaviour
{
    private const string SkipStoryToggleName = "SkipStoryToggle_Runtime";
    private static bool exhibitStabilityApplied;

    [Header("UI")]
    public GameObject playerIdRoot;
    public TMP_InputField playerIdInput;

    [Header("Scene")]
    public string storySceneName = "Story";
    public string tutorialSceneName = "Tutorial GamePlay";
    public string gameplaySceneName = "GamePlay 30S program DEMO";

    [Header("Exhibit Flow")]
    [SerializeField] private bool createSkipStoryToggle = true;
    [SerializeField] private string skipStoryPrefsKey = "BodybuildingMage.SkipStory";

    private bool idShown = false;
    private Coroutine focusCoroutine;
    private Coroutine openInputCoroutine;
    private Toggle skipStoryToggle;
    private bool skipStoryEnabled;
    private float nextInputModeEnforceTime;

    private void OnEnable()
    {
        TransitionGuard.End();
        ApplyExhibitStabilitySettings();
        ShowInputForScan();
        EnsureSkipStoryToggle();
        RestartOpenInputRoutine();
    }

    private void Update()
    {
        if (playerIdRoot != null && !playerIdRoot.activeInHierarchy)
            ShowInputForScan(false);

        KeepIdInputReady();

        if (!idShown && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
            OnStartButtonPressed();
    }

    private void ResolveUI()
    {
        if (playerIdRoot != null && playerIdInput != null) return;

        var all = UnityEngine.Object.FindObjectsByType<Transform>(
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
        ShowInputForScan(false);
    }

    private void ShowInputForScan(bool clearText = true)
    {
        if (clearText && GoogleSheetDataHandler.Instance != null)
            GoogleSheetDataHandler.Instance.ShowInputField(false);

        ResolveUI();

        if (playerIdRoot != null)
        {
            playerIdRoot.SetActive(true);
            idShown = true;
        }

        if (playerIdInput == null)
        {
            Debug.LogWarning("[MainMenu] PlayerIDInput not found.");
            return;
        }

        if (clearText)
            playerIdInput.text = "";

        ExhibitInputGuard.ConfigureIdInput(playerIdInput);
        FocusInput();

        if (focusCoroutine != null)
            StopCoroutine(focusCoroutine);
        focusCoroutine = StartCoroutine(FocusInputNextFrame());
    }

    private void FocusInput()
    {
        if (playerIdInput == null) return;

        ExhibitInputGuard.ForceEnglishKeyboard();
        ExhibitInputGuard.ConfigureIdInput(playerIdInput);

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(playerIdInput.gameObject);

        playerIdInput.ActivateInputField();
        playerIdInput.Select();
    }

    private void KeepIdInputReady()
    {
        if (playerIdInput == null || !playerIdInput.isActiveAndEnabled) return;

        ExhibitInputGuard.NormalizeIdInput(playerIdInput);

        if (Time.unscaledTime < nextInputModeEnforceTime) return;

        ExhibitInputGuard.ForceEnglishKeyboard();
        nextInputModeEnforceTime = Time.unscaledTime + 0.5f;
    }

    private IEnumerator FocusInputNextFrame()
    {
        yield return null;

        FocusInput();
        focusCoroutine = null;
    }

    private void RestartOpenInputRoutine()
    {
        if (openInputCoroutine != null)
            StopCoroutine(openInputCoroutine);

        openInputCoroutine = StartCoroutine(OpenInputAfterSceneSettles());
    }

    private IEnumerator OpenInputAfterSceneSettles()
    {
        yield return null;
        EnsureSkipStoryToggle();
        ShowInputForScan(false);

        yield return new WaitForSecondsRealtime(0.25f);
        ShowInputForScan(false);

        yield return new WaitForSecondsRealtime(0.25f);
        ShowInputForScan(false);

        openInputCoroutine = null;
    }

    private void EnsureSkipStoryToggle()
    {
        if (!createSkipStoryToggle)
        {
            CleanupSkipStoryToggles(null);
            return;
        }

        Canvas canvas = FindMainSceneCanvas();
        if (canvas == null) return;

        skipStoryEnabled = PlayerPrefs.GetInt(skipStoryPrefsKey, 0) == 1;
        Toggle existingToggle = CleanupSkipStoryToggles(canvas);

        if (existingToggle != null)
        {
            BindSkipStoryToggle(existingToggle);
            return;
        }

        GameObject root = new GameObject(SkipStoryToggleName, typeof(RectTransform), typeof(Toggle), typeof(Image));
        root.transform.SetParent(canvas.transform, false);
        root.transform.SetAsLastSibling();

        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0f, 0f);
        rootRect.anchorMax = new Vector2(0f, 0f);
        rootRect.pivot = new Vector2(0f, 0f);
        rootRect.anchoredPosition = new Vector2(28f, 28f);
        rootRect.sizeDelta = new Vector2(44f, 44f);

        Image hitArea = root.GetComponent<Image>();
        hitArea.color = new Color(0f, 0f, 0f, 0f);

        GameObject box = new GameObject("Box", typeof(RectTransform), typeof(Image));
        box.transform.SetParent(root.transform, false);
        RectTransform boxRect = box.GetComponent<RectTransform>();
        boxRect.anchorMin = new Vector2(0.5f, 0.5f);
        boxRect.anchorMax = new Vector2(0.5f, 0.5f);
        boxRect.pivot = new Vector2(0.5f, 0.5f);
        boxRect.anchoredPosition = Vector2.zero;
        boxRect.sizeDelta = new Vector2(30f, 30f);
        Image boxImage = box.GetComponent<Image>();
        boxImage.color = new Color(1f, 1f, 1f, 0.92f);

        GameObject check = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
        check.transform.SetParent(box.transform, false);
        RectTransform checkRect = check.GetComponent<RectTransform>();
        checkRect.anchorMin = new Vector2(0.5f, 0.5f);
        checkRect.anchorMax = new Vector2(0.5f, 0.5f);
        checkRect.pivot = new Vector2(0.5f, 0.5f);
        checkRect.anchoredPosition = Vector2.zero;
        checkRect.sizeDelta = new Vector2(20f, 20f);
        Image checkImage = check.GetComponent<Image>();
        checkImage.color = new Color(0.08f, 0.95f, 0.9f, 1f);

        skipStoryToggle = root.GetComponent<Toggle>();
        skipStoryToggle.targetGraphic = boxImage;
        skipStoryToggle.graphic = checkImage;
        BindSkipStoryToggle(skipStoryToggle);
    }

    private Toggle CleanupSkipStoryToggles(Canvas targetCanvas)
    {
        Toggle keep = null;
        RectTransform[] rects = UnityEngine.Object.FindObjectsByType<RectTransform>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (var rect in rects)
        {
            if (rect == null || rect.name != SkipStoryToggleName)
                continue;

            bool isValidTarget = targetCanvas != null &&
                                 rect.gameObject.scene == targetCanvas.gameObject.scene &&
                                 rect.parent == targetCanvas.transform;
            Toggle candidate = rect.GetComponent<Toggle>();

            if (isValidTarget && candidate != null && keep == null)
            {
                keep = candidate;
                continue;
            }

            Destroy(rect.gameObject);
        }

        skipStoryToggle = keep;
        return keep;
    }

    private void BindSkipStoryToggle(Toggle toggle)
    {
        if (toggle == null) return;

        skipStoryToggle = toggle;
        skipStoryToggle.gameObject.SetActive(true);
        skipStoryToggle.transform.SetAsLastSibling();
        skipStoryToggle.onValueChanged.RemoveListener(SetSkipStoryEnabled);
        skipStoryToggle.SetIsOnWithoutNotify(skipStoryEnabled);
        skipStoryToggle.onValueChanged.AddListener(SetSkipStoryEnabled);
    }

    private Canvas FindMainSceneCanvas()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        Canvas[] canvases = UnityEngine.Object.FindObjectsByType<Canvas>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (var canvas in canvases)
        {
            if (IsPreferredMainSceneCanvas(canvas, activeScene))
                return canvas;
        }

        foreach (var canvas in canvases)
        {
            if (IsAcceptableMainSceneCanvas(canvas, activeScene))
                return canvas;
        }

        return null;
    }

    private bool IsPreferredMainSceneCanvas(Canvas canvas, Scene activeScene)
    {
        return IsAcceptableMainSceneCanvas(canvas, activeScene) &&
               canvas.isRootCanvas &&
               canvas.transform.parent == null;
    }

    private bool IsAcceptableMainSceneCanvas(Canvas canvas, Scene activeScene)
    {
        if (canvas == null || canvas.gameObject.scene != activeScene)
            return false;

        Transform root = canvas.transform.root;
        if (root == null) return true;

        string rootName = root.name;
        return rootName != "GoogleSheetService" &&
               rootName != "transitionManager" &&
               rootName != "TransitionManager";
    }

    private void ApplyExhibitStabilitySettings()
    {
        if (exhibitStabilityApplied) return;

        Application.targetFrameRate = 60;
        exhibitStabilityApplied = true;
    }

    private void SetSkipStoryEnabled(bool value)
    {
        skipStoryEnabled = value;
        PlayerPrefs.SetInt(skipStoryPrefsKey, value ? 1 : 0);
        PlayerPrefs.Save();
        FocusInput();
    }

    private bool ShouldSkipStory()
    {
        if (skipStoryToggle != null)
        {
            skipStoryEnabled = skipStoryToggle.isOn;
            return skipStoryEnabled;
        }

        skipStoryEnabled = PlayerPrefs.GetInt(skipStoryPrefsKey, 0) == 1;
        return skipStoryEnabled;
    }

    public void StartGameAfterId(string id)
    {
        id = (id ?? "").Trim();
        if (string.IsNullOrEmpty(id)) return;

        ResultData.playerId = id;
        PlayerDataStore.LoadBestStats(id, out ResultData.bestScore, out ResultData.bestMaxCombo);

        if (!TransitionGuard.TryBegin()) return;

        string targetScene = ShouldSkipStory() ? tutorialSceneName : storySceneName;

        if (TransitionManager.Instance != null)
            TransitionManager.Instance.LoadLevel(targetScene);
        else
            SceneManager.LoadScene(targetScene);
    }
}

public static class ExhibitInputGuard
{
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    private const uint KlfActivate = 0x00000001;
    private const uint KlfSetForProcess = 0x00000100;

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr LoadKeyboardLayout(string layoutId, uint flags);

    [DllImport("user32.dll")]
    private static extern IntPtr ActivateKeyboardLayout(IntPtr keyboardLayout, uint flags);
#endif

    public static void ForceEnglishKeyboard()
    {
        Input.imeCompositionMode = IMECompositionMode.Off;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        IntPtr englishLayout = LoadKeyboardLayout("00000409", KlfActivate | KlfSetForProcess);
        if (englishLayout != IntPtr.Zero)
            ActivateKeyboardLayout(englishLayout, KlfActivate);
#endif
    }

    public static void ConfigureIdInput(TMP_InputField inputField)
    {
        if (inputField == null) return;

        inputField.contentType = TMP_InputField.ContentType.Alphanumeric;
        inputField.characterValidation = TMP_InputField.CharacterValidation.Alphanumeric;
        inputField.lineType = TMP_InputField.LineType.SingleLine;
        inputField.richText = false;
    }

    public static void NormalizeIdInput(TMP_InputField inputField)
    {
        if (inputField == null) return;

        string clean = KeepAsciiLettersAndDigits(inputField.text);
        if (clean == inputField.text) return;

        inputField.SetTextWithoutNotify(clean);
        inputField.caretPosition = clean.Length;
        inputField.selectionAnchorPosition = clean.Length;
        inputField.selectionFocusPosition = clean.Length;
    }

    public static string KeepAsciiLettersAndDigits(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        StringBuilder builder = new StringBuilder(value.Length);
        foreach (char c in value)
        {
            if ((c >= '0' && c <= '9') ||
                (c >= 'A' && c <= 'Z') ||
                (c >= 'a' && c <= 'z'))
            {
                builder.Append(c);
            }
        }

        return builder.ToString();
    }
}
