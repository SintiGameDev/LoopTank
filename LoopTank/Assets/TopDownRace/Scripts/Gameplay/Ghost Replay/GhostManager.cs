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

    private List<GhostReplay> ghostInstances = new List<GhostReplay>();

    // Timer
    public Timer roundTimer; // Im Inspector zuweisen!
    private bool timerStarted = false;
    private bool timerUiShown = true;

    // *** NEU: Merkt, ob schon ein Ghost gespawnt wurde ***
    private bool hasSpawnedFirstGhost = false;

    // Call vom Renn-Controller am Start/Ziellinie:
    public void OnLapStarted()
    {
        playerRecorder.BeginLap();
        // Ghosts werden nicht hier, sondern nach der Runde gespawnt!
    }

    private IEnumerator SpawnAndActivateGhostDelayed(LapData ghostLap, float delaySeconds)
    {
        var go = Instantiate(
            ghostPrefab,
            playerRecorder.transform.position,
            playerRecorder.transform.rotation,
            ghostParent
        );
        var ghost = go.GetComponent<GhostReplay>();

        // SpriteRenderer anmachen
        var spriteRenderers = ghost.GetComponentsInChildren<SpriteRenderer>();
        foreach (var item in spriteRenderers)
        {
            if (item != null)
                item.gameObject.SetActive(true);
        }

        // CollisionIgnorer-Kind suchen (rekursiv) und für 1 Sekunde aktivieren
        var collisionIgnorer = FindDeepChild(go.transform, "CollisionIgnorer");
        if (collisionIgnorer != null)
        {
            collisionIgnorer.gameObject.SetActive(true);
            StartCoroutine(DisableAfterSeconds(collisionIgnorer.gameObject, 1f, ghost));
        }

        ghostInstances.Add(ghost);
        if (ghostInstances.Count > maxGhosts)
        {
            Destroy(ghostInstances[0].gameObject);
            ghostInstances.RemoveAt(0);
        }

        yield return new WaitForSeconds(delaySeconds);

        if (ghost != null)
            ghost.Play(ghostLap);

        // Hier könntest du noch Blink-Effekte o.ä. machen
    }

    private IEnumerator DisableAfterSeconds(GameObject obj, float seconds, GhostReplay ghost)
    {
        yield return new WaitForSeconds(seconds);
        if (obj != null)
        {
            obj.SetActive(false);
            obj.tag = "Untagged";

            var spriteRenderers = ghost.GetComponentsInChildren<SpriteRenderer>();
            foreach (var item in spriteRenderers)
            {
                if (item != null)
                    item.gameObject.SetActive(true);
            }
        }
    }

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

        if (playerRecorder.currentLap == null || playerRecorder.currentLap.frames.Count < 2)
        {
            Debug.LogWarning("Current lap data is empty or invalid. Cannot create ghost.");
            return;
        }

        // "Letzte Runde" übernehmen
        lastLap = Clone(playerRecorder.currentLap);

        // "Beste Runde" updaten
        if (bestLap == null || lapTime < bestLap.lapTime)
            bestLap = Clone(playerRecorder.currentLap);

        LapData toReplay = null;
        if (mode == GhostMode.LastLap) toReplay = lastLap;
        else if (mode == GhostMode.BestLap) toReplay = bestLap;

        // *** HIER: Ghosts erst ab der zweiten Runde spawnen ***
        if (!hasSpawnedFirstGhost)
        {
            // Beim allerersten Mal nur das Flag setzen!
            hasSpawnedFirstGhost = true;
            return;
        }
        // Ab jetzt immer Ghosts erzeugen
        if (toReplay != null)
        {
            StartCoroutine(SpawnAndActivateGhostDelayed(Clone(toReplay), 1f));
        }
    }

    LapData Clone(LapData src)
    {
        var c = ScriptableObject.CreateInstance<LapData>();
        c.lapTime = src.lapTime;
        foreach (var f in src.frames)
            c.frames.Add(new LapFrame { t = f.t, pos = f.pos, rotZ = f.rotZ });
        return c;
    }

    public void ClearAllGhosts()
    {
        foreach (var ghost in ghostInstances)
            if (ghost != null) Destroy(ghost.gameObject);
        ghostInstances.Clear();
    }

    public void RemoveGhost(GhostReplay ghost)
    {
        if (ghostInstances.Contains(ghost))
        {
            Destroy(ghost.gameObject);
            ghostInstances.Remove(ghost);
        }
    }

    public void SetMode(int m) { mode = (GhostMode)m; }

    public IReadOnlyList<GhostReplay> ActiveGhosts => ghostInstances;

    private IEnumerator BlinkSprite(GameObject spriteObj)
    {
        if (spriteObj == null) yield break;
        SpriteRenderer spriteRenderer = spriteObj.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) yield break;

        for (int i = 0; i < 3; i++)
        {
            spriteRenderer.color = new Color(1f, 1f, 1f, 1f);
            yield return new WaitForSeconds(0.1f);
            spriteRenderer.color = new Color(1f, 1f, 1f, 0f);
            yield return new WaitForSeconds(0.1f);
        }
        spriteRenderer.color = new Color(1f, 1f, 1f, 1f);
    }
}

public static class GameObjectExtensions
{
    public static void SetOpacity(this GameObject gameObject, float opacity)
    {
        if (gameObject == null) return;
        var spriteRenderers = gameObject.GetComponentsInChildren<SpriteRenderer>();
        foreach (var spriteRenderer in spriteRenderers)
        {
            var color = spriteRenderer.color;
            color.a = opacity;
            spriteRenderer.color = color;
        }
    }
}
