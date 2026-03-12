using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TMPro;
using UnityEngine;

public enum SortOrder
{
    Ascending,
    Descending
}

public class ScoreboardDisplay : MonoBehaviour
{
    public SortOrder sortOrder = SortOrder.Descending;
    public int rankAmount = 5;
    [SerializeField] private TextMeshProUGUI textMesh;

    void Start()
    {
        UpdateScoreBoard();
    }

    public void UpdateScoreBoard() 
    {
        StartCoroutine(IUpdateScoreBoard());
    }

    private IEnumerator IUpdateScoreBoard() 
    {
        List<IList<object>> scoreList = new List<IList<object>>();
        bool isDone = false;

        Thread t = new Thread(() =>
        {
            // 現在這個方法已經補回 GoogleSheetDataHandler 了
            scoreList = GoogleSheetDataHandler.Instance.GetScoreData();
            isDone = true;
        });
        t.Start();

        yield return new WaitUntil(() => isDone);

        if (scoreList == null || scoreList.Count == 0)
        {
            textMesh.text = "No Data Found";
            yield break;
        }

        // 排序邏輯
        try {
            if(sortOrder == SortOrder.Descending)
            {
                scoreList = scoreList.OrderByDescending(r => float.Parse(r[2].ToString())).ToList();
            }
            else
            {
                scoreList = scoreList.OrderBy(r => float.Parse(r[2].ToString())).ToList();
            }
        } catch { Debug.LogWarning("Some score data format is invalid."); }

        string str = "";
        int amount = Mathf.Min(scoreList.Count, rankAmount);
        for(int i = 0; i < amount; i++)
        {
            // 顯示排名. ID | 分數
            str += $"{i+1}. {scoreList[i][1]} | {scoreList[i][2]}\n";
        }
        
        textMesh.text = str;
    }
}