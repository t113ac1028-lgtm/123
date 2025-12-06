using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ScoreFade : MonoBehaviour
{
     public float lifeTime = 1.5f;      // 淡出的總時間
    private TMP_Text text;

    void Start()
    {
        text = GetComponent<TMP_Text>();
        StartCoroutine(FadeOut());
    }

    IEnumerator FadeOut()
    {
        float timer = 0f;
        Color c = text.color;

        while (timer < lifeTime)
        {
            timer += Time.deltaTime;
            float t = timer / lifeTime;

            // 讓 alpha 從 1 → 0
            c.a = Mathf.Lerp(1f, 0f, t);
            text.color = c;

            yield return null;
        }

        Destroy(gameObject);
    }
}
