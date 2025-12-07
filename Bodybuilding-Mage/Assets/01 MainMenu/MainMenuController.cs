using UnityEngine;
using MaskTransitions;   // ★ 一定要加這行，才能看到 TransitionManager

public class MainMenuController : MonoBehaviour
{
    public void StartGame()
    {
        // 用 TransitionManager 播動畫 + 載入 Story 場景
        TransitionManager.Instance.LoadLevel("Story");
        // "Story" 改成你實際的故事場景名稱
    }
}
