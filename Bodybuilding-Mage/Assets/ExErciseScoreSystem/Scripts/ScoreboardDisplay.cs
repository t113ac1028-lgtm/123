using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
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
    // Start is called before the first frame update

    void Start()
    {
        UpdateScoreBoard();
    }

    // Update is called once per frame

    public void UpdateScoreBoard() //use this <-
    {
        StartCoroutine(IUpdateScoreBoard());
    }

    private IEnumerator IUpdateScoreBoard() //Get data, sort data and display on textmesh in coroutine
    {
        List<IList<object>> scoreList = new List<IList<object>>();
        Thread t = new Thread(() =>
        {
            scoreList = GoogleSheetDataHandler.Instance.GetScoreData();
        });
        t.Start();
        yield return new WaitUntil(() => t.IsAlive == false);
        if(sortOrder == SortOrder.Descending)
        {
            scoreList = scoreList.OrderByDescending(r=>float.Parse(r[2].ToString())).ToList();
        }
        else if(sortOrder == SortOrder.Ascending)
        {
            scoreList = scoreList.OrderBy(r=>float.Parse(r[2].ToString())).ToList();
        }
        foreach(IList<object> row in scoreList)
        {
            Debug.Log($"{row[0]} | {row[1]} | {row[2]}");
        }
        string str = "";
        int amount = (scoreList.Count() > rankAmount) ? rankAmount : scoreList.Count();
        for(int i = 0; i < amount; i++)
        {
            str += $"{i+1}. {scoreList[i][1]} | {scoreList[i][2]}\n"; //String format. If you want to change the look, edit this line
        }
        textMesh.text = str;
        yield break;
    }
}
