using System.Collections.Generic;
using UnityEngine;

public class RopePatternDetector : MonoBehaviour
{
    // 兩隻 Joy-Con（自動找）
    private Joycon L, R;
    private List<Joycon> jcs;

    // ------- 和 JoyconHands 一樣的軸映射設定 -------
    public enum Axis { X, Y, Z }

    [Header("Axis Mapping (per hand) — 請填成和 JoyconHands 一樣")]
    public Axis leftHorizontal  = Axis.X;
    public Axis leftVertical    = Axis.Y;
    public bool leftInvertH     = false;
    public bool leftInvertV     = false;

    public Axis rightHorizontal = Axis.X;
    public Axis rightVertical   = Axis.Y;
    public bool rightInvertH    = false;
    public bool rightInvertV    = false;
    // ------------------------------------------------

    [Header("Filtering")]
    public float lowpassSpeed = 2f;     // 越大=平均越慢(更平穩)
    public float gravityComp  = 0.90f;  // 抵銷重力比例(0~1)

    [Header("Thresholds")]
    public float vThreshold = 1.4f;     // 垂直觸發門檻
    public float hThreshold = 0.9f;     // ↓ 降低，水平更易觸發
    public float slamThresholdV = 2.2f; // 重摔的垂直門檻

    [Header("Timing (seconds)")]
    public float minInterval = 0.12f;   // 同手連兩次最短間隔(去抖)
    public float syncWindow  = 0.18f;   // ↑ 放寬，雙手「同時」時間窗
    public float altWindow   = 0.35f;   // 交替節奏時間窗
    public float holdTime    = 0.35f;   // 模式維持時間

    [Header("Horizontal detection tweaks")]
    public float horizVHMinRatio = 1.3f; // H 必須 >= V*ratio 才算主要是水平

    public enum HorizontalSignMode { SameSign, OppositeSign, Either }
    [Header("Horizontal sign mode")]
    public HorizontalSignMode horizontalSignMode = HorizontalSignMode.OppositeSign; // 戰繩建議 OppositeSign

    // 狀態輸出
    public enum Pattern { Idle, AlternatingVertical, HorizontalWave, VerticalSlam }
    public Pattern Current { get; private set; } = Pattern.Idle;
    public float Force { get; private set; }    // 近瞬間強度
    public float Cadence { get; private set; }  // 1 秒滑窗次數

    // 內部
    private Vector3 lpL, lpR; // lowpass
    private float lastSwingL_V, lastSwingR_V, lastSwingL_H, lastSwingR_H;
    private float lastPatternTime;
    private readonly Queue<float> swingTimes = new Queue<float>();

    // HUD
    [Header("HUD")]
    public bool showHud = true;
    public int hudFontSize = 16;
    public float hudX = 10f;
    public float hudY = 10f;
    private string hudL = "", hudR = "", hudC = "";

    void Start()
    {
        jcs = JoyconManager.Instance.j;
        if (jcs == null || jcs.Count == 0)
        {
            Debug.Log("❌ 沒偵測到 Joy-Con");
            enabled = false;
            return;
        }
        foreach (var jc in jcs) { if (jc.isLeft) L = jc; else R = jc; }
        if (L != null) L.debug_type = Joycon.DebugType.NONE;
        if (R != null) R.debug_type = Joycon.DebugType.NONE;
        Debug.Log($"🎮 左手:{(L!=null)} 右手:{(R!=null)}");
    }

