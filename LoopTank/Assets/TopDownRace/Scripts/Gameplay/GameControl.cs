using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements; // Beachte: UnityEngine.UIElements ist für das neue UI-Toolkit, UnityEngine.UI für das alte.

namespace TopDownRace
{
    public class GameControl : MonoBehaviour
    {
        public int m_levelRounds;
        [HideInInspector]
        public int m_FinishedLaps;

        public static GameControl m_Current;

        public GameObject m_PlayerCarPrefab;
        public GameObject m_RivalCarPrefab;

        [HideInInspector]
        public int m_PlayerPosition;

        [HideInInspector]
        public GameObject[] m_Cars;

        [HideInInspector]
        public bool m_LostRace;
        [HideInInspector]
        public bool m_WonRace;

        [HideInInspector]
        public int m_StartTimer;

        [HideInInspector]
        public bool m_StartRace;
        public static bool m_restartGame;

        //Timer
        public Timer roundTimer; // Im Inspector zuweisen!
        private bool timerStarted = false; // Merkt, ob der Timer schon gestartet wurde
        private bool timerUiShown = true; // Verhindert mehrfaches Anzeigen der UI

        private void Awake()
        {
            GhostManager.Instance.ClearAllGhosts();

            m_LostRace = false;
            m_WonRace = false;
            m_StartRace = false;
            m_FinishedLaps = 0;
            m_Current = this;
            m_restartGame = true;
        }
        // Start is called before the first frame update
        void Start()
        {
            m_Cars = new GameObject[4];

            // Player spawnen
            GameObject playerCar = Instantiate(m_PlayerCarPrefab);
            playerCar.transform.position = RaceTrackControl.m_Main.m_StartPositions[0].position;
            playerCar.transform.rotation = RaceTrackControl.m_Main.m_StartPositions[0].rotation;
            m_Cars[0] = playerCar;

            // ➜ LapRecorder sicherstellen (falls nicht schon am Prefab)
            var recorder = playerCar.GetComponent<LapRecorder>();
            if (recorder == null) recorder = playerCar.AddComponent<LapRecorder>();

            // ➜ GhostManager mit Recorder füttern
            GhostManager.Instance.playerRecorder = recorder;

            // (Optional) gleich Ghost für die neue Runde vorbereiten
            GhostManager.Instance.OnLapStarted();

            // Rivals spawnen (unverändert)
            for (int i = 1; i < 4; i++)
            {
                if (m_RivalCarPrefab == null)
                {
                    continue;
                }
                GameObject rivalCar = Instantiate(m_RivalCarPrefab);
                rivalCar.transform.position = RaceTrackControl.m_Main.m_StartPositions[i].position;
                rivalCar.transform.rotation = RaceTrackControl.m_Main.m_StartPositions[i].rotation;
                m_Cars[i] = rivalCar;
            }

            m_PlayerPosition = 0;
            StartCoroutine(Co_StartRace());
        }


        // Update is called once per frame
        void Update()
        {
            int position = 0;
            int playerPoint = 0;
            if (RaceTrackControl.m_Main == null || RaceTrackControl.m_Main.m_Checkpoints == null || RaceTrackControl.m_Main.m_Checkpoints.Length == 0)
            {
                playerPoint = 1000; // Default Wert, falls keine Checkpoints definiert sind
            }
            else
            {
                playerPoint = m_FinishedLaps * RaceTrackControl.m_Main.m_Checkpoints.Length + PlayerCar.m_Current.m_CurrentCheckpoint;
            }
            for (int i = 1; i < 4; i++)
            {
                if (m_Cars[i] == null || m_Cars[i].GetComponent<Rivals>() == null) continue;
                int rivalPoint = m_Cars[i].GetComponent<Rivals>().m_FinishedLaps * RaceTrackControl.m_Main.m_Checkpoints.Length + m_Cars[i].GetComponent<Rivals>().m_WaypointsCounter;
                if (playerPoint < rivalPoint)
                {
                    position++;
                }
            }

            m_PlayerPosition = position;
        }

        public bool PlayerLapEndCheck()
        {
            // NEU: 15 Sekunden zum Timer hinzufügen, wenn eine Runde beendet wurde
            // Dies geschieht, bevor geprüft wird, ob das Rennen gewonnen ist.
            if (roundTimer != null)
            {
                roundTimer.AddTime(15f); // Füge 15 Sekunden hinzu
            }
            else
            {
                Debug.LogWarning("GameControl: roundTimer ist nicht zugewiesen oder gefunden. Kann keine Zeit hinzufügen.");
            }

            if (m_FinishedLaps == m_levelRounds)
            {
                if (!m_LostRace)
                {
                    PlayerCar.m_Current.m_Control = false;
                    UISystem.ShowUI("win-ui");
                    m_WonRace = true;
                }
                else
                {
                    PlayerCar.m_Current.m_Control = false;
                    UISystem.ShowUI("lose-ui");
                    // Finde Objekt mit dem Tag "Score" und setze den Text auf m_FinishedLaps
                    var scoreText = GameObject.FindGameObjectsWithTag("Score");
                    if (scoreText.Length > 0 && scoreText[0].GetComponent<UnityEngine.UI.Text>() != null)
                    {
                        scoreText[0].GetComponent<UnityEngine.UI.Text>().text = m_FinishedLaps.ToString();
                    }
                    else
                    {
                        Debug.LogWarning("Score-Text-Objekt mit Tag 'Score' nicht gefunden oder hat keine UnityEngine.UI.Text-Komponente.");
                    }
                }
                return true;
            }

            return false;
        }

        public void RivalsLapEndCheck(Rivals rival)
        {
            if (rival.m_FinishedLaps == m_levelRounds)
            {
                if (!m_WonRace)
                {
                    m_LostRace = true;
                }
            }
        }


        IEnumerator Co_StartRace()
        {
            m_StartTimer = 3;
            yield return new WaitForSeconds(1.5f);
            m_StartTimer--;
            yield return new WaitForSeconds(1);
            m_StartTimer--;
            yield return new WaitForSeconds(1);
            m_StartTimer--;
            m_StartRace = true;

            // ➜ Jetzt beginnt die erste Runde wirklich:
            StartLapTimer();
            // ➜ Hier den GhostManager informieren, dass die Runde begonnen hat:

            if (roundTimer == null)
            {
                //find timer automatically if not set
                GameObject timerObject = GameObject.FindGameObjectWithTag("Timer");
                if (timerObject != null)
                {
                    roundTimer = timerObject.GetComponent<Timer>();
                    if (roundTimer == null)
                    {
                        Debug.LogError("GameControl: The object with tag 'Timer' does not have a Timer component.");
                    }
                }
                else
                {
                    Debug.LogError("GameControl: No GameObject with tag 'Timer' found!");
                }
                if (roundTimer == null)
                {
                    Debug.LogError("GameControl: No Timer found! Please assign a Timer in the Inspector or ensure one exists in the scene.");
                    yield break; // Ensure we exit the coroutine if no timer is found
                }
            }
            // TIMER NUR BEI ERSTER RUNDE STARTEN
            if (!timerStarted && roundTimer != null)
            {
                Debug.Log("GameControl: Starting round timer.");
                roundTimer.StartTimer();
                timerStarted = true;
                timerUiShown = false; // falls z.B. Reset erlaubt ist
            }
        }

        ///----------------Gemini-------------------

        private float m_LapStartTime;

        public float GetCurrentLapTime()
        {
            return Time.time - m_LapStartTime;
        }

        // Rufe das beim Start einer neuen Runde auf:
        public void StartLapTimer()
        {
            m_LapStartTime = Time.time;
        }
    }
}