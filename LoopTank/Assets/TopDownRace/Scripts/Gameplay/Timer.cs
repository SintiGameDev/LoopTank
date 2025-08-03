using System.Collections; // Notwendig für Coroutinen
using TMPro; // Wichtig: Füge dies hinzu, wenn du TextMeshPro verwendest!
using TopDownRace;
using UnityEngine;
             // Wenn du den alten UI.Text verwendest, nimm: using UnityEngine.UI;

public class Timer : MonoBehaviour
{
    [Tooltip("Die Dauer des Timers in Sekunden, einstellbar im Inspector.")]
    [SerializeField] // Macht die private Variable im Inspector sichtbar
    private float timerDuration = 5.0f;

    [Tooltip("Wird auf TRUE gesetzt, wenn der Timer abgelaufen ist, und auf FALSE, wenn er gestartet wird.")]
    public bool TimerEnd { get; private set; } = false; // Property mit privatem Setter

    private Coroutine timerCoroutine; // Speichert die Referenz zur laufenden Coroutine

    // NEU: Referenz auf das UI-Text-Element
    private TextMeshProUGUI timerTextUI; // Ändere dies zu 'Text timerTextUI;', wenn du UnityEngine.UI.Text verwendest

    void Awake()
    {
        // Finde das UI-Text-Element mit dem Tag "Timer"
        // Es ist wichtig, dass dieses GameObject in deiner Szene existiert und den korrekten Tag hat.
        GameObject timerTextObject = GameObject.FindWithTag("Timer");
        if (timerTextObject != null)
        {
            // Versuche, die Text-Komponente zu bekommen
            timerTextUI = timerTextObject.GetComponent<TextMeshProUGUI>(); // Oder .GetComponent<Text>();
            if (timerTextUI == null)
            {
                Debug.LogError("GameObject mit Tag 'Timer' gefunden, aber es hat keine TextMeshProUGUI-Komponente (oder UnityEngine.UI.Text-Komponente).", timerTextObject);
            }
            else { 
            timerTextUI.text = string.Format("{0:00}:{1:00}", Mathf.FloorToInt(timerDuration / 60), Mathf.FloorToInt(timerDuration % 60)); // Initiale Anzeige
            }
        }
        else
        {
            Debug.LogError("Kein GameObject mit dem Tag 'Timer' in der Szene gefunden. Der Timer-Text kann nicht aktualisiert werden.");
        }
    }

    /// <summary>
    /// Startet den Timer neu mit der im Inspector eingestellten Dauer.
    /// Setzt TimerEnd auf FALSE.
    /// </summary>
    public void StartTimer()
    {
        // Wenn bereits ein Timer läuft, stoppe ihn zuerst
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
        }

        TimerEnd = false; // TimerEnd zurücksetzen, da der Timer neu startet
        timerCoroutine = StartCoroutine(RunTimer()); // Starte die Coroutine
        Debug.Log($"Timer gestartet für {timerDuration} Sekunden.");

        // Timer-UI beim Start aktualisieren
        UpdateTimerUI(timerDuration);
    }

    /// <summary>
    /// Eine Coroutine, die den Timer herunterzählt.
    /// </summary>
    private IEnumerator RunTimer()
    {
        float currentTime = timerDuration;

        while (currentTime > 0)
        {
            Debug.Log($"Timer läuft: {currentTime} Sekunden verbleibend.");
            // Aktualisiere die UI in jedem Frame
            UpdateTimerUI(currentTime);

            yield return null; // Warte einen Frame
            currentTime -= Time.deltaTime; // Zähle die Zeit herunter
        }

        // Timer ist beendet
        currentTime = 0; // Sicherstellen, dass die Zeit nicht negativ wird
        UpdateTimerUI(currentTime); // Letztes Update, um 00:00 anzuzeigen
        TimerEnd = true; // TimerEnd auf TRUE setzen
        Debug.Log("Timer beendet!");
        PlayerCar playerCar = PlayerCar.m_Current; // Hole die aktuelle Spieler-Auto-Instanz
        PlayerCar.m_Current.m_Control = false;
        UISystem.ShowUI("win-ui");
        //GameControl.m_WonRace = true;
        //// Replace the line causing the error:
        //GameControl.m_WonRace = true;

        // With the following code:
        if (GameControl.m_Current != null)
        {
            GameControl.m_Current.m_WonRace = true;
        }
        else
        {
            Debug.LogError("GameControl.m_Current is null. Cannot set m_WonRace.");
        }
    }

    /// <summary>
    /// Aktualisiert das UI-Text-Element mit der gegebenen Zeit.
    /// </summary>
    /// <param name="timeToDisplay">Die verbleibende Zeit in Sekunden.</param>
    private void UpdateTimerUI(float timeToDisplay)
    {
        if (timerTextUI != null)
        {
            // Berechne Minuten und Sekunden
            int minutes = Mathf.FloorToInt(timeToDisplay / 60);
            int seconds = Mathf.FloorToInt(timeToDisplay % 60);

            // Formatieren als MM:SS
            timerTextUI.text = string.Format("{0:00}:{1:00}", minutes, seconds);
            Debug.Log($"Timer aktualisiert: {timerTextUI.text}");
        }
    }

    // Optional: Eine Methode zum manuellen Stoppen des Timers, falls benötigt
    public void StopTimer()
    {
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
            Debug.Log("Timer manuell gestoppt.");
            // Optional: UI auch hier aktualisieren, z.B. auf 0 oder eine Meldung
             UpdateTimerUI(0);
        }
    }
}