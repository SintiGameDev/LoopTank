using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TopDownRace
{
    public class GhostManager : MonoBehaviour
    {
        public static GhostManager Instance { get; private set; }

        [Header("References")]
        public LapRecorder playerRecorder;
        public GameObject ghostPrefab;
        public Transform ghostParent;

        public enum GhostMode { Off, LastLap, BestLap }

        [Header("Ghost Mode")]
        public GhostMode mode = GhostMode.LastLap;

        [Header("Limits")]
        public int maxGhosts = 2;

        [Header("Sounds")]
        [Tooltip("Der Sound, der abgespielt wird, wenn eine Runde erfolgreich beendet wurde.")]
        public AudioClip m_LapFinishedSound;
        [Tooltip("Die Lautstärke des 'Runde beendet'-Sounds.")]
        [Range(0.0f, 1.0f)]
        public float m_LapFinishedVolume = 1.0f;
        private AudioSource m_AudioSource;

        LapData lastLap;
        LapData bestLap;

        private List<GhostReplay> ghostInstances = new List<GhostReplay>();

        // Timer
        public Timer roundTimer;
        private bool timerStarted = false;
        private bool timerUiShown = true;

        private bool hasSpawnedFirstGhost = false;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Multiple GhostManager instances found! Destroying duplicate.");
                return;
            }
            Instance = this;
        }

        void Start()
        {
            m_AudioSource = GetComponent<AudioSource>();
            if (m_AudioSource == null)
            {
                m_AudioSource = gameObject.AddComponent<AudioSource>();
                m_AudioSource.playOnAwake = false;
            }
        }

        public void OnLapStarted()
        {
            playerRecorder.BeginLap();
        }

        public void OnLapFinished(float lapTime)
        {
            playerRecorder.EndLap(lapTime);

            if (playerRecorder.currentLap == null || playerRecorder.currentLap.frames.Count < 2)
            {
                Debug.LogWarning("Current lap data is empty or invalid. Cannot create ghost.");
                return;
            }

            // Spiele den 'Runde beendet'-Sound ab, wenn er zugewiesen ist
            if (m_LapFinishedSound != null && m_AudioSource != null)
            {
                m_AudioSource.PlayOneShot(m_LapFinishedSound, m_LapFinishedVolume);
            }

            lastLap = Clone(playerRecorder.currentLap);

            if (bestLap == null || lapTime < bestLap.lapTime)
                bestLap = Clone(playerRecorder.currentLap);

            LapData toReplay = null;
            if (mode == GhostMode.LastLap) toReplay = lastLap;
            else if (mode == GhostMode.BestLap) toReplay = bestLap;

            if (!hasSpawnedFirstGhost)
            {
                hasSpawnedFirstGhost = true;
                return;
            }

            if (toReplay != null)
            {
                StartCoroutine(SpawnAndActivateGhostDelayed(Clone(toReplay), 0.8f));
            }
        }

        private IEnumerator SpawnAndActivateGhostDelayed(LapData ghostLap, float delaySeconds)
        {
            var go = Instantiate(
                ghostPrefab,
                playerRecorder.transform.position,
                playerRecorder.transform.rotation,
                ghostParent
            );
            var ghost = go.GetComponent<GhostReplay>();

            var spriteRenderers = ghost.GetComponentsInChildren<SpriteRenderer>();
            foreach (var item in spriteRenderers)
            {
                if (item != null)
                    item.gameObject.SetActive(true);
            }

            var collisionIgnorer = FindDeepChild(go.transform, "CollisionIgnorer");
            if (collisionIgnorer != null)
            {
                collisionIgnorer.gameObject.SetActive(true);
                StartCoroutine(DisableAfterSeconds(collisionIgnorer.gameObject, 1f, ghost));
            }

            ghostInstances.Add(ghost);
            if (ghostInstances.Count > maxGhosts)
            {
                Destroy(ghostInstances[0].gameObject);
                ghostInstances.RemoveAt(0);
            }

            yield return new WaitForSeconds(delaySeconds);

            if (ghost != null)
                ghost.Play(ghostLap);
        }

        private IEnumerator DisableAfterSeconds(GameObject obj, float seconds, GhostReplay ghost)
        {
            yield return new WaitForSeconds(seconds);
            if (obj != null)
            {
                obj.SetActive(false);
                obj.tag = "Untagged";

                var spriteRenderers = ghost.GetComponentsInChildren<SpriteRenderer>();
                foreach (var item in spriteRenderers)
                {
                    if (item != null)
                        item.gameObject.SetActive(true);
                }
            }
        }

        private Transform FindDeepChild(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name)
                    return child;
                var result = FindDeepChild(child, name);
                if (result != null)
                    return result;
            }
            return null;
        }

        LapData Clone(LapData src)
        {
            var c = ScriptableObject.CreateInstance<LapData>();
            c.lapTime = src.lapTime;
            foreach (var f in src.frames)
                c.frames.Add(new LapFrame { t = f.t, pos = f.pos, rotZ = f.rotZ });
            return c;
        }

        public void ClearAllGhosts()
        {
            foreach (var ghost in ghostInstances)
                if (ghost != null) Destroy(ghost.gameObject);
            ghostInstances.Clear();
        }

        public void RemoveGhost(GhostReplay ghost)
        {
            if (ghostInstances.Contains(ghost))
            {
                Destroy(ghost.gameObject);
                ghostInstances.Remove(ghost);
            }
        }

        public void SetMode(int m) { mode = (GhostMode)m; }

        public IReadOnlyList<GhostReplay> ActiveGhosts => ghostInstances;

        private IEnumerator BlinkSprite(GameObject spriteObj)
        {
            if (spriteObj == null) yield break;
            SpriteRenderer spriteRenderer = spriteObj.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null) yield break;

            for (int i = 0; i < 3; i++)
            {
                spriteRenderer.color = new Color(1f, 1f, 1f, 1f);
                yield return new WaitForSeconds(0.1f);
                spriteRenderer.color = new Color(1f, 1f, 1f, 0f);
                yield return new WaitForSeconds(0.1f);
            }
            spriteRenderer.color = new Color(1f, 1f, 1f, 1f);
        }
    }
}