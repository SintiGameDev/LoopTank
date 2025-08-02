using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class GhostReplay : MonoBehaviour
{
    public LapData source;     // zugewiesen bei Start
    public bool playing { get; private set; }

    Rigidbody2D rb;
    int i;             // Index des nächsten Frames
    float t;           // Replay-Zeit

    void Awake() { rb = GetComponent<Rigidbody2D>(); }

    public void Play(LapData lap)
    {
        source = lap;
        if (source == null || source.frames.Count < 2) { playing = false; return; } 
        i = 1;
        t = 0f;
        // sofort an den ersten Frame setzen
        var f0 = source.frames[0];
        rb.position = f0.pos;
        rb.rotation = f0.rotZ;
        playing = true;
        gameObject.SetActive(true);
    }

    public void Stop()
    {
        Debug.Log("Ghost Replay stopped");
        playing = false;
        gameObject.SetActive(false);
    }

    void FixedUpdate()
    {
        if (!playing) return;
        t += Time.fixedDeltaTime;

        // Ende erreicht?
        if (t >= source.frames[^1].t) { Stop(); return; }

        // zum passenden Segment vorrücken
        while (i < source.frames.Count && source.frames[i].t < t) i++;

        var a = source.frames[i - 1];
        var b = source.frames[i];

        float seg = Mathf.InverseLerp(a.t, b.t, t);
        Vector2 pos = Vector2.Lerp(a.pos, b.pos, seg);
        float rot = Mathf.LerpAngle(a.rotZ, b.rotZ, seg);

        rb.MovePosition(pos);
        rb.MoveRotation(rot);

        //Debug.Log($"Ghost Replay: t={t:F2} pos={pos} rot={rot} seg={seg:F2} frame={i}/{source.frames.Count}");
        //Debug.Log($"LapRecorder: Frame aufgenommen t={t:F2}, pos={rb.position}, rotZ={rb.rotation}, frames.Count={source.frames.Count}");
    }
}
