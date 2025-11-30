using System.IO;
using UnityEngine;

public class SwingDataRecorder : MonoBehaviour
{
    [Header("References")]
    public AltAndSlamCoordinator coordinator;
    public DownSwingDetector leftDetector;
    public DownSwingDetector rightDetector;

    [Header("Recording")]
    public string fileName = "swing_log.csv";

    private StreamWriter writer;
    private float startTime;

    void Start()
    {
        startTime = Time.time;

        // 存在 Application.persistentDataPath 底下
        string path = Path.Combine(Application.persistentDataPath, fileName);
        writer = new StreamWriter(path, append: false);
        writer.WriteLine("time,event,hand,strength,downL,downR,spdL,spdR,horizL,horizR");
        writer.Flush();

        Debug.Log($"[SwingDataRecorder] Logging to: {path}");

        if (coordinator != null)
        {
            coordinator.OnAlternateSwing.AddListener(OnAltSwing);
            coordinator.OnHeavySlam.AddListener(OnSlam);
        }
    }

    void OnDestroy()
    {
        if (coordinator != null)
        {
            coordinator.OnAlternateSwing.RemoveListener(OnAltSwing);
            coordinator.OnHeavySlam.RemoveListener(OnSlam);
        }

        if (writer != null)
        {
            writer.Flush();
            writer.Close();
            writer = null;
        }
    }

    // 單手 Slash（交替揮）
    void OnAltSwing(string hand, float strength)
    {
        LogEvent("Slash", hand, strength);
    }

    // 雙手 Slam
    void OnSlam(float strength)
    {
        LogEvent("Slam", "Both", strength);
    }

    void LogEvent(string evt, string hand, float strength)
    {
        if (writer == null) return;

        Vector3 vL = leftDetector  ? leftDetector.Velocity  : Vector3.zero;
        Vector3 vR = rightDetector ? rightDetector.Velocity : Vector3.zero;

        float downL = Mathf.Max(0f, -vL.y);
        float downR = Mathf.Max(0f, -vR.y);

        float horizL = new Vector2(vL.x, vL.z).magnitude;
        float horizR = new Vector2(vR.x, vR.z).magnitude;

        float spdL = vL.magnitude;
        float spdR = vR.magnitude;

        float t = Time.time - startTime;

        writer.WriteLine($"{t:F3},{evt},{hand},{strength:F3},{downL:F3},{downR:F3},{spdL:F3},{spdR:F3},{horizL:F3},{horizR:F3}");
        writer.Flush();
    }
}
