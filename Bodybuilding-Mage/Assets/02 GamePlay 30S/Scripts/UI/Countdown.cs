using System.Collections;
using UnityEngine;
using TMPro;

public class Countdown : MonoBehaviour
{
    public TextMeshProUGUI countdownText;
    public TextMeshProUGUI startText;

    public static bool gameStarted = false;  // 全域開關

    void Start()
    {
        gameStarted = false;                 // 一開始先關閉
        StartCoroutine(CountdownRoutine());
    }

    IEnumerator CountdownRoutine()
    {
        countdownText.gameObject.SetActive(true);

        for (int i = 3; i > 0; i--)
        {
            countdownText.text = i.ToString();
            yield return new WaitForSeconds(1f);
        }

        countdownText.gameObject.SetActive(false);
        startText.gameObject.SetActive(true);
        startText.text = "START!";
        yield return new WaitForSeconds(0.7f);

        startText.gameObject.SetActive(false);

        // 🔥 正式開始遊戲
        gameStarted = true;
    }
}
