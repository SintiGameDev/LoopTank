using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TopDownRace
{
    public class PlayerCar : MonoBehaviour
    {
        [HideInInspector]
        public float m_Speed;

        [HideInInspector]
        public int m_CurrentCheckpoint;

        [HideInInspector]
        public bool m_Control = false;

        public static PlayerCar m_Current;

        [Tooltip("Die Geschwindigkeit, mit der sich der Panzer beim Bewegen und im Stand dreht.")]
        [Range(0.1f, 10.0f)]
        public float m_RotationSpeed = 3.0f;

        // Neu: Eine separate Rotationsgeschwindigkeit für den TankTop
        [Tooltip("Die Geschwindigkeit, mit der sich der TankTop mit den Pfeiltasten dreht.")]
        [Range(10.0f, 300.0f)] // Erhöhter Bereich, da hier feste Rotation pro Sekunde
        public float m_TankTopRotationSpeed = 100.0f;

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
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            // Wenn der Spieler mit einem "Ghost" kollidiert und Kollisionen nicht ignoriert werden
            if (collision.CompareTag("Ghost") && m_CollisionsIgnored == false)
            {
                Debug.Log("Kollision mit Ghost! Kontrolle deaktiviert und Spiel verloren.");
                m_Control = false; // Deaktiviere die Kontrolle des Spielers
                Time.timeScale = 0f; // Pausiere das Spiel
                // Hier könntest du eine UI einblenden, z.B. Game Over
                UISystem.ShowUI("lose-ui"); 
            }
        }

        private void OnTriggerStay2D(Collider2D collision)
        {
            // Wenn der Spieler in einem "CollisionIgnorer"-Bereich ist, ignoriere Kollisionen
            if (collision.CompareTag("CollisionIgnorer"))
            {
                m_CollisionsIgnored = true;
            }
        }

        private void OnTriggerExit2D(Collider2D collision)
        {
            // Wenn der Spieler einen "CollisionIgnorer"-Bereich verlässt, reaktiviere Kollisionen
            if (collision.CompareTag("CollisionIgnorer"))
            {
                m_CollisionsIgnored = false;
            }
        }

        void Update()
        {
            float vertical = Input.GetAxisRaw("Vertical");
            float horizontal = Input.GetAxisRaw("Horizontal"); // Diese Variable nutzen wir jetzt auch für den TankTop

            // Überprüfe, ob das Rennen gestartet ist
            if (GameControl.m_Current != null && GameControl.m_Current.m_StartRace)
            {
                if (m_Control)
                {
                    // Steuere die Beschleunigung des Autos
                    GetComponent<CarPhysics>().m_InputAccelerate = vertical;

                    // Steuere die Lenkung des Autos basierend auf Bewegung
                    if (Mathf.Abs(vertical) > 0.01f)
                    {
                        // Lenkung ist invers, wenn man rückwärts fährt
                        GetComponent<CarPhysics>().m_InputSteer = -horizontal * m_RotationSpeed * (vertical > 0 ? 1 : -1);
                    }
                    else
                    {
                        // Normale Lenkung, wenn das Auto steht oder langsam ist
                        GetComponent<CarPhysics>().m_InputSteer = -horizontal * m_RotationSpeed;
                    }
                }
            }

            // NEU: TankTop-Rotation basierend auf den Pfeiltasten Links/Rechts 
            if (m_TankTop != null)
            {
                // Hole die Eingabe für die horizontale Achse 
                float tankTopRotationInput = Input.GetAxisRaw("TankTopHorizontal");

                // Drehe den TankTop um seine Z-Achse
                // Multipliziere mit Time.deltaTime für eine geschmeidige, framerate-unabhängige Drehung
                m_TankTop.Rotate(0, 0, -tankTopRotationInput * m_TankTopRotationSpeed * Time.deltaTime);
            }
        }
    }
}