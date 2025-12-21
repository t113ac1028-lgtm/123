using UnityEngine;

public class HandheldCameraEffect : MonoBehaviour
{
    [Header("晃動設定 (微醺感)")]
    [Tooltip("是否啟用晃動")]
    public bool isActive = true;

    [Tooltip("晃動的速度 (數值越小越慢，建議 0.2 ~ 1.0)")]
    public float swaySpeed = 0.5f;

    [Tooltip("旋轉晃動的幅度 (角度)，建議 0.5 ~ 2.0，太大會暈")]
    public float rotationAmount = 1.0f;

    [Tooltip("位移晃動的幅度 (公尺)，建議 0.01 ~ 0.05，一點點就好")]
    public float positionAmount = 0.02f;

    // 用來記錄原本的座標，確保晃動是在原點附近
    private Vector3 initialPos;
    private Quaternion initialRot;
    
    // 用來累加時間的種子，讓動作不重複
    private float timeSeed;

    void Awake()
    {
        initialPos = transform.localPosition;
        initialRot = transform.localRotation;
        timeSeed = Random.Range(0f, 100f); // 隨機起點
    }

    // 給外部控制開關
    public void SetShaking(bool active)
    {
        isActive = active;
        if (!active)
        {
            // 關閉時，雖然不需要強制歸零 (因為導演會接手運鏡)，
            // 但如果想要平滑歸零可以寫 Lerp，這邊為了效能直接停住即可。
        }
    }

    void Update()
    {
        if (!isActive) return;

        // 使用 Time.time 加上種子，乘上速度
        float t = (Time.time + timeSeed) * swaySpeed;

        // --- 1. 計算旋轉雜訊 (Perlin Noise) ---
        // PerlinNoise 回傳 0~1，我們減 0.5 讓他在 -0.5 ~ 0.5 之間擺盪
        float rotX = (Mathf.PerlinNoise(t, 0) - 0.5f) * 2f * rotationAmount; // 上下看
        float rotY = (Mathf.PerlinNoise(0, t) - 0.5f) * 2f * rotationAmount; // 左右看
        float rotZ = (Mathf.PerlinNoise(t, t) - 0.5f) * 2f * rotationAmount * 0.5f; // 微幅歪頭

        // 套用旋轉 (疊加在原本的旋轉上)
        transform.localRotation = initialRot * Quaternion.Euler(rotX, rotY, rotZ);

        // --- 2. 計算位移雜訊 (呼吸感) ---
        float posX = (Mathf.PerlinNoise(t * 0.8f, 10) - 0.5f) * 2f * positionAmount;
        float posY = (Mathf.PerlinNoise(10, t * 0.8f) - 0.5f) * 2f * positionAmount;

        // 套用位移
        transform.localPosition = initialPos + new Vector3(posX, posY, 0);
    }
}