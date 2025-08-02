using UnityEngine;

public class LapTrigger : MonoBehaviour
{
    public GhostManager ghostManager;
    public Transform startLine; // optional zum Resetten
    float lapStartTime;

    void Start()
    {
        lapStartTime = Time.time;
        ghostManager.OnLapStarted();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        float lapTime = Time.time - lapStartTime;
        ghostManager.OnLapFinished(lapTime);

        lapStartTime = Time.time;
        ghostManager.OnLapStarted();
    }
}
