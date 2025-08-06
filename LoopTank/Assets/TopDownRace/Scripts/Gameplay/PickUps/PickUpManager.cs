using UnityEngine;

public class PickUpManager : MonoBehaviour
{
    // Die OnTriggerEnter2D-Methode wird automatisch von Unity aufgerufen,
    // wenn ein Objekt mit einem Collider (und IsTrigger aktiviert)
    // von einem anderen Objekt (dem Spieler) ber�hrt wird.
    private void OnTriggerEnter2D(Collider2D other)
    {
        // �berpr�fe, ob das kollidierende Objekt das "PlayerCar" ist.
        // Daf�r nutzen wir einen Tag. Stelle sicher, dass dein Player-Objekt
        // in Unity den Tag "Player" hat.
        if (other.CompareTag("Player"))
        {
            Debug.Log("Pickup kollidiert mit Spieler! Pickup wird zerst�rt.");

            // Zerst�re das Game-Objekt, an dem dieses Skript h�ngt (also das Pickup selbst).
            Destroy(gameObject);
        }
    }
}