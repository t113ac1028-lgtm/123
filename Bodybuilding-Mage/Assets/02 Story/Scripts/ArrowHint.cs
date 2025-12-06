using UnityEngine;

public class ArrowBounce : MonoBehaviour
{
    public float amplitude = 20f;   // 上下位移的高度（UI 像素）
    public float speed     = 2f;    // 上下晃動的速度

    RectTransform rect;
    float startY;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        startY = rect.anchoredPosition.y;
    }

    void Update()
    {
        var pos = rect.anchoredPosition;
        pos.y = startY + Mathf.Sin(Time.time * speed) * amplitude;
        rect.anchoredPosition = pos;
    }
}
