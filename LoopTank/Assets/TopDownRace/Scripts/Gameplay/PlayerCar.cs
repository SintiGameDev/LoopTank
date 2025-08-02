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

        private Transform m_TankTop;
        private bool m_CollisionsIgnored = false;

        private float targetTimeScale = 1f;

        void Awake()
        {
            m_Current = this;
        }

        void Start()
        {
            m_CurrentCheckpoint = 1;
            m_Control = true;
            m_Speed = 80;

            m_TankTop = transform.Find("TankTop");

            if (m_TankTop == null)
            {
                Debug.LogError("TankTop-Objekt nicht gefunden! Stelle sicher, dass ein Kindobjekt mit dem Namen 'TankTop' existiert.", this);
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            //Falls 5 sekunden nach Spielerspawn vergangen sind
            if (collision.CompareTag("Ghost") && m_CollisionsIgnored == false)
            {
                Debug.Log("M_Collisions" + m_CollisionsIgnored);
                m_Control = false;
                Debug.Log("Ghost berührt! Kontrolle deaktiviert.");
                UISystem.ShowUI("win-ui");
            }
            //else if (collision.CompareTag("Ghost"))
            //{
            //    m_Control = true;
            //    Debug.Log("Checkpoint berührt! Kontrolle aktiviert.");
            //}
        }

        private void OnTriggerStay2D(Collider2D collision)
        {
            if (collision.CompareTag("CollisionIgnorer"))
            {
                //Spielercolider deaktivieren, um Kollisionen zu ignorieren
                //GetComponent<Collider2D>().enabled = false;
                m_CollisionsIgnored = true;
               // Debug.Log("CollisionIgnorer berührt! Kollisionen werden ignoriert.");
            }
        }

        private void OnTriggerExit2D(Collider2D collision)
        {
            if (collision.CompareTag("CollisionIgnorer"))
            {
                //Spielercolider wieder aktivieren, um Kollisionen zu ermöglichen
                //GetComponent<Collider2D>().enabled = true;
                m_CollisionsIgnored = false;
            }
        }

        //private void OnCollisionEnter2D(Collision2D collision)
        //{
        //    if (m_CollisionsIgnored && collision.gameObject.CompareTag("Ghost"))
        //    {
        //        // Kollision ignorieren, wenn m_CollisionsIgnored wahr ist
        //        Physics2D.IgnoreCollision(GetComponent<Collider2D>(), collision.collider);
        //        Debug.Log("Kollision mit Ghost ignoriert.");
        //    }
        //}
        

        void Update()
        {
            float vertical = Input.GetAxisRaw("Vertical");
            float horizontal = Input.GetAxisRaw("Horizontal");

            if (GameControl.m_Current.m_StartRace)
            {
                if (m_Control)
                {
                    GetComponent<CarPhysics>().m_InputAccelerate = vertical;

                    if (Mathf.Abs(vertical) > 0.01f)
                    {
                        GetComponent<CarPhysics>().m_InputSteer = -horizontal * m_RotationSpeed * (vertical > 0 ? 1 : -1);
                        targetTimeScale = 1f;
                    }
                    else
                    {
                        GetComponent<CarPhysics>().m_InputSteer = -horizontal * m_RotationSpeed;
                        targetTimeScale = Mathf.Clamp(Mathf.Abs(GetComponent<CarPhysics>().m_InputSteer), 0.2f, 1f); // z.B. min 0.2
                    }
                }
            }

            // Smoothes Interpolieren der Zeit
            Time.timeScale = Mathf.Lerp(Time.timeScale, targetTimeScale, Time.unscaledDeltaTime * 5f);

            if (m_TankTop != null)
            {
                Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, Camera.main.transform.position.y - m_TankTop.position.y));
                Vector3 directionToMouse = (mouseWorldPos - m_TankTop.position).normalized;
                directionToMouse.z = 0f;

                float angle = Vector2.SignedAngle(Vector2.right, new Vector2(directionToMouse.x, directionToMouse.y));
                m_TankTop.rotation = Quaternion.Euler(0, 0, angle);
            }
        }
    }
}