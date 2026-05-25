using System.Collections.Generic;
using UnityEngine;

public class JoyconHands : MonoBehaviour
{
    public Transform leftHand;
    public Transform rightHand;

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
    public float moveScale = 50f;
    public float smooth = 12f;
    public float gravityComp = 0.90f;

    [Header("Direction Detect")]
    public float dirThreshold = 1.6f;
    public bool logDirections = false;

    private Joycon leftJC, rightJC;
    private List<Joycon> jcs;
    private Vector3 lBase, rBase, lShown, rShown;
    private Vector3 lLP, rLP;

    void Start()
    {
        if (leftHand)  { lBase = leftHand.localPosition;  lShown = lBase; }
        if (rightHand) { rBase = rightHand.localPosition; rShown = rBase; }

        if (JoyconManager.Instance == null) return;

        jcs = JoyconManager.Instance.j;

        foreach (var jc in jcs)
        {
            if (jc.isLeft)
                rightJC = jc;
            else
                leftJC = jc;
        }

        if (leftJC != null)  leftJC.debug_type  = Joycon.DebugType.NONE;
        if (rightJC != null) rightJC.debug_type = Joycon.DebugType.NONE;

        Debug.Log($"[JoyconHands] Found L:{(leftJC != null)} R:{(rightJC != null)}");
    }

    void Update()
    {
        if (leftJC != null && leftHand != null)
            UpdateOne(leftJC, leftHand, ref lShown, ref lLP, lBase, "L",
                leftHorizontal, leftVertical, leftInvertH, leftInvertV);

        if (rightJC != null && rightHand != null)
            UpdateOne(rightJC, rightHand, ref rShown, ref rLP, rBase, "R",
                rightHorizontal, rightVertical, rightInvertH, rightInvertV);

        if ((leftJC  != null && leftJC.GetButtonDown(Joycon.Button.PLUS)) ||
            (rightJC != null && rightJC.GetButtonDown(Joycon.Button.PLUS)))
        {
            if (leftHand)  { lBase = leftHand.localPosition;  lShown = lBase;  lLP = Vector3.zero; }
            if (rightHand) { rBase = rightHand.localPosition; rShown = rBase;  rLP = Vector3.zero; }
            Debug.Log("[JoyconHands] Recenter hands");
        }
    }

    void UpdateOne(Joycon jc, Transform hand, ref Vector3 shown, ref Vector3 lowpass, Vector3 basePos,
                   string tag, Axis hAxis, Axis vAxis, bool invH, bool invV)
    {
        Vector3 a = jc.GetAccel();

        lowpass = Vector3.Lerp(lowpass, a, 1f - Mathf.Exp(-2f * Time.deltaTime));
        Vector3 aHP = a - lowpass * gravityComp;

        float H = SelectAxis(aHP, hAxis) * (invH ? -1f : 1f);
        float V = SelectAxis(aHP, vAxis) * (invV ? -1f : 1f);

        Vector3 target = basePos + new Vector3(H, V, 0f) * moveScale;

        shown = Vector3.Lerp(shown, target, 1f - Mathf.Exp(-smooth * Time.deltaTime));
        hand.localPosition = shown;

        if (logDirections && V >  dirThreshold) Debug.Log($"{tag} Up");
        if (logDirections && V < -dirThreshold) Debug.Log($"{tag} Down");
        if (logDirections && H >  dirThreshold) Debug.Log($"{tag} Right");
        if (logDirections && H < -dirThreshold) Debug.Log($"{tag} Left");
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
