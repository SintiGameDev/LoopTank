using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class GhostManager : MonoBehaviour
{
    public static GhostManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple GhostManager instances found! Destroying duplicate.");
            //Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    [Header("References")]
    public LapRecorder playerRecorder;
    public GameObject ghostPrefab;
    public Transform ghostParent; // optional für Ordnerstruktur

    public enum GhostMode { Off, LastLap, BestLap }

    [Header("Ghost Mode")]
    public GhostMode mode = GhostMode.LastLap;

    [Header("Limits")]
    public int maxGhosts = 2; // <-- Hier die maximale Anzahl definieren

    LapData lastLap;
    LapData bestLap;

    // Liste ALLER Ghost-Instanzen
    private List<GhostReplay> ghostInstances = new List<GhostReplay>();

    //Timer
    public Timer roundTimer; // Im Inspector zuweisen!
    private bool timerStarted = false; // Merkt, ob der Timer schon gestartet wurde
    private bool timerUiShown = false; // Verhindert mehrfaches Anzeigen der UI


    // Call vom Renn-Controller am Start/Ziellinie:
    public void OnLapStarted()
    {
        playerRecorder.BeginLap();

        // TIMER NUR BEI ERSTER RUNDE STARTEN
        if (!timerStarted && roundTimer != null)
        {
            roundTimer.StartTimer();
            timerStarted = true;
            timerUiShown = false; // falls z.B. Reset erlaubt ist
        }

        LapData toReplay = null;
        if (mode == GhostMode.LastLap) toReplay = lastLap;
        else if (mode == GhostMode.BestLap) toReplay = bestLap;

        // Nur wenn wir wirklich eine gespeicherte Runde haben
        if (toReplay != null)
        {
            // *** KLONE genau das LapData-Objekt, das dieser Ghost wiederholen soll! ***
            LapData ghostLap = Clone(toReplay);

            // Neue Ghost-Instanz erzeugen
            var go = Instantiate(
                ghostPrefab,
                playerRecorder.transform.position,
                playerRecorder.transform.rotation,
                ghostParent // kann null sein
            );
            var ghost = go.GetComponent<GhostReplay>();
            ghost.Play(ghostLap);

            // In Liste aufnehmen
            ghostInstances.Add(ghost);

            // Maximale Anzahl beachten: älteste löschen
            if (ghostInstances.Count > maxGhosts)
            {
                Destroy(ghostInstances[0].gameObject);
                ghostInstances.RemoveAt(0);
            }
        }
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

    // Deep Copy, damit alle Ghosts unabhängig laufen
    LapData Clone(LapData src)
    {
        var c = ScriptableObject.CreateInstance<LapData>();
        c.lapTime = src.lapTime;
        foreach (var f in src.frames)
            c.frames.Add(new LapFrame { t = f.t, pos = f.pos, rotZ = f.rotZ });
        return c;
    }

    // Zum Löschen aller Ghosts (z.B. beim Reset/Restart)
    public void ClearAllGhosts()
    {
        foreach (var ghost in ghostInstances)
            if (ghost != null) Destroy(ghost.gameObject);
        ghostInstances.Clear();
    }

    // Optional: gezieltes Entfernen eines Ghosts
    public void RemoveGhost(GhostReplay ghost)
    {
        if (ghostInstances.Contains(ghost))
        {
            Destroy(ghost.gameObject);
            ghostInstances.Remove(ghost);
        }
    }

    // Optional: UI-Buttons können das hier aufrufen
    public void SetMode(int m) { mode = (GhostMode)m; }

    // Für Zugriff von außen (z.B. Anzeige)
    public IReadOnlyList<GhostReplay> ActiveGhosts => ghostInstances;
}
