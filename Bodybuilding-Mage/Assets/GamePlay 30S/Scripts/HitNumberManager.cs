using UnityEngine;
using TMPro;
using System.Collections;

public class HitNumberManager : MonoBehaviour
{
    public RectTransform canvas;
    public GameObject textPrefab; // 內含 TextMeshProUGUI

    public void Spawn(Vector3 worldPos, int amount, Camera cam){
        if (!canvas || !textPrefab || !cam) return;
        var go  = GameObject.Instantiate(textPrefab, canvas);
        var txt = go.GetComponent<TextMeshProUGUI>();
        txt.text = amount.ToString();

        Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, worldPos);
        var rt = go.GetComponent<RectTransform>();
        rt.position = screen;

        // 簡單往上飄＋淡出
        go.AddComponent<MonoHook>().StartCoroutine(Fly(rt, txt));
    }

    IEnumerator Fly(RectTransform rt, TextMeshProUGUI txt){
        float t=0, dur=0.6f;
        Vector3 a = rt.position, b = a + Vector3.up * 120f;
        while (t<dur){
            t += Time.deltaTime;
            float k = t/dur;
            rt.position = Vector3.Lerp(a,b,k*k*(3-2*k));
            txt.alpha = 1f-k;
            yield return null;
        }
        Destroy(rt.gameObject);
    }

    // 小掛鉤讓協程能跑在臨時物件上
    class MonoHook : MonoBehaviour {}
}
