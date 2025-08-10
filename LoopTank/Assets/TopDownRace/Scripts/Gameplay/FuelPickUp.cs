using UnityEngine;

namespace TopDownRace
{
    public class FuelPickup : MonoBehaviour
    {
        [Header("Fuel Pickup Settings")]
        [Tooltip("Die Menge an Kraftstoff, die dem Spieler hinzugefügt wird.")]
        [SerializeField]
        private float m_FuelAmount = 25f;

        [Tooltip("Der Sound, der abgespielt wird, wenn der Spieler das Pickup aufnimmt.")]
        [SerializeField]
        private AudioClip m_PickupSound;

        private AudioSource m_AudioSource;
        private SpriteRenderer m_SpriteRenderer;

        void Start()
        {
            m_AudioSource = GetComponent<AudioSource>();
            if (m_AudioSource == null)
            {
                // Fügt eine AudioSource hinzu, falls keine vorhanden ist
                m_AudioSource = gameObject.AddComponent<AudioSource>();
            }
            m_SpriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // Prüft, ob der kollidierende Collider zum Spieler gehört
            if (other.CompareTag("Player"))
            {
                // Rufe die Methode im FuelMechanic-Skript auf, um den Kraftstoff aufzufüllen
                if (FuelMechanic.Instance != null)
                {
                    FuelMechanic.Instance.AddFuel(m_FuelAmount);
                }

                // Finde den Spawner und sage ihm, dass der Platz wieder frei ist
                PickupSpawner spawner = transform.parent.GetComponent<PickupSpawner>();
                if (spawner != null)
                {
                    spawner.ClearSpawnedPickup();
                }

                // Spielt den Soundeffekt ab (falls vorhanden)
                if (m_PickupSound != null && m_AudioSource != null)
                {
                    m_AudioSource.PlayOneShot(m_PickupSound);
                }

                // Versteckt das Sprite, damit es nicht mehr sichtbar ist, aber der Sound noch abgespielt werden kann.
                if (m_SpriteRenderer != null)
                {
                    m_SpriteRenderer.enabled = false;
                }

                // Zerstört das GameObject nach einer kurzen Verzögerung, damit der Sound zu Ende gespielt werden kann
                Destroy(gameObject, m_PickupSound != null ? m_PickupSound.length : 0f);
            }
        }
    }
}