using System.Collections.Generic;
using UnityEngine;

public class JoyconTest : MonoBehaviour
{
    private List<Joycon> joycons;
    private Joycon joycon;
    private float ticker;

    void Start()
    {
        joycons = JoyconManager.Instance.j;
        if (joycons == null || joycons.Count < 1)
        {
            Debug.Log("❌ 沒有偵測到 Joy-Con");
            return;
        }

        joycon = joycons[0];
        // 開一些內部 Log 看資料是否有進來（佇列 Enqueue/Dequeue）
        joycon.debug_type = Joycon.DebugType.NONE;

        Debug.Log($"✅ Joy-Con 已連線 | isLeft={joycon.isLeft}");
    }

    void Update()
    {
        if (joycon == null) return;

        // 每 0.5 秒印一次狀態
        ticker += Time.deltaTime;
        if (ticker >= 0.5f)
        {
            ticker = 0f;
            Debug.Log($"[State] {joycon.state}");
        }

        // 按鍵測試：請按 L/R 肩鍵或十字鍵
        if (joycon.GetButtonDown(Joycon.Button.SHOULDER_1)) Debug.Log("Pressed L1/R1");
        if (joycon.GetButtonDown(Joycon.Button.SHOULDER_2)) Debug.Log("Pressed L2/R2");
        if (joycon.GetButtonDown(Joycon.Button.DPAD_UP))    Debug.Log("Pressed DPAD UP");

        // 感測值
        Vector3 accel = joycon.GetAccel();
        Vector3 gyro  = joycon.GetGyro();
        Debug.Log($"Accel:{accel}  Gyro:{gyro}");
    }
}
