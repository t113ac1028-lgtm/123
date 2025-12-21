using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class RankingListPopulator : MonoBehaviour
{
    [Header("設定")]
    [Tooltip("Scroll View 裡面的 Content 物件")]
    public Transform contentTransform;
    
    [Header("樣式 Prefabs (交替顯示)")]
    [Tooltip("單數名次的樣式 (例如：第 1, 3, 5 名用藍色)")]
    public GameObject itemPrefabTypeA;
    [Tooltip("雙數名次的樣式 (例如：第 2, 4, 6 名用粉色)")]
    public GameObject itemPrefabTypeB;

    private class PlayerData
    {
        public string id;
        public int score;
        public int combo;
    }

    void Start()
    {
        RefreshRanking();
    }

    public void RefreshRanking()
    {
        // 1. 清空目前的列表
        foreach (Transform child in contentTransform)
        {
            Destroy(child.gameObject);
        }

        // 2. 抓取資料
        List<PlayerData> allPlayers = new List<PlayerData>();
        string[] allIds = PlayerDataStore.GetAllPlayerIds();

        foreach (string id in allIds)
        {
            PlayerDataStore.LoadBestStats(id, out int pScore, out int pCombo);
            allPlayers.Add(new PlayerData { id = id, score = pScore, combo = pCombo });
        }

        // 3. 排序 (分數高 -> Combo 高)
        var sortedPlayers = allPlayers
            .OrderByDescending(p => p.score)
            .ThenByDescending(p => p.combo)
            .ToList();

        // 4. 生成列表
        for (int i = 0; i < sortedPlayers.Count; i++)
        {
            // ★ 修改：奇偶數切換 Prefab
            // 預設用 A
            GameObject prefabToUse = itemPrefabTypeA;
            
            // 如果有設定 B，且是雙數索引 (i=1, 3, 5... 代表排名 2, 4, 6)，就換成 B
            if (itemPrefabTypeB != null && (i % 2 != 0))
            {
                prefabToUse = itemPrefabTypeB;
            }

            // ★ 關鍵：加上 false 參數，防止 UI 被壓扁變形
            GameObject newItem = Instantiate(prefabToUse, contentTransform, false);
            
            RankingItem itemScript = newItem.GetComponent<RankingItem>();
            if (itemScript != null)
            {
                // 填入資料 (排名是 i + 1)
                itemScript.SetData(i + 1, sortedPlayers[i].id, sortedPlayers[i].score, sortedPlayers[i].combo);
            }
        }
    }
}