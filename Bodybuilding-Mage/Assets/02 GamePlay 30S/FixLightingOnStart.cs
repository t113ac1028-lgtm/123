using UnityEngine;

public class LightingFix : MonoBehaviour
{
    void Awake()
    {
        // 強制 Unity 重新更新環境光設定
        RenderSettings.skybox.shader = Shader.Find(RenderSettings.skybox.shader.name);
        DynamicGI.UpdateEnvironment();
    }
}