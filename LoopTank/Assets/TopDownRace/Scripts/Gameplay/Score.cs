using UnityEngine;
using System; // Notwendig f�r Action

public class Score : MonoBehaviour
{
    // Statische Instanz des Score-Skripts, f�r einfachen Zugriff von �berall.
    public static Score Instance { get; private set; }

    // Dies speichert den aktuellen Punktestand.
    // [SerializeField] macht es im Inspector sichtbar, obwohl es private ist (gute Praxis).
    [SerializeField]
    private int currentScore = 0;

    // Eine Property, um den Punktestand von anderen Skripten aus lesbar zu machen.
    // Der Setter ist privat, damit der Punktestand nur �ber die AddScore-Methode ge�ndert werden kann.
    public int CurrentScore
    {
        get { return currentScore; }
        private set
        {
            currentScore = value;
            // L�se das Event aus, wenn sich der Punktestand �ndert.
            // Das ist super n�tzlich, wenn du z.B. eine UI hast, die den Score anzeigt.
            OnScoreChanged?.Invoke(currentScore);
        }
    }

    // Ein Event, das ausgel�st wird, wenn sich der Punktestand �ndert.
    // Andere Skripte k�nnen sich hier registrieren, um auf �nderungen zu reagieren (z.B. UI-Aktualisierung).
    public static event Action<int> OnScoreChanged;

    void Awake()
    {
        // Sicherstellen, dass nur eine Instanz dieses Score-Managers existiert.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // Zerst�re doppelte Instanzen
        }
        else
        {
            Instance = this; // Setze diese Instanz als die globale Instanz
            // Optional: Wenn du den Score �ber Szenenwechsel hinweg beibehalten m�chtest
            // DontDestroyOnLoad(gameObject); 
        }

        // Setze den Punktestand beim Start auf 0.
        // Die Property wird verwendet, damit das OnScoreChanged-Event auch beim Initialisieren ausgel�st wird.
        CurrentScore = 0;
    }

    /// <summary>
    /// Erh�ht den Punktestand um den angegebenen Wert.
    /// Dies ist die Methode, die du aufrufen solltest, wenn ein Ghost erfolgreich gespawnt wird.
    /// </summary>
    /// <param name="amount">Der Wert, um den der Punktestand erh�ht werden soll. Standard ist 1.</param>
    public void AddScore(int amount = 1)
    {
        // Der Setter der CurrentScore-Property wird verwendet, 
        // der automatisch das OnScoreChanged-Event ausl�st.
        CurrentScore += amount;
        Debug.Log($"Score erh�ht! Neuer Punktestand: {CurrentScore}");
    }

    /// <summary>
    /// Setzt den Punktestand auf 0 zur�ck.
    /// N�tzlich am Anfang eines neuen Spiels oder einer neuen Runde.
    /// </summary>
    public void ResetScore()
    {
        // Der Setter der CurrentScore-Property wird verwendet.
        CurrentScore = 0;
        Debug.Log("Punktestand zur�ckgesetzt.");
    }

    // Beispielnutzung f�r Debugging (kann entfernt werden)
    void Update()
    {
        // Nur zum Testen: Dr�cke 'S', um den Score zu erh�hen
        // und 'R', um ihn zur�ckzusetzen.
        if (Input.GetKeyDown(KeyCode.S))
        {
            AddScore(); // F�gt 1 Punkt hinzu
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetScore();
        }
    }
}