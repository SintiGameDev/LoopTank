using UnityEngine;
using System.Collections; // Notwendig f�r Coroutinen

public class Timer : MonoBehaviour
{
    [Tooltip("Die Dauer des Timers in Sekunden, einstellbar im Inspector.")]
    [SerializeField] // Macht die private Variable im Inspector sichtbar
    private float timerDuration = 5.0f;

    [Tooltip("Wird auf TRUE gesetzt, wenn der Timer abgelaufen ist, und auf FALSE, wenn er gestartet wird.")]
    public bool TimerEnd { get; private set; } = false; // Property mit privatem Setter

    private Coroutine timerCoroutine; // Speichert die Referenz zur laufenden Coroutine

    /// <summary>
    /// Startet den Timer neu mit der im Inspector eingestellten Dauer.
    /// Setzt TimerEnd auf FALSE.
    /// </summary>
    public void StartTimer()
    {
        // Wenn bereits ein Timer l�uft, stoppe ihn zuerst
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
        }

        TimerEnd = false; // TimerEnd zur�cksetzen, da der Timer neu startet
        timerCoroutine = StartCoroutine(RunTimer()); // Starte die Coroutine
        Debug.Log($"Timer gestartet f�r {timerDuration} Sekunden.");
    }

    /// <summary>
    /// Eine Coroutine, die den Timer herunterz�hlt.
    /// </summary>
    private IEnumerator RunTimer()
    {
        float currentTime = timerDuration;

        while (currentTime > 0)
        {
            yield return null; // Warte einen Frame
            currentTime -= Time.deltaTime; // Z�hle die Zeit herunter
        }

        // Timer ist beendet
        currentTime = 0; // Sicherstellen, dass die Zeit nicht negativ wird
        TimerEnd = true; // TimerEnd auf TRUE setzen
        Debug.Log("Timer beendet!");
    }

    // Optional: Eine Methode zum manuellen Stoppen des Timers, falls ben�tigt
    public void StopTimer()
    {
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
            Debug.Log("Timer manuell gestoppt.");
        }
    }
}