    void Update()
    {
        if (L == null || R == null) return;

        // 1) 取 H / V（高通去重力 + 軸映射與反向，與 JoyconHands 保持一致）
        var (HL, VL) = GetHV_Mapped(L, ref lpL, leftHorizontal,  leftVertical,  leftInvertH,  leftInvertV);
        var (HR, VR) = GetHV_Mapped(R, ref lpR, rightHorizontal, rightVertical, rightInvertH, rightInvertV);

        // 2) 強度
        Force = Mathf.Sqrt((HL*HL + VL*VL + HR*HR + VR*VR) * 0.5f);

        // 3) 事件偵測（門檻 + 去抖）
        bool L_v_up   = VL >  vThreshold && Time.time - lastSwingL_V > minInterval;
        bool L_v_down = VL < -vThreshold && Time.time - lastSwingL_V > minInterval;
        bool R_v_up   = VR >  vThreshold && Time.time - lastSwingR_V > minInterval;
        bool R_v_down = VR < -vThreshold && Time.time - lastSwingR_V > minInterval;

        bool L_h_pos  = HL >  hThreshold && Time.time - lastSwingL_H > minInterval;
        bool L_h_neg  = HL < -hThreshold && Time.time - lastSwingL_H > minInterval;
        bool R_h_pos  = HR >  hThreshold && Time.time - lastSwingR_H > minInterval;
        bool R_h_neg  = HR < -hThreshold && Time.time - lastSwingR_H > minInterval;

        // 4) Cadence（1 秒滑窗次數）
        if (L_v_up||L_v_down||R_v_up||R_v_down||L_h_pos||L_h_neg||R_h_pos||R_h_neg)
        {
            swingTimes.Enqueue(Time.time);
            while (swingTimes.Count>0 && Time.time - swingTimes.Peek() > 1f) swingTimes.Dequeue();
            Cadence = swingTimes.Count;
        }

        // 更新時間戳
        if (L_v_up||L_v_down) lastSwingL_V = Time.time;
        if (R_v_up||R_v_down) lastSwingR_V = Time.time;
        if (L_h_pos||L_h_neg) lastSwingL_H = Time.time;
        if (R_h_pos||R_h_neg) lastSwingR_H = Time.time;

        // 5) 三種模式（優先序：重摔 > 水平 > 交替）
        bool slam =
            (Mathf.Abs(VL) > slamThresholdV && Mathf.Abs(VR) > slamThresholdV &&
             Mathf.Abs(Time.time - lastSwingL_V) <= syncWindow &&
             Mathf.Abs(Time.time - lastSwingR_V) <= syncWindow &&
             Mathf.Sign(VL) == Mathf.Sign(VR));

        // —— 加強版水平判斷（含號向模式） —— //
        bool leftMostlyH  = Mathf.Abs(HL) > hThreshold && Mathf.Abs(HL) > Mathf.Abs(VL) * horizVHMinRatio;
        bool rightMostlyH = Mathf.Abs(HR) > hThreshold && Mathf.Abs(HR) > Mathf.Abs(VR) * horizVHMinRatio;

        bool signOK = true;
        if (horizontalSignMode == HorizontalSignMode.SameSign)
            signOK = Mathf.Sign(HL) == Mathf.Sign(HR);
        else if (horizontalSignMode == HorizontalSignMode.OppositeSign)
            signOK = Mathf.Sign(HL) == -Mathf.Sign(HR);

        bool nearSimul = Mathf.Abs(Time.time - lastSwingL_H) <= syncWindow &&
                         Mathf.Abs(Time.time - lastSwingR_H) <= syncWindow;

        bool horizSameDir = leftMostlyH && rightMostlyH && signOK && nearSimul;
        // ———————————————————————— //

        bool alternating =
            (((L_v_up && R_v_down) || (L_v_down && R_v_up)) ||
             (Mathf.Sign(VL) == -Mathf.Sign(VR) && Mathf.Abs(VL)>vThreshold && Mathf.Abs(VR)>vThreshold)) &&
            (Mathf.Abs(Time.time - lastSwingL_V) <= altWindow || Mathf.Abs(Time.time - lastSwingR_V) <= altWindow);

        Pattern newPattern = Pattern.Idle;
        if (slam)              newPattern = Pattern.VerticalSlam;
        else if (horizSameDir) newPattern = Pattern.HorizontalWave;
        else if (alternating)  newPattern = Pattern.AlternatingVertical;

        if (newPattern != Pattern.Idle && newPattern != Current)
        {
            Current = newPattern;
            lastPatternTime = Time.time;
            Debug.Log($"▶ 姿勢：{GetPatternName(Current)}");
        }
        else
        {
            if (newPattern == Pattern.Idle && Time.time - lastPatternTime > holdTime)
                Current = Pattern.Idle;
        }

        // 6) HUD 字串（中文）
        hudL = $"左手  水平H:{HL,6:F2}  垂直V:{VL,6:F2}";
        hudR = $"右手  水平H:{HR,6:F2}  垂直V:{VR,6:F2}";
        hudC = $"姿勢：{GetPatternName(Current)}    力道：{Force:F2}    節奏：{Cadence:F1}";
    }

    // —— 依設定軸映射/反向來取得 H/V，並做高通去重力 ——
    (float H, float V) GetHV_Mapped(Joycon jc, ref Vector3 lp, Axis hAxis, Axis vAxis, bool invH, bool invV)
    {
        Vector3 a = jc.GetAccel();
        lp = Vector3.Lerp(lp, a, 1f - Mathf.Exp(-lowpassSpeed * Time.deltaTime)); // 高通低通混合
        Vector3 aHP = a - lp * gravityComp; // 去掉重力慢漂移

        float H = SelectAxis(aHP, hAxis) * (invH ? -1f : 1f);
        float V = SelectAxis(aHP, vAxis) * (invV ? -1f : 1f);
        return (H, V);
    }

    float SelectAxis(Vector3 v, Axis axis)
    {
        switch (axis)
        {
            case Axis.X: return v.x;
            case Axis.Y: return v.y;
            default:     return v.z;
        }
    }

    // 英文枚舉 → 中文名稱
    string GetPatternName(Pattern p)
    {
        switch (p)
        {
            case Pattern.Idle:               return "靜止中";
            case Pattern.AlternatingVertical:return "交替揮繩";
            case Pattern.HorizontalWave:     return "水平甩繩";
            case Pattern.VerticalSlam:       return "重摔揮繩";
            default:                         return "未知";
        }
    }

    // HUD
    void OnGUI()
    {
        if (!showHud) return;
        var oldColor = GUI.color;
        var oldSize  = GUI.skin.label.fontSize;
        GUI.color = Color.white;
        GUI.skin.label.fontSize = hudFontSize;

        GUI.Label(new Rect(hudX, hudY +  0, 720, 24), hudL);
        GUI.Label(new Rect(hudX, hudY + 24, 720, 24), hudR);
        GUI.Label(new Rect(hudX, hudY + 48, 900, 24), hudC);

        GUI.skin.label.fontSize = oldSize;
        GUI.color = oldColor;
    }
}
