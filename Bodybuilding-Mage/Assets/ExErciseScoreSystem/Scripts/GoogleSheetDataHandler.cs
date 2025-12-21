using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using UnityEngine;
using Google.Apis.Sheets.v4;
using Google.Apis.Auth.OAuth2;
using System.IO;
using System;
using Google.Apis.Sheets.v4.Data;
using System.Linq;
using UnityEngine.Events;
using TMPro;


public class GoogleSheetDataHandler : MonoBehaviour
{
    static public GoogleSheetDataHandler Instance;
    [Header("Settings")]
    [SerializeField] private string credentialJsonFilePath;
    [SerializeField] public Group group;
    [SerializeField] private UnityEvent OnPlayerIDEntered;

    [Header("!!!Do Not Touch!!!")]
    [SerializeField] private GameObject IDInputUI;
    [SerializeField] private TMP_InputField IDInputField;
    [SerializeField] private TextMeshProUGUI IDTextMesh;
    
    public string PlayerID {get;set;}

    private static readonly string sheetID = "1yEwvS0Ol1dWWUqCm3RoTPN3qDOm2LGqTKUdx6LuGbC8";
  
    private static readonly string[] Scopes = { SheetsService.Scope.Spreadsheets };
    
    private byte UploadStatus = 0;
    private bool changeID = false;
    private static SheetsService service;

    
    public void ShowInputField(bool changingID = false) //Show ID inputfield
    {
        changeID = changingID;
        IDInputUI.SetActive(true);
        IDInputField.ActivateInputField();
    }

    public List<IList<object>> GetScoreData()  //Get all score data (unsorted). Recommend using ScoreboardDisplay.cs
    {
        var data = TgetScoreboard().GetAwaiter().GetResult();
        if(data == null)
        {
            return new List<IList<object>>();
        }
        return data.ToList();
    }

    public void UploadScore<T>(T score) //UploadScore. score type need to be int or float
    {
        Thread t = new Thread(() =>
        {
            TuploadScore(score).GetAwaiter();
        });
        StartCoroutine(showUploadStatus());
        t.Start();
    }

    public void PlayerIDEntered() //Run after Player Entered ID
    {
        if (!changeID)
        {
            OnPlayerIDEntered.Invoke();
        }
        if(PlayerID == "" || PlayerID == null)
        {
            IDTextMesh.text = "<empty>";
            return;
        }
        IDTextMesh.text = PlayerID;
    }


    async void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        GoogleCredential credential;
        
