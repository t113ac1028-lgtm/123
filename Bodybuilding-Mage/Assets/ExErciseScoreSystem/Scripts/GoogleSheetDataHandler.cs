using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using UnityEngine;
using Google.Apis.Sheets.v4;
using Google.Apis.Auth.OAuth2;
using System.IO;
using System;
using GData = Google.Apis.Sheets.v4.Data; 
using System.Linq;
using UnityEngine.Events;
using TMPro;

public class GoogleSheetDataHandler : MonoBehaviour
{
    static public GoogleSheetDataHandler Instance;
    
    [Header("Settings")]
    [SerializeField] private string credentialJsonFilePath;
    [SerializeField] public Group group; // 使用檔案最下方的 Enum
    [SerializeField] private UnityEvent OnPlayerIDEntered;

    [Header("!!!Do Not Touch!!!")]
    [SerializeField] private GameObject IDInputUI;
    [SerializeField] private TMP_InputField IDInputField;
    [SerializeField] private TextMeshProUGUI IDTextMesh;
    
    public string PlayerID {get;set;}

    private static readonly string sheetID = "1VM5JbEfk4_CFy0A1qfKJaFc1u3Nvm9ueXKm_pH5k0Dw";
    private static readonly string[] Scopes = { SheetsService.Scope.Spreadsheets };
    private byte UploadStatus = 0;
    private bool changeID = false;
    private static SheetsService service;

    public void ShowInputField(bool changingID = false)
    {
        changeID = changingID;
        if(IDInputUI != null) IDInputUI.SetActive(true);
        if(IDInputField != null) 
        {
            IDInputField.text = "";
            IDInputField.ActivateInputField();
        }
    }

    // ★ 處理讀卡機格式：只取最後 10 位數
    public void PlayerIDEntered() 
    {
        if (IDInputField == null) return;
        string rawInput = IDInputField.text.Trim();
        if (string.IsNullOrEmpty(rawInput)) return;

        // 不論長度，只取最後 10 位
        if (rawInput.Length >= 10)
            PlayerID = rawInput.Substring(rawInput.Length - 10);
        else
            PlayerID = rawInput;

        ResultData.playerId = PlayerID; // 同步給遊戲邏輯使用

        if (!changeID) OnPlayerIDEntered.Invoke();
        if (IDTextMesh != null) IDTextMesh.text = PlayerID;
        Debug.Log($"[讀卡成功] 原始輸入: {rawInput} | 處理後 ID: {PlayerID}");
    }

    // ★ 補回排行榜需要的資料獲取方法
    public List<IList<object>> GetScoreData() 
    {
        var data = TgetScoreboard().GetAwaiter().GetResult();
        return data != null ? data.ToList() : new List<IList<object>>();
    }

    private async Task<IList<IList<object>>> TgetScoreboard()
    {
        if (service == null) return null;
        try {
            var request = service.Spreadsheets.Values.Get(sheetID, group.ToString());
            var response = await request.ExecuteAsync().ConfigureAwait(false);
            return response?.Values ?? new List<IList<object>>();
        } catch (Exception ex) {
            Debug.LogError("讀取排行榜失敗: " + ex.Message);
            return null;
        }
    }

    public void UploadScore<T>(T score)
    {
        if (string.IsNullOrEmpty(PlayerID)) PlayerID = ResultData.playerId;
        Thread t = new Thread(() => { TuploadScore(score).GetAwaiter(); });
        StartCoroutine(showUploadStatus());
        t.Start();
    }

    async void Awake()
    {
        if(Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); return; }

        try {
            GoogleCredential credential;
            using(var stream = new FileStream(credentialJsonFilePath, FileMode.Open, FileAccess.Read))
            {
                credential = GoogleCredential.FromServiceAccountCredential(ServiceAccountCredential.FromServiceAccountData(stream)).CreateScoped(Scopes);
            }
            service = new SheetsService(new Google.Apis.Services.BaseClientService.Initializer() {
                HttpClientInitializer = credential,
                ApplicationName = group.ToString(),
            });
            await CheckAndCreateSheet();
        } catch (Exception ex) { Debug.LogError("初始化失敗: " + ex.Message); }
    }

    private async Task CheckAndCreateSheet()
    {
        if (service == null) return;
        var request = service.Spreadsheets.Get(sheetID);
        var response = await request.ExecuteAsync();
        bool sheetExists = response.Sheets.Any(s => s.Properties.Title == group.ToString());

        if (!sheetExists)
        {
            var addSheetRequest = new GData.AddSheetRequest { Properties = new GData.SheetProperties { Title = group.ToString() } };
            var batchUpdate = new GData.BatchUpdateSpreadsheetRequest { Requests = new List<GData.Request> { new GData.Request { AddSheet = addSheetRequest } } };
            await service.Spreadsheets.BatchUpdate(batchUpdate, sheetID).ExecuteAsync();
        }
    }

    private async Task TuploadScore<T>(T score)
    {
        float fscore = (score is int i) ? i : (score is float f ? f : 0f);
        try {
            var valueRange = new GData.ValueRange();
            var dataList = new List<object>() { DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), PlayerID, fscore.ToString("F2") };
            valueRange.Values = new List<IList<object>> { dataList };
            var request = service.Spreadsheets.Values.Append(valueRange, sheetID, group.ToString());
            request.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;
            await request.ExecuteAsync();
            UploadStatus = 1;
            Debug.Log($"[試算表] 上傳成功: {PlayerID} | 分數: {fscore}");
        } catch (Exception ex) {
            Debug.LogError("上傳失敗: " + ex.Message);
            UploadStatus = 2;
        }
    }

    private IEnumerator showUploadStatus()
    {
        UploadStatus = 0;
        float t = 3f;
        while(t > 0) {
            if(UploadStatus == 0) IDTextMesh.text = "Uploading...";
            else if(UploadStatus == 1) IDTextMesh.text = "Success!";
            else if(UploadStatus == 2) IDTextMesh.text = "Failed";
            t -= Time.deltaTime;
            yield return null;
        }
        IDTextMesh.text = string.IsNullOrEmpty(PlayerID) ? "<empty>" : PlayerID;
    }
}

public enum Group { TEST, Group1, Group2, Group3, Group4, Group5, Group6, Group7, Group8, Group9, Group10, Group11, Group12, Group13, Group14, Group15, Group16, Group17 }