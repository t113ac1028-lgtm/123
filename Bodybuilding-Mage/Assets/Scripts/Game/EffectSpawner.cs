using UnityEngine;

public class EffectSpawner : MonoBehaviour
{
    [Header("Refs")]
    public AltAndSlamCoordinator coordinator;
    public Transform leftHand;
    public Transform rightHand;

    [Header("Prefabs")]
    public GameObject slashPrefab;   // 單手下揮用的劍氣
    public GameObject slamPrefab;    // 重摔用的爆發

    [Header("Slash")]
    public float slashForwardSpeed = 8f;
    public float slashLife = 0.6f;
    public Vector3 slashLocalOffset = new Vector3(0, -0.05f, 0.1f); // 手前方一點點
    public enum SlashForwardMode { CameraForward, HandForward, WorldForwardZ }
    public SlashForwardMode slashForwardMode = SlashForwardMode.CameraForward;

    [Header("Slam")]
    public float slamLife = 0.9f;
    public Vector3 slamOffset = Vector3.zero;

    Camera _cam;

    void Awake()
    {
        _cam = Camera.main;
    }

    void OnEnable()
    {
        if (coordinator != null)
        {
            coordinator.OnAlternateSwing.AddListener(SpawnSlash);
            coordinator.OnHeavySlam.AddListener(SpawnSlam);
        }
    }

    void OnDisable()
    {
        if (coordinator != null)
        {
            coordinator.OnAlternateSwing.RemoveListener(SpawnSlash);
            coordinator.OnHeavySlam.RemoveListener(SpawnSlam);
        }
    }

    void SpawnSlash(string who, float strength)
    {
        if (slashPrefab == null) return;

        Transform hand = (who == "Left") ? leftHand : rightHand;
        if (hand == null) return;

        Vector3 pos = hand.TransformPoint(slashLocalOffset);
        Quaternion rot = Quaternion.identity;

        // 前進方向
        Vector3 fwd = Vector3.forward;
        switch (slashForwardMode)
        {
            case SlashForwardMode.CameraForward:
                if (_cam != null) fwd = _cam.transform.forward;
                break;
            case SlashForwardMode.HandForward:
                fwd = hand.forward;
                break;
            case SlashForwardMode.WorldForwardZ:
                fwd = Vector3.forward;
                break;
        }

        GameObject fx = Instantiate(slashPrefab, pos, Quaternion.LookRotation(fwd, Vector3.up));
        float scale = Mathf.Lerp(0.9f, 1.8f, strength);
        fx.transform.localScale = Vector3.one * scale;

        // 若特效帶剛體則推一下；否則簡單移動腳本
        var rb = fx.GetComponent<Rigidbody>();
        if (rb != null)
            rb.velocity = fwd * (slashForwardSpeed * Mathf.Lerp(1f, 1.7f, strength));
        else
            fx.AddComponent<SimpleMover>().Init(fwd * (slashForwardSpeed * Mathf.Lerp(1f, 1.7f, strength)));

        Destroy(fx, slashLife);
    }

    void SpawnSlam(float strength)
    {
        if (slamPrefab == null || leftHand == null || rightHand == null) return;

        Vector3 mid = (leftHand.position + rightHand.position) * 0.5f + slamOffset;
        GameObject fx = Instantiate(slamPrefab, mid, Quaternion.identity);
        float scale = Mathf.Lerp(1.2f, 2.4f, strength);
        fx.transform.localScale = Vector3.one * scale;
        Destroy(fx, slamLife);
    }

    // 簡單位移用（若你的特效沒有剛體）
    class SimpleMover : MonoBehaviour
    {
        Vector3 v;
        public void Init(Vector3 vel) => v = vel;
        void Update() => transform.position += v * Time.deltaTime;
    }
}
