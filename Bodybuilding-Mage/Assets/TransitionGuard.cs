using UnityEngine;

public static class TransitionGuard
{
    public static bool IsSwitchingScene = false;

    public static void Begin()
    {
        IsSwitchingScene = true;
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
    }

    public static void End()
    {
        IsSwitchingScene = false;
    }
}