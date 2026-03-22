using UnityEngine;

public class TutorialCountdown : MonoBehaviour
{
    void Start()
    {
        Countdown.gameStarted = true;
        Debug.Log("[TutorialCountdown] gameStarted = true");
    }
}