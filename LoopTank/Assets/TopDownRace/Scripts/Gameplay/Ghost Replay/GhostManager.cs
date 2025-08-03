using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    public int maxGhosts = 2; // Maximale Anzahl definieren

    LapData lastLap;
    LapData bestLap;

    // Liste ALLER Ghost-Instanzen
    private List<GhostReplay> ghostInstances = new List<GhostReplay>();

    // Timer
    public Timer roundTimer; // Im Inspector zuweisen!
    private bool timerStarted = false; // Merkt, ob der Timer schon gestartet wurde
    private bool timerUiShown = true; // Verhindert mehrfaches Anzeigen der UI

    // Call vom Renn-Controller am Start/Ziellinie:
    public void OnLapStarted()
    {
        playerRecorder.BeginLap();

        LapData toReplay = null;
        if (mode == GhostMode.LastLap) toReplay = lastLap;
        else if (mode == GhostMode.BestLap) toReplay = bestLap;

        //if (toReplay != null)
        //{
        //    // Ghost nach 5 Sekunden verzögert starten:
        //    StartCoroutine(SpawnAndActivateGhostDelayed(Clone(toReplay), 5f));
        //}
    }

    // Die Coroutine, die den Ghost nach delaySeconds startet und Sprite kurz blinken lässt
    private IEnumerator SpawnAndActivateGhostDelayed(LapData ghostLap, float delaySeconds)
    {
        // Ghost-Objekt direkt instanziieren (sichtbar, aber bewegt sich noch nicht)
        var go = Instantiate(
            ghostPrefab,
            playerRecorder.transform.position,
            playerRecorder.transform.rotation,
            ghostParent // kann null sein
        );
        var ghost = go.GetComponent<GhostReplay>();

        // Sprite-Kind suchen und erstmal deaktivieren (für sicheres Blinken)
        //var spriteTf = go.transform.Find("SpriteRenderer");
        //var spriteTf = GetComponent<SpriteRenderer>();
        //var spriteTf = ghost.GetComponentInChildren<SpriteRenderer>(); //funzt für oben
        var spriteTf = ghost.GetComponentsInChildren<SpriteRenderer>();
        foreach (var item in spriteTf)
        {
            GameObject spriteObj = item ? item.gameObject : null;
            if (spriteObj != null)
                spriteObj.SetActive(true);
        }


        // CollisionIgnorer-Kind suchen (rekursiv) und für 5 Sekunden aktivieren
        var collisionIgnorer = FindDeepChild(go.transform, "CollisionIgnorer");
        if (collisionIgnorer != null)
        {
            collisionIgnorer.gameObject.SetActive(true);
            StartCoroutine(DisableAfterSeconds(collisionIgnorer.gameObject, 1f, ghost));
        }

        // In Liste aufnehmen (optional: kann auch erst nach Play passieren)
        ghostInstances.Add(ghost);

        // Maximale Anzahl beachten: älteste löschen
        if (ghostInstances.Count > maxGhosts)
        {
            Destroy(ghostInstances[0].gameObject);
            ghostInstances.RemoveAt(0);
        }

        // Jetzt erst warten!
        yield return new WaitForSeconds(delaySeconds);

        // Jetzt die Ghost-Bewegung starten!
        if (ghost != null)
            ghost.Play(ghostLap);

        foreach (var item in spriteTf)
        {
            //GameObject spriteObj = item ? item.gameObject : null;
            ////SpriteRenderer holen
            //SpriteRenderer spriteRenderer = item ? item.GetComponent<SpriteRenderer>() : null;
            //if (spriteRenderer != null)
            //{
            //    // Farbe auf transparent setzen, damit es nicht sofort sichtbar ist
            //    spriteRenderer.color = new Color(1f, 1f, 1f, 0f);
            //    // Dann kurz sichtbar machen
            //    StartCoroutine(BlinkSprite(spriteRenderer.sprite));
            //}

            //// Das Sprite kurz blinken lassen
            //if (item != null)
            //{
            //    spriteRenderer.SetActive(true);
            //    this.gameObject.SetOpacity(0.5f); // Ghost-Objekt unsichtbar machen
            //    yield return new WaitForSeconds(0.1f); // 0.1 Sekunden sichtbar
            //    this.gameObject.SetOpacity(1f); // Ghost-Objekt unsichtbar machen
            //    spriteRenderer.SetActive(false);
            //}
        }

    }

    // Coroutine zum späteren Deaktivieren des Kindobjekts
    private IEnumerator DisableAfterSeconds(GameObject obj, float seconds, GhostReplay ghost)
    {
        yield return new WaitForSeconds(seconds);
        if (obj != null)
        {
            obj.SetActive(false); //ehemals false
            obj.tag = "Untagged"; // Sicherstellen, dass es nicht mehr als Ignorer erkannt wird

            var spriteTf = ghost.GetComponentsInChildren<SpriteRenderer>();
            foreach (var item in spriteTf)
            {
                GameObject spriteObj = item ? item.gameObject : null;
                if (spriteObj != null)
                    spriteObj.SetActive(true);
            }
        }
    }

    // Rekursive Suche nach einem Kindobjekt mit Namen
    private Transform FindDeepChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child;
            var result = FindDeepChild(child, name);
            if (result != null)
                return result;
        }
        return null;
    }

    // Call am Rundenende:
    public void OnLapFinished(float lapTime)
    {
        playerRecorder.EndLap(lapTime);

        if(playerRecorder.currentLap == null || playerRecorder.currentLap.frames.Count < 2)
        {
            Debug.LogWarning("Current lap data is empty or invalid. Cannot create ghost.");
            return;
        }

        // "Letzte Runde" übernehmen
        lastLap = Clone(playerRecorder.currentLap);

        // "Beste Runde" updaten
        if (bestLap == null || lapTime < bestLap.lapTime)
            bestLap = Clone(playerRecorder.currentLap);

        //Ghosts erst nach Runde spawnen
        LapData toReplay = null;
        if (mode == GhostMode.LastLap) toReplay = lastLap;
        else if (mode == GhostMode.BestLap) toReplay = bestLap;

        if (toReplay != null )
           // if (toReplay != null && bestLap.lapTime <= 0.1f)
            {
            // Ghost nach 5 Sekunden verzögert starten:
            StartCoroutine(SpawnAndActivateGhostDelayed(Clone(toReplay), 1f));
        }
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
    // Add the missing BlinkSprite method to resolve the CS0103 error.
    private IEnumerator BlinkSprite(GameObject spriteObj)
    {


        if (spriteObj == null) yield break;

        SpriteRenderer spriteRenderer = spriteObj.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) yield break;

        // Blink logic: make the sprite visible and invisible repeatedly
        for (int i = 0; i < 3; i++) // Blink 3 times
        {
            spriteRenderer.color = new Color(1f, 1f, 1f, 1f); // Fully visible
            yield return new WaitForSeconds(0.1f); // Visible for 0.1 seconds
            spriteRenderer.color = new Color(1f, 1f, 1f, 0f); // Fully transparent
            yield return new WaitForSeconds(0.1f); // Invisible for 0.1 seconds
        }

        // Ensure the sprite is fully visible at the end
        spriteRenderer.color = new Color(1f, 1f, 1f, 1f);
    }
    // Für Zugriff von außen (z.B. Anzeige)
    public IReadOnlyList<GhostReplay> ActiveGhosts => ghostInstances;
}
// Add this extension method to provide the missing SetOpacity functionality for GameObject.
public static class GameObjectExtensions
{
    public static void SetOpacity(this GameObject gameObject, float opacity)
    {
        if (gameObject == null) return;

        // Get all SpriteRenderer components in the GameObject and its children
        var spriteRenderers = gameObject.GetComponentsInChildren<SpriteRenderer>();
        foreach (var spriteRenderer in spriteRenderers)
        {
            var color = spriteRenderer.color;
            color.a = opacity; // Set the alpha value
            spriteRenderer.color = color;
        }
    }
}
