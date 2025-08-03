using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TopDownRace
{
    public class CarPhysics : MonoBehaviour
    {
        [HideInInspector]
        public Rigidbody2D m_Body;

        [HideInInspector]
        public float m_InputAccelerate = 0;
        [HideInInspector]
        public float m_InputSteer = 0;

        public float m_SpeedForce = 80;

        public GameObject m_TireTracks;
        public Transform m_T_TireMarkPoint; // Korrektur: m_TireMarkPoint war in deinem Code ein 'Transform', aber der Name war inkonsistent. Habe es hier angepasst, um Verwirrung zu vermeiden.

        // --- VARIABLEN FÜR DEN TIRETRACK-SOUND (LOOPEND MIT FADE) ---
        [Tooltip("Der Sound-Clip, der geloopt wird, wenn Reifenspuren erzeugt werden (z.B. ein konstantes Quietsch-/Rutschgeräusch).")]
        public AudioClip m_TireTrackLoopSoundClip;

        [Tooltip("Die maximale Lautstärke für den loopenden Reifenspur-Soundeffekt.")]
        [Range(0.0f, 1.0f)]
        public float m_TireTrackMaxVolume = 0.7f; // Maximale Lautstärke

        [Tooltip("Die Geschwindigkeit, mit der der Reifenspur-Sound ein- und ausblendet.")]
        [Range(0.1f, 5.0f)]
        public float m_TireTrackFadeSpeed = 2.0f; // Ein- und Ausblendgeschwindigkeit

        private AudioSource m_TireTrackLoopAudioSource; // Dedizierte AudioSource für diesen loopenden Sound
        private bool m_IsTireSoundActive = false; // Zustand, ob der Sound gerade aktiv sein sollte
        // -------------------------------------

        // Start is called before the first frame-Aufruf
        void Start()
        {
            m_Body = GetComponent<Rigidbody2D>();

            // --- INITIALISIERUNG DER TIRETRACK-SOUND-AUDIO SOURCE ---
            m_TireTrackLoopAudioSource = gameObject.AddComponent<AudioSource>();
            if (m_TireTrackLoopSoundClip != null)
            {
                m_TireTrackLoopAudioSource.clip = m_TireTrackLoopSoundClip;
                m_TireTrackLoopAudioSource.loop = true;          // Sound soll loopen
                m_TireTrackLoopAudioSource.playOnAwake = false;  // Wir starten ihn manuell
                m_TireTrackLoopAudioSource.volume = 0.0f;        // Startet stumm
                m_TireTrackLoopAudioSource.Play();               // Beginnt den Loop (stumm)
            }
            else
            {
                Debug.LogWarning("Kein loopender Reifenspur-Sound-Clip zugewiesen! Der Sound wird nicht abgespielt.", this);
            }
            // --------------------------------------------------------
        }

        void Update()
        {
            Vector2 velocity = m_Body.linearVelocity;
            // Helper.ToVector2(transform.right) gibt die "rechte" Richtung des Autos zurück.
            // In 2D-Top-Down-Spielen ist die "Vorwärts"-Richtung oft transform.up,
            // und die "Seite"-Richtung ist transform.right.
            // Wir wollen den Drift-Winkel zwischen der Bewegungsrichtung und der Fahrtrichtung (vorwärts).
            // Wenn dein Auto sich um Z dreht und transform.right die Längsachse ist, dann ist das so ok.
            // Falls transform.up die "Vorwärts"-Richtung deines Autos ist, müsstest du hier:
            // Vector2 forward = Helper.ToVector2(transform.up); verwenden.
            Vector2 forward = Helper.ToVector2(transform.right);
            float delta = Vector2.SignedAngle(forward, velocity);

            // Bedingung für das Erzeugen von Reifenspuren und Aktivieren des Sounds
            bool shouldSpawnTireTracks = (velocity.magnitude > 10 && Mathf.Abs(delta) > 20);

            if (shouldSpawnTireTracks)
            {
                // Spawne Reifenspur-Prefab wie gehabt
                GameObject obj = Instantiate(m_TireTracks);
                obj.transform.position = m_T_TireMarkPoint.position;
                obj.transform.rotation = m_T_TireMarkPoint.rotation;
                Destroy(obj, 2); // Zerstöre die Spur nach 2 Sekunden

                // Aktiviere den Reifenspur-Sound
                m_IsTireSoundActive = true;
            }
            else
            {
                // Deaktiviere den Reifenspur-Sound
                m_IsTireSoundActive = false;
            }

            // Steuerung des loopenden Reifenspur-Sounds (Ein- und Ausblenden)
            if (m_TireTrackLoopAudioSource != null && m_TireTrackLoopAudioSource.clip != null)
            {
                if (m_IsTireSoundActive)
                {
                    // Blende Sound ein
                    m_TireTrackLoopAudioSource.volume = Mathf.MoveTowards(m_TireTrackLoopAudioSource.volume, m_TireTrackMaxVolume, m_TireTrackFadeSpeed * Time.deltaTime);
                }
                else
                {
                    // Blende Sound aus
                    m_TireTrackLoopAudioSource.volume = Mathf.MoveTowards(m_TireTrackLoopAudioSource.volume, 0.0f, m_TireTrackFadeSpeed * Time.deltaTime);
                }
            }
        }

        // FixedUpdate wird einmal pro fester Physik-Frame-Rate aufgerufen
        void FixedUpdate()
        {
            Vector3 forward = Quaternion.Euler(0, 0, m_Body.rotation) * Vector3.right;

            m_Body.AddForce(m_InputAccelerate * m_SpeedForce * Helper.ToVector2(forward), ForceMode2D.Impulse);

            Vector3 right = Quaternion.Euler(0, 0, 90) * forward;
            Vector3 project1 = Vector3.Project(Helper.ToVector3(m_Body.linearVelocity), right);

            m_Body.linearVelocity -= .02f * Helper.ToVector2(project1);

            m_Body.angularVelocity += 40 * m_InputSteer;

            m_InputAccelerate = 0;
            m_InputSteer = 0;
        }
    }
}