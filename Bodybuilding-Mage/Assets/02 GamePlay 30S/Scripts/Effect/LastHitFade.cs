using UnityEngine;
using TMPro;

public class LastHitFade : MonoBehaviour
{
    public float hold = 0.1f;
    public float fade = 0.4f;
    TextMeshProUGUI t;
    float timer;

    void Awake(){ t = GetComponent<TextMeshProUGUI>(); if(t) t.alpha = 0f; }
    public void Show(string s){
        if(!t) return;
        t.text = s; timer = hold + fade; t.alpha = 1f;
    }
    void Update(){
        if(!t || timer<=0f) return;
        timer -= Time.deltaTime;
        if(timer < fade) t.alpha = Mathf.Clamp01(timer / fade);
    }
}
