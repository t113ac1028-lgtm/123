using UnityEngine;

public static class TransitionGuard
{
    public static bool IsSwitchingScene = false;
    private static float lastBeginRealtime = -999f;

    public static bool TryBegin(float minInterval = 1f)
    {
        float now = Time.realtimeSinceStartup;

        if (IsSwitchingScene)
            return false;

        if (now - lastBeginRealtime < minInterval)
            return false;

        Begin();
        return true;
    }

    public static void Begin()
    {
        IsSwitchingScene = true;
        lastBeginRealtime = Time.realtimeSinceStartup;
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
    }

    public static void End()
    {
        IsSwitchingScene = false;
    }
}
