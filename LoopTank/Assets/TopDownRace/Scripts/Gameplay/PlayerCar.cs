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

        // --- NEUE VARIABLEN FÜR TANKTOP-DREH-SOUND ---
        [Tooltip("Das Soundfile, das abgespielt wird, wenn der TankTop gedreht wird.")]
        public AudioClip m_TankTopRotationSoundClip;
        [Tooltip("Die Lautstärke des TankTop-Rotationssounds.")]
        [Range(0.0f, 1.0f)]
        public float m_TankTopRotationVolume = 0.8f;
        [Tooltip("Die Geschwindigkeit, mit der der TankTop-Rotationssound ein-/ausblendet.")]
        [Range(0.1f, 5.0f)]
        public float m_TankTopFadeSpeed = 2.0f;

        private AudioSource m_TankTopRotationAudioSource; // Eigene AudioSource für den TankTop-Sound
        // ---------------------------------------------

        // --- NEUE VARIABLEN FÜR SCHUSSFUNKTION ---
        [Tooltip("Das Prefab der Kugel, die abgefeuert werden soll.")]
        public GameObject m_BulletPrefab;
        [Tooltip("Der Punkt, von dem aus die Kugel gespawnt wird (z.B. die Mündung des Geschützturms).")]
        public Transform m_BulletSpawnPoint;
        [Tooltip("Die Stärke des Impulses, mit dem die Kugel abgefeuert wird.")]
        public float m_BulletSpeed = 15f; // Bezeichnung beibehalten, aber jetzt als Kraftstärke interpretiert
        [Tooltip("Die Zeit in Sekunden, nach der die Kugel zerstört wird.")]
        public float m_BulletLifetime = 3f;
        [Tooltip("Der Soundeffekt, der beim Abfeuern der Kugel abgespielt wird.")]
        public AudioClip m_ShootSoundClip;

        private AudioSource m_ShootAudioSource; // Eigene AudioSource für den Schuss-Sound
        // -----------------------------------------

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
            // Stelle sicher, dass ein BulletSpawnPoint zugewiesen ist, wenn der TankTop existiert
            if (m_TankTop != null && m_BulletSpawnPoint == null)
            {
                // Versuche, einen 'Muzzle' oder 'BarrelEnd' als Kind des TankTops zu finden
                m_BulletSpawnPoint = m_TankTop.Find("Muzzle");
                if (m_BulletSpawnPoint == null)
                {
                    m_BulletSpawnPoint = m_TankTop.Find("BarrelEnd");
                }

                if (m_BulletSpawnPoint == null)
                {
                    Debug.LogWarning("Kein spezifischer BulletSpawnPoint zugewiesen oder als Kind des TankTops gefunden (versucht 'Muzzle' oder 'BarrelEnd'). Die Kugel wird direkt vom TankTop gespawnt. Erstelle einen leeren GameObject-Child am Ende deiner Kanone und weise ihn zu.", this);
                    m_BulletSpawnPoint = m_TankTop; // Fallback: Spawnt direkt vom TankTop
                }
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

            // --- Initialisierung der KOLLISIONSSOUND-AudioSource ---
            // Eine DRITTE AudioSource, die nicht looped
            m_CollisionAudioSource = gameObject.AddComponent<AudioSource>();
            m_CollisionAudioSource.loop = false; // Kollisionssounds sollen nicht wiederholt werden
            m_CollisionAudioSource.playOnAwake = false; // Wir spielen sie manuell ab
            m_CollisionAudioSource.volume = m_CollisionSoundVolume; // Setzt die Lautstärke

            // --- Initialisierung der TANKTOP-ROTATIONS-AudioSource ---
            // Eine VIERTE AudioSource, die looped, aber deren Lautstärke gesteuert wird
            m_TankTopRotationAudioSource = gameObject.AddComponent<AudioSource>();
            if (m_TankTopRotationSoundClip != null)
            {
                m_TankTopRotationAudioSource.clip = m_TankTopRotationSoundClip;
                m_TankTopRotationAudioSource.loop = true; // Dieser Sound looped
                m_TankTopRotationAudioSource.playOnAwake = false;
                m_TankTopRotationAudioSource.volume = 0.0f; // Startet leise und wird bei Bewegung eingeblendet

                m_TankTopRotationAudioSource.Play(); // Beginnt den Loop, aber stumm
            }
            else
            {
                Debug.LogWarning("Kein TankTop-Rotations-Sound-Clip zugewiesen! Bitte weise einen im Inspector zu.", this);
            }

            // --- Initialisierung der Schuss-AudioSource ---
            // Eine FÜNFTE AudioSource für den Schuss-Sound
            m_ShootAudioSource = gameObject.AddComponent<AudioSource>();
            m_ShootAudioSource.loop = false;
            m_ShootAudioSource.playOnAwake = false;
            m_ShootAudioSource.volume = 1.0f; // Standardlautstärke, kann angepasst werden
            // ---------------------------------------------------

            // --- Referenz zur CarPhysics-Komponente holen ---
            m_CarPhysics = GetComponent<CarPhysics>();
            if (m_CarPhysics == null)
            {
                Debug.LogError("CarPhysics-Komponente nicht gefunden! Kann Geschwindigkeit nicht anpassen.", this);
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (
                collision.CompareTag("Ghost")
                && !m_CollisionsIgnored
                && !HasTagInHierarchy(collision.gameObject, "CollisionIgnorer")
            )
            {
                Debug.Log("Kollision mit Ghost! Kontrolle deaktiviert und Spiel verloren.");
                m_Control = false;
                UISystem.ShowUI("lose-ui");

                GhostManager.Instance.ClearAllGhosts();

                if (m_EngineAudioSource != null && m_EngineAudioSource.isPlaying)
                    m_EngineAudioSource.Stop();

                if (m_AccelerationAudioSource != null && m_AccelerationAudioSource.isPlaying)
                    m_AccelerationAudioSource.Stop();

                if (m_TankTopRotationAudioSource != null && m_TankTopRotationAudioSource.isPlaying)
                    m_TankTopRotationAudioSource.Stop();

                // NEU: Schuss-Sound auch stoppen bei Spielverlust, falls er gerade abgespielt wird
                if (m_ShootAudioSource != null && m_ShootAudioSource.isPlaying)
                    m_ShootAudioSource.Stop();
            }
        }

        bool HasTagInHierarchy(GameObject obj, string tag)
        {
            if (obj.CompareTag(tag)) return true;
            foreach (Transform child in obj.transform)
            {
                if (HasTagInHierarchy(child.gameObject, tag))
                    return true;
            }
            return false;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (m_CollisionSoundClips != null && m_CollisionSoundClips.Count > 0 && m_CollisionAudioSource != null)
            {
                int randomIndex = Random.Range(0, m_CollisionSoundClips.Count);
                AudioClip clipToPlay = m_CollisionSoundClips[randomIndex];
                m_CollisionAudioSource.PlayOneShot(clipToPlay, m_CollisionSoundVolume);
            }
        }

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
            float verticalInput = Input.GetAxisRaw("Vertical");
            float horizontalInput = Input.GetAxisRaw("Horizontal");
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

                    // NEU: Schuss-Input-Erkennung
                    if (Input.GetKeyDown(KeyCode.Space))
                    {
                        ShootBullet();
                    }
                }
            }

            if (m_TankTop != null)
            {
                m_TankTop.Rotate(0, 0, -tankTopRotationInput * m_TankTopRotationSpeed * Time.deltaTime);

                if (m_TankTopRotationAudioSource != null)
                {
                    if (Mathf.Abs(tankTopRotationInput) > 0.01f)
                    {
                        m_TankTopRotationAudioSource.volume = Mathf.MoveTowards(m_TankTopRotationAudioSource.volume, m_TankTopRotationVolume, m_TankTopFadeSpeed * Time.deltaTime);
                    }
                    else
                    {
                        m_TankTopRotationAudioSource.volume = Mathf.MoveTowards(m_TankTopRotationAudioSource.volume, 0.0f, m_TankTopFadeSpeed * Time.deltaTime);
                    }
                }
            }

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
        }

        void ShootBullet()
        {
            if (m_BulletPrefab == null)
            {
                Debug.LogWarning("Bullet Prefab ist nicht zugewiesen! Bitte weise ein Bullet Prefab im Inspector zu.", this);
                return;
            }

            if (m_BulletSpawnPoint == null)
            {
                Debug.LogWarning("Bullet Spawn Point ist nicht zugewiesen! Die Kugel kann nicht gespawnt werden. Stelle sicher, dass du ein Child-GameObject ('Muzzle' oder 'BarrelEnd') im TankTop hast oder manuell zuweist.", this);
                return;
            }

            // Kugel instanziieren an der Position und Rotation des BulletSpawnPoint
            GameObject bullet = Instantiate(m_BulletPrefab, m_BulletSpawnPoint.position, m_BulletSpawnPoint.rotation);

            // Holen Sie sich die Rigidbody2D-Komponente der Kugel
            Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                Debug.LogError("Die Kugel (Bullet Prefab) benötigt eine Rigidbody2D-Komponente, um geschossen werden zu können!", bullet);
                // Wenn kein Rigidbody2D vorhanden ist, zerstöre das gespawnte Objekt, um Unordnung zu vermeiden.
                Destroy(bullet);
                return;
            }

            // Setze den Tag der Kugel
            bullet.tag = "Bullet"; // Du hast erwähnt, dass deine Bullet diesen Tag hat.

            // Berechne die Schussrichtung basierend auf der Rotation des TankTops (und damit des BulletSpawnPoint)
            // In 2D ist die "Vorwärts"-Richtung (also nach oben auf der Sprite-Achse) oft Vector2.up oder Vector2.right,
            // je nachdem, wie dein Sprite ausgerichtet ist.
            // transform.up ist die grüne Achse (Y-Achse) des Objekts im lokalen Raum,
            // transform.right ist die rote Achse (X-Achse).
            // Für einen 2D-Panzer, der sich um die Z-Achse dreht, ist `transform.up` oft die Richtung, in die der Lauf zeigt.
            Vector2 shootDirection = m_BulletSpawnPoint.up; // Oder m_BulletSpawnPoint.right, je nach Ausrichtung deines Bullet Prefabs/Sprites

            // Wende eine einmalige, starke Kraft auf die Kugel an
            // ForceMode2D.Impulse simuliert einen sofortigen Stoß (wie ein Schuss)
            rb.AddForce(shootDirection * m_BulletSpeed, ForceMode2D.Impulse);

            // Zerstöre die Kugel nach einer bestimmten Zeit, um das Spielfeld sauber zu halten
            Destroy(bullet, m_BulletLifetime);

            // Spiele den Schuss-Sound ab
            if (m_ShootSoundClip != null && m_ShootAudioSource != null)
            {
                m_ShootAudioSource.PlayOneShot(m_ShootSoundClip);
            }
        }
    }
}