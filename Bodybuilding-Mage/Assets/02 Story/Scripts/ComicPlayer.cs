using UnityEngine;

public class ComicSequence : MonoBehaviour
{
    [Header("依序放入每一張漫畫的 Panel or Image")]
    public GameObject[] pages;

    private int index = 0;

    private void Start()
    {
        // 只顯示第一張
        for (int i = 0; i < pages.Length; i++)
            pages[i].SetActive(i == 0);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ShowNext();
        }
    }

    void ShowNext()
    {
        // 關掉當前
        pages[index].SetActive(false);

        index++;

        // 如果還有下一張 → 顯示
        if (index < pages.Length)
        {
            pages[index].SetActive(true);
        }
        else
        {
            // 播放結束，可自由決定要做什麼
            Debug.Log("漫畫播放完畢");
            // gameObject.SetActive(false); // 或跳場景
        }
    }
}
