using UnityEngine;

public class ComicSequence : MonoBehaviour
{
    [Header("依序放入每一張漫畫的 Panel or Image")]
    public GameObject[] pages;

    [Header("鍵盤操作設定")]
    [Tooltip("下一頁主要鍵")]
    public KeyCode nextKeyMain = KeyCode.Space;

    [Tooltip("下一頁副鍵（例如右方向鍵）")]
    public KeyCode nextKeyAlt = KeyCode.RightArrow;

    [Tooltip("上一頁鍵（例如左方向鍵）")]
    public KeyCode prevKey = KeyCode.LeftArrow;

    private int index = 0;

    private void Start()
    {
        // 只顯示第一張
        for (int i = 0; i < pages.Length; i++)
        {
            pages[i].SetActive(i == 0);
        }
    }

    private void Update()
    {
        // 下一頁：Space 或 右方向鍵
        if (Input.GetKeyDown(nextKeyMain) || Input.GetKeyDown(nextKeyAlt))
        {
            ShowNext();
        }

        // 上一頁：左方向鍵
        if (Input.GetKeyDown(prevKey))
        {
            ShowPrevious();
        }
    }

    // 下一張
    public void ShowNext()
    {
        // 已經是最後一張就不要再往後
        if (index >= pages.Length - 1)
        {
            Debug.Log("已經是最後一張漫畫");
            return;
        }

        // 關掉當前
        pages[index].SetActive(false);

        // 換到下一張
        index++;
        pages[index].SetActive(true);
    }

    // 上一張
    public void ShowPrevious()
    {
        // 已經是第一張就不要往前
        if (index <= 0)
        {
            Debug.Log("已經是第一張漫畫");
            return;
        }

        // 關掉當前
        pages[index].SetActive(false);

        // 回到上一張
        index--;
        pages[index].SetActive(true);
    }
}
