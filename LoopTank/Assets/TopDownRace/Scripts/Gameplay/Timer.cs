using System.Collections;
using TMPro;
using TopDownRace;
using UnityEngine;
using System.Collections.Generic; // Notwendig für List<T>

public class Timer : MonoBehaviour
{
    [Tooltip("Die Dauer des Timers in Sekunden, einstellbar im Inspector.")]
    [SerializeField]
    private float timerDuration = 5.0f;

    [Tooltip("Wird auf TRUE gesetzt, wenn der Timer abgelaufen ist, und auf FALSE, wenn er gestartet wird.")]
    public bool TimerEnd { get; private set; } = false;

    private Coroutine timerCoroutine;

    public TextMeshProUGUI timerTextUI;

    // NEU: Referenz für deinen Prefab
    [Tooltip("Der Prefab, der zusammen mit dem Timer aktiviert werden soll.")]
    public GameObject prefabToActivate;

    private float m_CurrentTime;
    private int m_LastSecondDisplayed; // NEU: Speichert die letzte angezeigte Sekunde

    // NEU: LeanTween Skalierungseinstellungen
    [Header("LeanTween Scale Animation")]
    [Tooltip("Der Skalierungsfaktor für den Tween-Effekt.")]
    [SerializeField]
    private float m_ScaleFactor = 1.2f;
    [Tooltip("Die Dauer des Tween-Effekts in Sekunden.")]
    [SerializeField]
    private float m_TweenDuration = 0.2f;
    [Tooltip("Der Easing-Typ für den Tween-Effekt.")]
    [SerializeField]
    private LeanTweenType m_EaseType = LeanTweenType.easeOutCubic;

    void Awake()
    {
        // ... (Dein ursprünglicher Awake-Code) ...
        GameObject timerTextObject = GameObject.FindWithTag("Timer");
        if (timerTextObject != null)
        {
            timerTextUI = timerTextObject.GetComponent<TextMeshProUGUI>();
            if (timerTextUI == null)
            {
                Debug.LogError("GameObject mit Tag 'Timer' gefunden, aber es hat keine TextMeshProUGUI-Komponente (oder UnityEngine.UI.Text-Komponente).", timerTextObject);
            }
            else
            {
                // Deaktiviere das UI-Element, bis der Timer startet
                timerTextUI.gameObject.SetActive(false);
                m_CurrentTime = timerDuration;
                // Setze den Text initial, aber unsichtbar
                timerTextUI.text = string.Format("{0:00}:{1:00}", Mathf.FloorToInt(m_CurrentTime / 60), Mathf.FloorToInt(m_CurrentTime % 60));
                m_LastSecondDisplayed = Mathf.FloorToInt(m_CurrentTime % 60);
            }
        }
        else
        {
            Debug.LogError("Kein GameObject mit dem Tag 'Timer' in der Szene gefunden. Der Timer-Text kann nicht aktualisiert werden.");
        }

        if (prefabToActivate != null)
        {
            prefabToActivate.SetActive(false);
        }
    }

    public void StartTimer()
    {
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
        }

        if (timerTextUI != null)
        {
            timerTextUI.gameObject.SetActive(true); // Aktiviere den Text
            timerTextUI.transform.localScale = Vector3.one;
        }

        if (prefabToActivate != null)
        {
            prefabToActivate.SetActive(true);
        }

        TimerEnd = false;
        m_CurrentTime = timerDuration;
        m_LastSecondDisplayed = Mathf.FloorToInt(m_CurrentTime % 60) + 1; // Setze die letzte Sekunde zurück, um den ersten Tween auszulösen

        timerCoroutine = StartCoroutine(RunTimer());
        Debug.Log($"Timer gestartet für {timerDuration} Sekunden.");

        UpdateTimerUI(m_CurrentTime);
    }

    // ... Rest des Codes bleibt unverändert ...

    private IEnumerator RunTimer()
    {
        while (m_CurrentTime > 0)
        {
            UpdateTimerUI(m_CurrentTime);
            yield return null;
            m_CurrentTime -= Time.deltaTime;
        }

        m_CurrentTime = 0;
        UpdateTimerUI(m_CurrentTime);
        TimerEnd = true;
        Debug.Log("Timer beendet!");

        if (PlayerCar.m_Current != null)
        {
            PlayerCar.m_Current.m_Control = false;
        }

        // NEU: Verwenden Sie hier die Null-Prüfung und UISystem.FindOpenUIByName
        if (UISystem.FindOpenUIByName("win-ui") == null)
        {
            UISystem.ShowUI("win-ui");
        }

        if (GameControl.m_Current != null)
        {
            GameControl.m_Current.m_WonRace = true;
        }
        else
        {
            Debug.LogError("GameControl.m_Current is null. Cannot set m_WonRace.");
        }

        // NEU: Deaktiviere den Prefab, wenn der Timer abläuft
        if (prefabToActivate != null)
        {
            prefabToActivate.SetActive(false);
        }
    }

    private void UpdateTimerUI(float timeToDisplay)
    {
        // NEU: Prüfe, ob eine "lose-ui" aktiv ist und stoppe den Timer, um Konflikte zu vermeiden
        if (UISystem.FindOpenUIByName("lose-ui") != null)
        {
            StopTimer();
            if (timerTextUI != null) timerTextUI.gameObject.SetActive(false);
            if (prefabToActivate != null) prefabToActivate.SetActive(false);
            return;
        }

        if (timerTextUI != null)
        {
            int minutes = Mathf.FloorToInt(timeToDisplay / 60);
            int seconds = Mathf.FloorToInt(timeToDisplay % 60);
            timerTextUI.text = string.Format("{0:00}:{1:00}", minutes, seconds);

            if (seconds != m_LastSecondDisplayed)
            {
                LeanTween.cancel(timerTextUI.gameObject);
                timerTextUI.transform.localScale = Vector3.one;
                LeanTween.scale(timerTextUI.gameObject, Vector3.one * m_ScaleFactor, m_TweenDuration)
                    .setEase(m_EaseType)
                    .setLoopPingPong(1);

                m_LastSecondDisplayed = seconds;
            }
        }
    }

    public void AddTime(float secondsToAdd)
    {
        m_CurrentTime += secondsToAdd;
        // NEU: Hier musst du die timerDuration auch aktualisieren, damit der Timer richtig initialisiert wird, wenn er neu gestartet wird
        timerDuration += secondsToAdd;
        Debug.Log($"Timer: {secondsToAdd} Sekunden hinzugefügt. Neue Zeit: {m_CurrentTime:F2}s");
        UpdateTimerUI(m_CurrentTime);
    }

    public void StopTimer()
    {
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
            Debug.Log("Timer manuell gestoppt.");
            // Deaktiviere die UI-Elemente, wenn der Timer stoppt
            if (timerTextUI != null) timerTextUI.gameObject.SetActive(false);
            if (prefabToActivate != null) prefabToActivate.SetActive(false);
        }
    }
}