using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class RankingItem : MonoBehaviour
{
    [Header("UI 元件 (請拖曳對應的物件)")]
    [Tooltip("顯示名次的文字")]
    public TextMeshProUGUI rankText;
    [Tooltip("顯示玩家名字的文字")]
    public TextMeshProUGUI nameText;
    [Tooltip("顯示分數的文字")]
    public TextMeshProUGUI scoreText;
    [Tooltip("顯示 Combo 的文字")]
    public TextMeshProUGUI comboText;

    // 設定資料的函式
    public void SetData(int rank, string playerName, int score, int maxCombo)
    {
        if (rankText) rankText.text = rank.ToString();
        if (nameText) nameText.text = playerName;
        if (scoreText) scoreText.text = score.ToString();
        if (comboText) comboText.text = maxCombo.ToString();

        // 這裡不再改顏色，完全保留你原本 Prefab 的樣子
    }
}