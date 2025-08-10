using UnityEngine;
using System.Collections.Generic;

namespace TopDownRace
{
    public class PickupSpawner : MonoBehaviour
    {
        [Header("Spawner Settings")]
        [Tooltip("Eine Liste von Pickup-Prefabs, die gespawnt werden können. Ziehe die Prefabs hier per Drag-and-Drop rein.")]
        [SerializeField]
        private List<GameObject> m_PickupPrefabs;

        [Tooltip("Die Wahrscheinlichkeit (in Prozent), dass ein Pickup gespawnt wird, wenn der Spawner nicht mehr sichtbar ist.")]
        [Range(0, 100)]
        [SerializeField]
        private int m_SpawnChance = 50;

        [Tooltip("Der Radius, in dem geprüft wird, ob bereits ein Pickup vorhanden ist.")]
        [SerializeField]
        private float m_CheckRadius = 1f;

        // Das zuletzt gespawnte Pickup-Objekt
        private GameObject m_SpawnedPickup;

        // Eine Flag, die sicherstellt, dass die Spawn-Logik nur einmal pro Unsichtbarkeit aufgerufen wird
        private bool m_IsVisible = true;

        void Start()
        {
            if (m_PickupPrefabs == null || m_PickupPrefabs.Count == 0)
            {
                Debug.LogWarning("Die Pickup-Prefab-Liste ist leer im PickupSpawner! Bitte weise Prefabs im Inspector zu.", this);
            }
        }

        // Diese Methode wird von Unity aufgerufen, wenn das Objekt sichtbar wird.
        private void OnBecameVisible()
        {
            m_IsVisible = true;
        }

        // Diese Methode wird von Unity aufgerufen, wenn das Objekt unsichtbar wird.
        private void OnBecameInvisible()
        {
            // Die Spawn-Logik nur aufrufen, wenn das Objekt zuvor sichtbar war.
            if (m_IsVisible)
            {
                TrySpawnPickup();
                m_IsVisible = false; // Zurücksetzen, damit es nur einmal spawnt
            }
        }

        public void TrySpawnPickup()
        {
            if (m_SpawnedPickup != null)
            {
                return;
            }

            if (Random.Range(0, 100) < m_SpawnChance)
            {
                SpawnPickup();
            }
        }

        private void SpawnPickup()
        {
            if (m_PickupPrefabs == null || m_PickupPrefabs.Count == 0)
            {
                return;
            }

            Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, m_CheckRadius);
            foreach (Collider2D hit in hitColliders)
            {
                if (hit.CompareTag("Pickup"))
                {
                    Debug.Log("Ein Pickup existiert bereits in der Nähe, es wird kein neues gespawnt.", this);
                    return;
                }
            }

            int randomIndex = Random.Range(0, m_PickupPrefabs.Count);
            GameObject pickupToSpawn = m_PickupPrefabs[randomIndex];
            m_SpawnedPickup = Instantiate(pickupToSpawn, transform.position, Quaternion.identity);
            m_SpawnedPickup.tag = "Pickup";

            // Optional: Parent setzen, damit es übersichtlicher bleibt
            // m_SpawnedPickup.transform.SetParent(transform);
        }

        public void ClearSpawnedPickup()
        {
            m_SpawnedPickup = null;
        }
    }
}