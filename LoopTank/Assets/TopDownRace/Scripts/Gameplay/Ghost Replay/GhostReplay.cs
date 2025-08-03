using UnityEngine;
using System.Collections; // F�ge dies hinzu, wenn du Coroutinen wie WaitForSeconds verwendest

[RequireComponent(typeof(Rigidbody2D))]
public class GhostReplay : MonoBehaviour
{
    public LapData source;       // zugewiesen bei Start
    public bool playing { get; private set; }

    Rigidbody2D rb;
    int i;                     // Index des n�chsten Frames
    float t;                   // Replay-Zeit

    // NEU: St�rke des R�cksto�es
    [Tooltip("Die St�rke des R�cksto�es, wenn der GhostTank von einer Kugel getroffen wird.")]
    public float bulletImpactForce = 50f;

    // NEU: Dauer des R�cksto�es / wie lange er von der Replay-Bahn abweicht
    [Tooltip("Die Dauer in Sekunden, f�r die der GhostTank vom Replay abweicht, nachdem er getroffen wurde.")]
    public float impactDuration = 0.2f; // Z.B. 0.2 Sekunden

    // NEU: Variable, um den Impact-Zustand zu verfolgen
    private bool isInImpact = false;
    private float impactTimer = 0f;

    void Awake() { rb = GetComponent<Rigidbody2D>(); }

    public void Play(LapData lap)
    {
        source = lap;
        if (source == null || source.frames.Count < 2) { playing = false; return; }

        // Initialisiere das Replay. Hier wird die Position und Rotation gesetzt.
        i = 1;
        t = 0f;
        var f0 = source.frames[0];
        rb.position = f0.pos;
        rb.rotation = f0.rotZ;
        playing = true;
        gameObject.SetActive(true);
        isInImpact = false; // Sicherstellen, dass der Impact-Zustand beim Start zur�ckgesetzt wird
    }

    public void Stop()
    {
        playing = false;
        gameObject.SetActive(false);
        isInImpact = false; // Impact-Zustand auch beim Stoppen zur�cksetzen
    }

    void FixedUpdate()
    {
        if (!playing) return;

        if (isInImpact)
        {
            // Wenn im Impact-Modus, lass die Physik den Tank bewegen.
            // Z�hle den Timer herunter
            impactTimer -= Time.fixedDeltaTime;
            if (impactTimer <= 0f)
            {
                // Impact-Dauer ist abgelaufen, kehre zum Replay zur�ck.
                // Wir m�ssen den n�chsten Replay-Frame finden, der dem aktuellen Zeitpunkt 't' am n�chsten liegt,
                // und die aktuelle Position des Rigidbody als neuen Startpunkt f�r das Replay setzen,
                // um einen Ruck zu vermeiden.
                isInImpact = false;

                // Finden Sie den Frame, der am besten zu unserer aktuellen Replay-Zeit passt
                // (wir setzen 't' hier nicht zur�ck, damit das Replay an der richtigen Stelle fortgesetzt wird)
                while (i < source.frames.Count && source.frames[i].t < t) i++;
                if (i >= source.frames.Count) // Falls wir �ber das Ende hinaus sind (unwahrscheinlich nach Looping-Check)
                {
                    t = 0f;
                    i = 1;
                }
                // Setze die Replay-Startposition auf die aktuelle Position des GhostTanks,
                // um einen Sprung zu vermeiden.
                // Wichtig: Das LapData.Frame m�sste hier aktualisiert werden oder wir ignorieren einfach den ersten Frame
                // und lassen das Replay von der aktuellen Position aus interpolieren.
                // F�r dieses Szenario lassen wir das Replay einfach von der aktuellen Zeit 't' aus weiterlaufen.
                // Die n�chste Interpolation wird dann von 'a' zu 'b' gehen, wo 'a' ein fr�herer Frame ist.
                // Das kann einen kleinen Sprung verursachen, wenn die aktuelle Position weit vom interpolierten 'a' entfernt ist.
                // Eine bessere L�sung w�re, LapData anzupassen oder den Replay-Index 'i' zur�ckzusetzen und von der aktuellen Position zu interpolieren.
                // F�r "f�hrt weiter ganz normal" ist es am einfachsten, die Zeit und den Index beizubehalten.
            }
            // Beende FixedUpdate, damit das Replay nicht sofort wieder �berschreibt
            return;
        }

        t += Time.fixedDeltaTime;

        // ENDE? Dann Loopen!
        if (t >= source.frames[^1].t)
        {
            t = 0f;
            i = 1;
            var f0 = source.frames[0];
            rb.position = f0.pos;
            rb.rotation = f0.rotZ;
            return;
        }

        while (i < source.frames.Count && source.frames[i].t < t) i++;

        var a = source.frames[i - 1];
        var b = source.frames[i];

        float seg = Mathf.InverseLerp(a.t, b.t, t);
        Vector2 pos = Vector2.Lerp(a.pos, b.pos, seg);
        float rot = Mathf.LerpAngle(a.rotZ, b.rotZ, seg);

        rb.MovePosition(pos);
        rb.MoveRotation(rot);
    }

    // NEU: Kollisionserkennung f�r Kugeln
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Pr�fen, ob das kollidierende Objekt den Tag "Bullet" hat
        if (collision.gameObject.CompareTag("Bullet"))
        {
            // Debug-Meldung, um zu sehen, ob die Kollision erkannt wird
            Debug.Log("GhostTank wurde von Kugel getroffen!", this);

            // Verhindere Mehrfachst��e, wenn bereits ein Impact aktiv ist
            if (isInImpact) return;

            isInImpact = true;
            impactTimer = impactDuration; // Setze den Timer f�r die Impact-Dauer

            // Berechne die Richtung weg von der Kugel
            // Die Kugel trifft den GhostTank. Wir wollen den GhostTank wegsto�en.
            // Die Normale des Kontakts zeigt aus dem Kollisionspunkt heraus.
            Vector2 pushDirection = collision.contacts[0].normal;

            // Wende die Kraft an
            // Wir verwenden ForceMode2D.Impulse f�r einen sofortigen Sto�
            rb.AddForce(pushDirection * bulletImpactForce, ForceMode2D.Impulse);

            // Optional: Zerst�re die Kugel, wenn sie den GhostTank trifft
            Destroy(collision.gameObject);
        }
    }
}