        using(var stream = new FileStream(credentialJsonFilePath, FileMode.Open, FileAccess.Read))
        {
            credential = GoogleCredential.FromServiceAccountCredential(ServiceAccountCredential.FromServiceAccountData(stream)).CreateScoped(Scopes);
        }
        service = new SheetsService(new Google.Apis.Services.BaseClientService.Initializer()
        {
            HttpClientInitializer = credential,
            ApplicationName = group.ToString(),
        });
        try{
            if (!await CheckSheet(group.ToString()))
            {
                var sheetRequest = new AddSheetRequest
                {
                    Properties = new SheetProperties()
                };
                sheetRequest.Properties.Title = group.ToString();
                BatchUpdateSpreadsheetRequest batchUpdateSpreadsheetRequest = new BatchUpdateSpreadsheetRequest();
                batchUpdateSpreadsheetRequest.Requests = new List<Request>
                {
                    new Request
                    {
                        AddSheet = sheetRequest,
                    }
                };
                var batchUpdateRequest = service.Spreadsheets.BatchUpdate(batchUpdateSpreadsheetRequest, sheetID);
                var response = await batchUpdateRequest.ExecuteAsync();
                if(response == null)
                {
                    Debug.LogError("Connection Error");
                    return;
                }
            }
        }
        catch(Exception ex)
        {
            Debug.LogError(ex);
            Debug.LogError($"Connection Error. Check your network environment");
        }
    }
    void Start()
    {
        ShowInputField();
    }
    private async Task<bool> CheckSheet(string sheetName)
    {
        var request = service.Spreadsheets.Get(sheetID);
        var response = await request.ExecuteAsync();
        if(response == null)
        {
            Debug.LogError("Connection Error");
            return false;
        }
        var sheets = response.Sheets;
        foreach(Sheet sheet in sheets)
        {
            if(sheet.Properties.Title == sheetName)
            {
                return true;
            }
        }
        return false;
    }
    public void TESTgetData()
    {
        var values = new List<IList<object>>();
        Thread t = new Thread(() =>
        {
            values = GetScoreData();
            foreach(List<object> row in values)
            {
                Debug.Log($"{row[0]}|{row[1]}|{row[2]}");
            }
        });
        t.Start();
    }
    public void TESTuploadScore()
    {
        UploadScore(200);
    }

    
    
    private async Task<IList<IList<object>>> TgetScoreboard()
    {
        Debug.Log("Fetching Score data");
        try{
            var request = service.Spreadsheets.Values.Get(sheetID, group.ToString());
            var response = await request.ExecuteAsync().ConfigureAwait(false);
            if(response == null)
            {
                Debug.LogError("Connection Error");
                return new List<IList<object>>();
            }
            var values = response.Values;
            if(values == null)
            {
                Debug.Log("No Data");
                Debug.Log("Get Scores Success");
                return new List<IList<object>>();
            }
            Debug.Log("Get Scores Success");
            return response.Values;
        }
        catch(Exception ex)
        {
            Debug.LogError(ex);
            Debug.LogError($"Connection Error. Check your network environment");
            return null;
        }

    }
    private async Task TuploadScore<T>(T score)
    {
        Debug.Log("Uploading Score");
        float fscore;
        if(typeof(T) == typeof(int))
        {
            fscore = (int)(object)score;
        }
        else if(typeof(T) == typeof(float))
        {
            fscore = (float)(object)score;
        }
        else
        {
            Debug.LogError("Score type error. Need to be int or float");
            UploadStatus = 2;
            IDTextMesh.text = PlayerID;
            return;
        }
        try{
            var valueRange = new ValueRange();
            var dataList = new List<object>() {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), PlayerID, fscore.ToString("F2")};
            valueRange.Values = new List<IList<object>> {dataList};
            var request = service.Spreadsheets.Values.Append(valueRange, sheetID, group.ToString());
            request.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;
            var response = await request.ExecuteAsync().ConfigureAwait(false);
            if(response == null)
            {
                Debug.LogError("Connection Error");
                UploadStatus = 2;
                return;
            }
            Debug.Log($"Score upload success: {dataList[0]}|{dataList[1]}|{dataList[2]}");
            UploadStatus = 1;
        }
        catch(Exception ex)
        {
            Debug.LogError(ex);
            Debug.LogError($"Connection Error. Check your network environment");
            UploadStatus = 2;
            return;
        }

    }

    private IEnumerator showUploadStatus()
    {
        UploadStatus = 0;
        float t = 3;
        while(t > 0)
        {
            if(UploadStatus == 0)
            {
                IDTextMesh.text = "Uploading Score";
            }
            else if(UploadStatus == 1)
            {
                IDTextMesh.text = "Suceess";
            }
            else if(UploadStatus == 2)
            {
                IDTextMesh.text = "Failed. Check console for more detail.";
            }
            t-=Time.deltaTime;
            yield return null;
        }
        if(PlayerID == "" || PlayerID == null)
        {
            IDTextMesh.text = "<empty>";
            yield break;
        }
        IDTextMesh.text = PlayerID;
        yield break;
    }
}
public enum Group
{
    TEST,
    Group1,
    Group2,
    Group3,
    Group4,
    Group5,
    Group6,
    Group7,
    Group8,
    Group9,
    Group10,
    Group11,
    Group12,
    Group13,
    Group14,
    Group15,
    Group16,
    Group17,
}