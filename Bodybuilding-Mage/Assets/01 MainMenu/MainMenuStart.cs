using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using TMPro;
using MaskTransitions;

public class MainMenuStart : MonoBehaviour
{
    [Header("UI")]
    public GameObject playerIdRoot;
    public TMP_InputField playerIdInput;

    [Header("Scene")]
    public string storySceneName = "Story";
    public string gameplaySceneName = "GamePlay 30S program DEMO";

    private bool idShown = false;
    private Coroutine focusCoroutine;
    private Coroutine openInputCoroutine;

    private void OnEnable()
    {
        TransitionGuard.End();
        ShowInputForScan();
        RestartOpenInputRoutine();
    }

    private void Update()
    {
        if (playerIdRoot != null && !playerIdRoot.activeInHierarchy)
            ShowInputForScan(false);

        if (!idShown && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
            OnStartButtonPressed();
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

        FocusInput();

        if (focusCoroutine != null)
            StopCoroutine(focusCoroutine);
        focusCoroutine = StartCoroutine(FocusInputNextFrame());
    }

    private void FocusInput()
    {
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(playerIdInput.gameObject);

        playerIdInput.ActivateInputField();
        playerIdInput.Select();
    }

    private IEnumerator FocusInputNextFrame()
    {
        yield return null;

        if (playerIdInput != null)
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
        ShowInputForScan(false);

        yield return new WaitForSecondsRealtime(0.25f);
        ShowInputForScan(false);

        yield return new WaitForSecondsRealtime(0.25f);
        ShowInputForScan(false);

        openInputCoroutine = null;
    }

    public void StartGameAfterId(string id)
    {
        id = (id ?? "").Trim();
        if (string.IsNullOrEmpty(id)) return;

        ResultData.playerId = id;
        PlayerDataStore.LoadBestStats(id, out ResultData.bestScore, out ResultData.bestMaxCombo);

        if (!TransitionGuard.TryBegin()) return;

        if (TransitionManager.Instance != null)
            TransitionManager.Instance.LoadLevel(storySceneName);
        else
            SceneManager.LoadScene(storySceneName);
    }
}
