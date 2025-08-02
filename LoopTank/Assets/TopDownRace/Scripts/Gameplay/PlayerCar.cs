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

        // Variable f�r die Rotationsgeschwindigkeit des Panzers
        [Tooltip("Die Geschwindigkeit, mit der sich der Panzer beim Bewegen und im Stand dreht.")]
        [Range(0.1f, 10.0f)]
        public float m_RotationSpeed = 3.0f;

        // Referenz auf die obere H�lfte des Panzers mit der Kanone
        private Transform m_TankTop;

        void Awake()
        {
            m_Current = this;
        }

        void Start()
        {
            m_CurrentCheckpoint = 1;
            m_Control = true;
            m_Speed = 80;

            // Finde die obere H�lfte des Panzers beim Start
            // Annahme: TankTop ist ein Kindobjekt und hei�t "TankTop"
            m_TankTop = transform.Find("TankTop");

            if (m_TankTop == null)
            {
                Debug.LogError("TankTop-Objekt nicht gefunden! Stelle sicher, dass ein Kindobjekt mit dem Namen 'TankTop' existiert.", this);
            }
        }
        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.CompareTag("Ghost"))
            {
                // Wenn der Panzer einen Ghost ber�hrt, wird die Kontrolle deaktiviert
                m_Control = false;
                Debug.Log("Ghost ber�hrt! Kontrolle deaktiviert.");
                UISystem.ShowUI("win-ui");
            }
            else if (collision.CompareTag("Ghost"))
            {
                // Wenn der Panzer einen Checkpoint ber�hrt, wird die Kontrolle aktiviert
                m_Control = true;
                Debug.Log("Checkpoint ber�hrt! Kontrolle aktiviert.");
            }
        }

        void Update()
        {
            float vertical = Input.GetAxisRaw("Vertical"); // Vorw�rts/R�ckw�rts
            float horizontal = Input.GetAxisRaw("Horizontal"); // Links/Rechts drehen

            if (GameControl.m_Current.m_StartRace)
            {
                if (m_Control)
                {
                    // Beschleunigung basierend auf der vertikalen Eingabe
                    GetComponent<CarPhysics>().m_InputAccelerate = vertical;

                    // Drehen des Panzers (K�rper)
                    if (Mathf.Abs(vertical) > 0.01f) // Wenn der Panzer sich bewegt (vorw�rts oder r�ckw�rts)
                    {
                        GetComponent<CarPhysics>().m_InputSteer = -horizontal * m_RotationSpeed * (vertical > 0 ? 1 : -1);
                    }
                    else // Wenn der Panzer steht, dreht er sich auf der Stelle
                    {
                        GetComponent<CarPhysics>().m_InputSteer = -horizontal * m_RotationSpeed;
                    }
                }
            }

            // --- Steuerung der Panzerkanone (TankTop) ---
            if (m_TankTop != null)
            {
                // Hole die Position des Mauses in der Welt
                Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, Camera.main.transform.position.y - m_TankTop.position.y));

                // Berechne die Richtung vom TankTop zum Mauszeiger
                Vector3 directionToMouse = (mouseWorldPos - m_TankTop.position).normalized;

                // Setze die Z-Koordinate auf 0, um nur die Drehung in der 2D-Ebene zu ber�cksichtigen.
                directionToMouse.z = 0f;

                // Berechne den Winkel zur Mausposition.
                // Da dein Sprite nach RECHTS ausgerichtet ist, verwenden wir Vector2.right als Referenz.
                float angle = Vector2.SignedAngle(Vector2.right, new Vector2(directionToMouse.x, directionToMouse.y));

                // Wende die Rotation auf das TankTop an.
                // Da der Standard "nach rechts" ist und wir den Winkel von dort aus berechnen,
                // sollte dies die korrekte Ausrichtung gew�hrleisten.
                m_TankTop.rotation = Quaternion.Euler(0, 0, angle);
            }
        }
    }
}