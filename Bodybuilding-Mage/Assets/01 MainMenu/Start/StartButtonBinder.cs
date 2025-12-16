using UnityEngine;
using UnityEngine.UI;

public class StartButtonBinder : MonoBehaviour
{
    private void Awake()
    {
        var btn = GetComponent<Button>();
        if (btn == null)
        {
            Debug.LogError("[StartButtonBinder] 這個物件沒有 Button 組件！");
            return;
        }

        // 每次進 MainMenu 都重新綁一次，避免回圈後變 Missing
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() =>
        {
            var mainMenu = FindObjectOfType<MainMenuStart>(true);
            if (mainMenu == null)
            {
                Debug.LogError("[StartButtonBinder] 場景裡找不到 MainMenuStart！");
                return;
            }
            Debug.Log("[StartButtonBinder] Click -> MainMenuStart.OnStartButtonPressed()");
            mainMenu.OnStartButtonPressed();
        });
    }
}
