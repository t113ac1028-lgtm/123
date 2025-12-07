using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GameScoreExplode : MonoBehaviour
{
    [Header("要炸出來的分數 prefab")]
    public GameObject scorePrefab;          // 拖 ScoreTextPrefab 進來

    [Header("分數爆炸出生點")]
    public Transform spawnPoint;            // 通常設成玩家頭上、或某個 UI 定位點

    [Header("爆炸參數")]
    public int spawnCount = 1;              // 一次噴幾個
    public float explosionForce = 5f;       // 推多大力
    public float spreadRadius = 0.3f;       // 出生點亂數半徑

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    /// <summary>
    /// 由外部呼叫：把這次造成的傷害 dmg 顯示出來
    /// </summary>
    
    public void ExplodeScore(int dmg)
    {
        if (scorePrefab == null || spawnPoint == null)
        {
            Debug.LogWarning("GameScoreExplode：scorePrefab 或 spawnPoint 沒指定");
            return;
        }

        for (int i = 0; i < spawnCount; i++)
        {
            // 在出生點附近一點點隨機位置生成
            Vector3 offset = (Vector3)(Random.insideUnitCircle * spreadRadius);
            GameObject obj = Instantiate(
                scorePrefab,
                spawnPoint.position + offset,
                Quaternion.identity,
                spawnPoint.parent       // 讓它跟原本 Canvas 同一層級
            );

            // 設定文字內容（假設 prefab 上有 TMP_Text + ScoreFade）
            TMP_Text tmp = obj.GetComponentInChildren<TMP_Text>();
            if (tmp != null)
            {
                tmp.text = $"+{dmg}";
            }

            // 給它一個隨機方向的力（往上噴）
            Rigidbody2D rb = obj.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                float baseAngle = 90f;                      // 往上
                float randomOffset = Random.Range(-45f, 45f);
                float angle = baseAngle + randomOffset;

                Vector2 dir = new Vector2(
                    Mathf.Cos(angle * Mathf.Deg2Rad),
                    Mathf.Sin(angle * Mathf.Deg2Rad)
                ).normalized;

                rb.AddForce(dir * explosionForce, ForceMode2D.Impulse);
                rb.AddTorque(Random.Range(-100f, 100f));
            }
        }
    }
}
