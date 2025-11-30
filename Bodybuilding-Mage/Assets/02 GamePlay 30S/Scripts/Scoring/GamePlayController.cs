
using UnityEngine;
using UnityEngine.SceneManagement;

public class GamePlayController : MonoBehaviour
{
    [Header("Core Systems")]
    public DamageCalculator damage;
    public ComboCounter combo;

    [Header("Match Settings")]
    public float matchDuration = 30f;  // 這一局玩幾秒
    public bool autoStartOnSceneLoad = false; // 若你有倒數，就設成 false

    float timeLeft;
    bool playing;

    void Start()
    {
        ResetMatch();

        if (autoStartOnSceneLoad)
            StartMatch();
        // 如果你用 Countdown，那就在倒數結束時呼叫 StartMatch()
    }

    void Update()
    {
        if (!playing) return;

        timeLeft -= Time.deltaTime;
        if (timeLeft <= 0f)
        {
            timeLeft = 0f;
            EndMatch();
        }
    }

    // ====== 對外 API ======

    public void ResetMatch()
    {
        timeLeft = matchDuration;
        playing  = false;

        // 分數 / Combo 歸零
        if (damage) damage.ResetScore(); // 等一下幫你在 DamageCalculator 補這個
        if (combo)  combo.Clear();
    }

    public void StartMatch()
    {
        playing = true;
    }

    public void EndMatch()
    {
        if (!playing) return;
        playing = false;

        int finalScore = damage ? damage.Total() : 0;
        int maxCombo   = combo  ? combo.Max      : 0;

        // TODO: 把分數存起來，切到結算場景
        ResultData.lastScore   = finalScore;
        ResultData.lastMaxCombo = maxCombo;
        PlayerDataStore.UpdateBestForCurrentRun();

        SceneManager.LoadScene("ResultScene"); // 之後你做好的排行場景名字
    }

    public bool IsPlaying => playing;
    public float TimeLeft  => timeLeft;
}
