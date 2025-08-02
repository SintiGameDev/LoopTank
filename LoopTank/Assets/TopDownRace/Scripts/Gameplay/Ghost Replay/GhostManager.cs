using UnityEngine;

// Zuständig für: Ghost-Prefab spawnen, "letzte" und "beste" Runde verwalten
public class GhostManager : MonoBehaviour
{
    [Header("References")]
    public LapRecorder playerRecorder;
    public GameObject ghostPrefab;

    public enum GhostMode { Off, LastLap, BestLap }

    [Header("Ghost Mode")]
    public GhostMode mode = GhostMode.LastLap;

    LapData lastLap;
    LapData bestLap;

    GhostReplay ghostInstance;

    void Start()
    {
        // Ghost instanzieren & anfangs verstecken
        ghostInstance = Instantiate(ghostPrefab).GetComponent<GhostReplay>();
        ghostInstance.Stop();
    }

    // Call vom Renn-Controller am Start/Ziellinie:
    public void OnLapStarted()
    {
        playerRecorder.BeginLap();

        // Ghost basierend auf Modus spielen
        if (mode == GhostMode.LastLap && lastLap != null) ghostInstance.Play(lastLap);
        else if (mode == GhostMode.BestLap && bestLap != null) ghostInstance.Play(bestLap);
        else ghostInstance.Stop();
    }

    // Call am Rundenende:
    public void OnLapFinished(float lapTime)
    {
        playerRecorder.EndLap(lapTime);

        // "Letzte Runde" übernehmen
        lastLap = Clone(playerRecorder.currentLap);

        // "Beste Runde" updaten
        if (bestLap == null || lapTime < bestLap.lapTime)
            bestLap = Clone(playerRecorder.currentLap);
    }

    // Einfacher Deep Copy, damit die ScriptableObjects unabhängig sind
    LapData Clone(LapData src)
    {
        var c = ScriptableObject.CreateInstance<LapData>();
        c.lapTime = src.lapTime;
        foreach (var f in src.frames)
            c.frames.Add(new LapFrame { t = f.t, pos = f.pos, rotZ = f.rotZ });
        return c;
    }

    // Optional: UI-Buttons können das hier aufrufen
    public void SetMode(int m) { mode = (GhostMode)m; }
}
