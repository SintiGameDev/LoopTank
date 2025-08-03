using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TopDownRace
{
    public class PlayerCar : MonoBehaviour
    {
        [HideInInspector]
        public float m_Speed; // Dies ist wahrscheinlich die Maximalgeschwindigkeit, CarPhysics wird die aktuelle Geschwindigkeit haben

        [HideInInspector]
        public int m_CurrentCheckpoint;

        [HideInInspector]
        public bool m_Control = false;

        public static PlayerCar m_Current;

        [Tooltip("Die Geschwindigkeit, mit der sich der gesamte Panzer (Body) beim Bewegen und im Stand dreht (A/D-Tasten).")]
        [Range(0.1f, 10.0f)]
        public float m_RotationSpeed = 3.0f;

        [Tooltip("Die Geschwindigkeit, mit der sich der TankTop mit den Pfeiltasten dreht.")]
        [Range(10.0f, 300.0f)]
        public float m_TankTopRotationSpeed = 100.0f;

        // --- SOUND-VARIABLEN FÜR DAS GRUNDLEGENDE MOTORENGERÄUSCH ---
        [Tooltip("Das Soundfile für das konstante Grund-Motorengeräusch des Panzers.")]
        public AudioClip m_EngineSoundClip;
        private AudioSource m_EngineAudioSource; // Referenz auf die AudioSource Komponente für das Grundgeräusch

        // --- SOUND-VARIABLEN FÜR DEN GESCHWINDIGKEITSABHÄNGIGEN SOUND (PITCH-ÄNDERUNG) ---
        [Tooltip("Das Soundfile, dessen Tonhöhe sich mit der Geschwindigkeit des Panzers ändert (z.B. Turbopfeifen, Anfahrgeräusch).")]
        public AudioClip m_AccelerationSoundClip;
        private AudioSource m_AccelerationAudioSource; // Referenz auf die AudioSource Komponente für den Beschleunigungssound

        [Tooltip("Die minimale Tonhöhe des Beschleunigungssounds bei Stillstand oder sehr langsamer Fahrt.")]
        [Range(0.1f, 2.0f)]
        public float m_MinPitch = 0.8f;

        [Tooltip("Die maximale Tonhöhe des Beschleunigungssounds bei voller Geschwindigkeit.")]
        [Range(1.0f, 3.0f)]
        public float m_MaxPitch = 1.8f;

        [Tooltip("Die minimale Lautstärke des Beschleunigungssounds bei Stillstand.")]
        [Range(0.0f, 1.0f)]
        public float m_MinAccelerationVolume = 0.0f;

        [Tooltip("Die maximale Lautstärke des Beschleunigungssounds bei voller Geschwindigkeit.")]
        [Range(0.0f, 1.0f)]
        public float m_MaxAccelerationVolume = 1.0f;

        // --- NEUE VARIABLEN FÜR KOLLISIONSSOUNDS ---
        [Tooltip("Eine Liste von Soundeffekten, die zufällig abgespielt werden, wenn der Panzer kollidiert.")]
        public List<AudioClip> m_CollisionSoundClips;
        [Tooltip("Die maximale Lautstärke der Kollisionssounds.")]
        [Range(0.0f, 1.0f)]
        public float m_CollisionSoundVolume = 0.7f;

        private AudioSource m_CollisionAudioSource; // Eine separate AudioSource für Kollisionssounds
        // ------------------------------------------

        private CarPhysics m_CarPhysics; // Referenz auf die CarPhysics-Komponente, um die Geschwindigkeit zu erhalten

        private Transform m_TankTop;
        private bool m_CollisionsIgnored = false;

        void Awake()
        {
            m_Current = this;
        }

        void Start()
        {
            m_CurrentCheckpoint = 1;
            m_Control = true;
            m_Speed = 80;

            // Finde das TankTop-Objekt als Kind dieses GameObjects
            m_TankTop = transform.Find("TankTop");

            if (m_TankTop == null)
            {
                Debug.LogError("TankTop-Objekt nicht gefunden! Stelle sicher, dass ein Kindobjekt mit dem Namen 'TankTop' existiert.", this);
            }

            // Stelle sicher, dass ein Rigidbody2D vorhanden ist
            if (GetComponent<Rigidbody2D>() == null)
            {
                gameObject.AddComponent<Rigidbody2D>().isKinematic = true;
            }
            // Stelle sicher, dass ein Collider2D vorhanden ist
            if (GetComponent<Collider2D>() == null)
            {
                gameObject.AddComponent<CapsuleCollider2D>().isTrigger = true;
            }

            // --- Initialisierung des GRUNDLEGENDEN MOTORENGERÄUSCHS ---
            // Wir nutzen die erste AudioSource, die am GameObject hängt oder fügen eine hinzu
            m_EngineAudioSource = GetComponent<AudioSource>();
            if (m_EngineAudioSource == null)
            {
                m_EngineAudioSource = gameObject.AddComponent<AudioSource>();
            }

            if (m_EngineSoundClip != null)
            {
                m_EngineAudioSource.clip = m_EngineSoundClip;
                m_EngineAudioSource.loop = true;
                m_EngineAudioSource.playOnAwake = false;
                m_EngineAudioSource.volume = 0.5f;

                m_EngineAudioSource.Play();
            }
            else
            {
                Debug.LogWarning("Kein GRUND-Motorengeräusch-Clip zugewiesen! Bitte weise einen im Inspector zu.", this);
            }

            // --- Initialisierung des BESCHLEUNIGUNGS-SOUNDS (PITCH-ÄNDERUNG) ---
            // Wir fügen EINE ZWEITE AudioSource hinzu
            m_AccelerationAudioSource = gameObject.AddComponent<AudioSource>();

            if (m_AccelerationSoundClip != null)
            {
                m_AccelerationAudioSource.clip = m_AccelerationSoundClip;
                m_AccelerationAudioSource.loop = true;
                m_AccelerationAudioSource.playOnAwake = false;
                m_AccelerationAudioSource.volume = m_MinAccelerationVolume;
                m_AccelerationAudioSource.pitch = m_MinPitch;

                m_AccelerationAudioSource.Play();
            }
            else
            {
                Debug.LogWarning("Kein BESCHLEUNIGUNGS-Sound-Clip zugewiesen! Bitte weise einen im Inspector zu.", this);
            }

            // --- NEU: Initialisierung der KOLLISIONSSOUND-AudioSource ---
            // Eine DRITTE AudioSource, die nicht looped
            m_CollisionAudioSource = gameObject.AddComponent<AudioSource>();
            m_CollisionAudioSource.loop = false; // Kollisionssounds sollen nicht wiederholt werden
            m_CollisionAudioSource.playOnAwake = false; // Wir spielen sie manuell ab
            m_CollisionAudioSource.volume = m_CollisionSoundVolume; // Setzt die Lautstärke
            // -------------------------------------------------------------

            // --- Referenz zur CarPhysics-Komponente holen ---
            m_CarPhysics = GetComponent<CarPhysics>();
            if (m_CarPhysics == null)
            {
                Debug.LogError("CarPhysics-Komponente nicht gefunden! Kann Geschwindigkeit nicht anpassen.", this);
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.CompareTag("Ghost") && m_CollisionsIgnored == false)
            {
                Debug.Log("Kollision mit Ghost! Kontrolle deaktiviert und Spiel verloren.");
                m_Control = false;
                Time.timeScale = 0f;
                UISystem.ShowUI("lose-ui");

                // Alle Motorengeräusche stoppen bei Spielverlust
                if (m_EngineAudioSource != null && m_EngineAudioSource.isPlaying)
                {
                    m_EngineAudioSource.Stop();
                }
                if (m_AccelerationAudioSource != null && m_AccelerationAudioSource.isPlaying)
                {
                    m_AccelerationAudioSource.Stop();
                }
            }
        }

        // --- NEUE METHODE FÜR PHYSISCHE KOLLISIONEN ---
        // OnCollisionEnter2D wird aufgerufen, wenn dieser Collider2D beginnt, einen anderen 2D-Collider zu berühren.
        private void OnCollisionEnter2D(Collision2D collision)
        {
            // Überprüfe, ob Kollisionssounds zugewiesen sind und die AudioSource bereit ist
            if (m_CollisionSoundClips != null && m_CollisionSoundClips.Count > 0 && m_CollisionAudioSource != null)
            {
                // Wähle einen zufälligen Sound aus der Liste
                int randomIndex = Random.Range(0, m_CollisionSoundClips.Count);
                AudioClip clipToPlay = m_CollisionSoundClips[randomIndex];

                // Spiele den ausgewählten Sound ab
                m_CollisionAudioSource.PlayOneShot(clipToPlay, m_CollisionSoundVolume);
            }
        }
        // -----------------------------------------------

        private void OnTriggerStay2D(Collider2D collision)
        {
            if (collision.CompareTag("CollisionIgnorer"))
            {
                m_CollisionsIgnored = true;
            }
        }

        private void OnTriggerExit2D(Collider2D collision)
        {
            if (collision.CompareTag("CollisionIgnorer"))
            {
                m_CollisionsIgnored = false;
            }
        }

        void Update()
        {
            // Input für den gesamten Panzer (WASD)
            float verticalInput = Input.GetAxisRaw("Vertical");    // W/S
            float horizontalInput = Input.GetAxisRaw("Horizontal"); // A/D (für die Panzer-Drehung)

            // Input NUR für den TankTop (Pfeiltasten Links/Rechts)
            float tankTopRotationInput = Input.GetAxisRaw("TankTopHorizontal");

            if (GameControl.m_Current != null && GameControl.m_Current.m_StartRace)
            {
                if (m_Control)
                {
                    GetComponent<CarPhysics>().m_InputAccelerate = verticalInput;

                    if (Mathf.Abs(verticalInput) > 0.01f)
                    {
                        GetComponent<CarPhysics>().m_InputSteer = -horizontalInput * m_RotationSpeed * (verticalInput > 0 ? 1 : -1);
                    }
                    else
                    {
                        GetComponent<CarPhysics>().m_InputSteer = -horizontalInput * m_RotationSpeed;
                    }
                }
            }

            // TankTop-Rotation
            if (m_TankTop != null)
            {
                m_TankTop.Rotate(0, 0, -tankTopRotationInput * m_TankTopRotationSpeed * Time.deltaTime);
            }

            // --- ANPASSEN VON PITCH UND VOLUME FÜR DEN BESCHLEUNIGUNGSSOUND ---
            if (m_AccelerationAudioSource != null && m_CarPhysics != null)
            {
                float currentSpeed = m_CarPhysics.GetComponent<Rigidbody2D>().linearVelocity.magnitude;

                float speedNormalized = 0f;
                if (m_Speed > 0)
                {
                    speedNormalized = Mathf.Clamp01(Mathf.Abs(currentSpeed) / m_Speed);
                }

                m_AccelerationAudioSource.pitch = Mathf.Lerp(m_MinPitch, m_MaxPitch, speedNormalized);
                m_AccelerationAudioSource.volume = Mathf.Lerp(m_MinAccelerationVolume, m_MaxAccelerationVolume, speedNormalized);
            }
            // ----------------------------------------------------------------------
        }
    }
}