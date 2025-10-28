using System.Collections.Generic;
using UnityEngine;

public class JoyconHands : MonoBehaviour
{
    public Transform leftHand;
    public Transform rightHand;

    // 讓你在 Inspector 選擇：把 Joy-Con 的哪一個軸 映射到 畫面水平/垂直
    public enum Axis { X, Y, Z }

    [Header("Axis Mapping (per hand)")]
    public Axis leftHorizontal  = Axis.X;
    public Axis leftVertical    = Axis.Y;
    public bool leftInvertH     = false;
    public bool leftInvertV     = false;

    public Axis rightHorizontal = Axis.X;
    public Axis rightVertical   = Axis.Y;
    public bool rightInvertH    = false;
    public bool rightInvertV    = false;

    [Header("Motion")]
    public float moveScale = 50f;     // 放大係數（調到你看得清楚）
    public float smooth = 12f;        // 指數平滑（越大越穩）
    public float gravityComp = 0.90f; // 0~1：抵銷掉長期的重力/偏移（0.9 很常用）

    [Header("Direction Detect")]
    public float dirThreshold = 1.6f; // 方向判斷門檻（g）

    private Joycon leftJC, rightJC;
    private List<Joycon> jcs;
    private Vector3 lBase, rBase, lShown, rShown;
    private Vector3 lLP, rLP; // 長期平均（做簡單高通）

    void Start()
    {
        jcs = JoyconManager.Instance.j;
        if (jcs == null || jcs.Count == 0) { Debug.Log("❌ 沒偵測到 Joy-Con"); enabled = false; return; }

        foreach (var jc in jcs) if (jc.isLeft) leftJC = jc; else rightJC = jc;
        if (leftJC != null)  leftJC.debug_type  = Joycon.DebugType.NONE;
        if (rightJC != null) rightJC.debug_type = Joycon.DebugType.NONE;


        if (leftHand)  { lBase = leftHand.localPosition;  lShown = lBase; }
        if (rightHand) { rBase = rightHand.localPosition; rShown = rBase; }

        Debug.Log($"🎮 Found L:{(leftJC!=null)} R:{(rightJC!=null)}");
    }

    void Update()
    {
        if (leftJC != null && leftHand != null)
        UpdateOne(leftJC, leftHand, ref lShown, ref lLP, lBase, "L",
              leftHorizontal, leftVertical, leftInvertH, leftInvertV);

        if (rightJC != null && rightHand != null)
        UpdateOne(rightJC, rightHand, ref rShown, ref rLP, rBase, "R",
              rightHorizontal, rightVertical, rightInvertH, rightInvertV);


        // 重新定錨（+鍵）
        if ((leftJC  != null && leftJC.GetButtonDown(Joycon.Button.PLUS)) ||
            (rightJC != null && rightJC.GetButtonDown(Joycon.Button.PLUS)))
        {
            if (leftHand)  { lBase = leftHand.localPosition;  lShown = lBase;  lLP = Vector3.zero; }
            if (rightHand) { rBase = rightHand.localPosition; rShown = rBase;  rLP = Vector3.zero; }
            Debug.Log("🔧 Recenter hands");
        }
    }

    void UpdateOne(Joycon jc, Transform hand, ref Vector3 shown, ref Vector3 lowpass, Vector3 basePos,
                   string tag, Axis hAxis, Axis vAxis, bool invH, bool invV)
    {
        // 1) 讀加速度（g）
        Vector3 a = jc.GetAccel();

        // 2) 簡單高通去重力：aHP = a - (低通平均 * gravityComp)
        lowpass = Vector3.Lerp(lowpass, a, 1f - Mathf.Exp(-2f * Time.deltaTime)); // 慢慢追隨的平均
        Vector3 aHP = a - lowpass * gravityComp;

        // 3) 依照設定抽出水平/垂直分量
        float H = SelectAxis(aHP, hAxis) * (invH ? -1f : 1f);
        float V = SelectAxis(aHP, vAxis) * (invV ? -1f : 1f);

        // 4) 位移目標（localPosition）
        Vector3 target = basePos + new Vector3(H, V, 0f) * moveScale;

        // 5) 平滑顯示
        shown = Vector3.Lerp(shown, target, 1f - Mathf.Exp(-smooth * Time.deltaTime));
        hand.localPosition = shown;

        // 6) 方向提示
        if (V >  dirThreshold) Debug.Log($"{tag} ⬆️ Up");
        if (V < -dirThreshold) Debug.Log($"{tag} ⬇️ Down");
        if (H >  dirThreshold) Debug.Log($"{tag} ➡️ Right");
        if (H < -dirThreshold) Debug.Log($"{tag} ⬅️ Left");
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
}
