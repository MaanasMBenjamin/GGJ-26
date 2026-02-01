using System.Linq;
using UnityEngine;

/// <summary>
/// Triggers Game Over when the player is within a configurable radius of any enemy.
/// Freezes the game and pauses background music.
/// Attach this to the Player (or a manager) and configure options in the Inspector.
/// </summary>
public class GameOverProximity : MonoBehaviour
{
    [Header("Detection Settings")]
    [SerializeField] private float triggerRadius = 0.9f; // editable
    [SerializeField] private float checkInterval = 0.05f; // how often to check (seconds)
    [Tooltip("Use tag-based detection of enemies. If false, uses LayerMask.")]
    [SerializeField] private bool useEnemyTag = true;
    [SerializeField] private string enemyTag = "Enemy";
    [Tooltip("Layer(s) containing enemies when tag mode is off.")]
    [SerializeField] private LayerMask enemyLayers;
    [Tooltip("If true, requires clear line-of-sight (no obstacles) to enemy before triggering.")]
    [SerializeField] private bool requireLineOfSight = false;
    [SerializeField] private LayerMask lineOfSightBlockers; // layers that block sight (e.g., Walls)

    [Header("Player Reference")]
    [Tooltip("Optional: assign player Transform. If null, uses this GameObject's Transform.")]
    [SerializeField] private Transform player;

    [Header("Audio Freeze Settings")]
    [Tooltip("Pause all audio via AudioListener.pause (recommended). If false, uses a specific background music AudioSource.")]
    [SerializeField] private bool pauseAllAudioViaListener = true;
    [Tooltip("Optional: background music AudioSource to pause when freezing.")]
    [SerializeField] private AudioSource backgroundMusic;
    [Tooltip("Fallback: find a GameObject tagged 'Music' and pause its AudioSource.")]
    [SerializeField] private bool tryFindMusicByTag = true;
    [Tooltip("Aggressively pause every AudioSource in the scene (overrides listener pause).")]
    [SerializeField] private bool forcePauseAllSources = true;

    [Header("State & Events")]
    [SerializeField] private bool debugLogs = true;
    private bool isGameFrozen;
    private float nextCheckTime;

    [Header("Script Disable Options")]
    [Tooltip("Disable player control scripts when freezing.")]
    [SerializeField] private bool disablePlayerScriptsOnFreeze = true;
    [Tooltip("Disable enemy AI/movement scripts when freezing.")]
    [SerializeField] private bool disableEnemyScriptsOnFreeze = true;
    [Tooltip("Disable ALL scripts in the scene when freezing (hard stop).")]
    [SerializeField] private bool disableAllScriptsInScene = false;
    [Tooltip("Tag used to find the player if Player Transform is not assigned.")]
    [SerializeField] private string playerTag = "Player";

    private void Awake()
    {
        if (player == null)
        {
            // Try to find the player via tag; else use self
            var go = GameObject.FindGameObjectWithTag("Player");
            player = go != null ? go.transform : transform;
        }
    }

    private void Update()
    {
        if (isGameFrozen) return;
        if (checkInterval > 0f && Time.unscaledTime < nextCheckTime) return;
        nextCheckTime = Time.unscaledTime + Mathf.Max(0.01f, checkInterval);
        var pos = player != null ? (Vector2)player.position : (Vector2)transform.position;

        bool enemyNearby = false;
        if (useEnemyTag)
        {
            // Tag-based detection: check colliders within radius and look for the enemy tag
            var hits = Physics2D.OverlapCircleAll(pos, triggerRadius);
            for (int i = 0; i < hits.Length; i++)
            {
                var h = hits[i];
                if (h != null && h.CompareTag(enemyTag)) { enemyNearby = true; break; }
                // Also allow checking attached Rigidbody2D/GameObject tag in case collider isn't tagged
                if (!enemyNearby && h != null && h.attachedRigidbody != null)
                {
                    var rbGo = h.attachedRigidbody.gameObject;
                    if (rbGo != null && rbGo.CompareTag(enemyTag)) { enemyNearby = true; break; }
                }
            }
            // Fallback: transform distance if colliders don't carry the tag
            if (!enemyNearby)
            {
                var enemies = GameObject.FindGameObjectsWithTag(enemyTag);
                for (int i = 0; i < enemies.Length; i++)
                {
                    var e = enemies[i];
                    if (e == null) continue;
                    float dist = Vector2.Distance(pos, e.transform.position);
                    if (dist <= triggerRadius) { enemyNearby = true; break; }
                }
            }
        }
        else
        {
            // Layer-based detection: single OverlapCircle using LayerMask
            var hit = Physics2D.OverlapCircle(pos, triggerRadius, enemyLayers);
            enemyNearby = hit != null;
            if (!enemyNearby)
            {
                // Fallback: scan transforms by layer
                var all = FindObjectsOfType<Transform>(true);
                for (int i = 0; i < all.Length; i++)
                {
                    var t = all[i];
                    if (((1 << t.gameObject.layer) & enemyLayers.value) == 0) continue;
                    float dist = Vector2.Distance(pos, t.position);
                    if (dist <= triggerRadius) { enemyNearby = true; break; }
                }
            }
        }

        if (enemyNearby)
        {
            if (!requireLineOfSight || HasLineOfSight(pos))
            {
                TriggerGameOverFreeze();
            }
        }
    }

    private bool HasLineOfSight(Vector2 playerPos)
    {
        // Check for any enemy within radius that is not obstructed by blockers
        if (useEnemyTag)
        {
            var enemies = GameObject.FindGameObjectsWithTag(enemyTag);
            for (int i = 0; i < enemies.Length; i++)
            {
                var e = enemies[i];
                if (e == null) continue;
                float dist = Vector2.Distance(playerPos, e.transform.position);
                if (dist > triggerRadius) continue;
                var hit = Physics2D.Linecast(playerPos, e.transform.position, lineOfSightBlockers);
                if (hit.collider == null) return true; // clear sight
            }
        }
        else
        {
            var all = FindObjectsOfType<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                var t = all[i];
                if (((1 << t.gameObject.layer) & enemyLayers.value) == 0) continue;
                float dist = Vector2.Distance(playerPos, t.position);
                if (dist > triggerRadius) continue;
                var hit = Physics2D.Linecast(playerPos, t.position, lineOfSightBlockers);
                if (hit.collider == null) return true;
            }
        }
        return false;
    }

    private void TriggerGameOverFreeze()
    {
        if (isGameFrozen) return;
        isGameFrozen = true;

        // Freeze time
        Time.timeScale = 0f;
        if (debugLogs) Debug.Log("[GameOverProximity] Game Over: player entered enemy radius; freezing game.");

        // Pause audio
        PauseAudio();

        // Freeze physics and animations (optional but more robust than timeScale alone)
        FreezeWorld();

        // Disable movement and input scripts (player + enemies or whole scene)
        DisableScripts();

        // Optionally: raise events or show UI - left to user scene/UI system
    }

    private void PauseAudio()
    {
        if (pauseAllAudioViaListener)
        {
            AudioListener.pause = true;
            return;
        }
        if (backgroundMusic != null)
        {
            if (backgroundMusic.isPlaying) backgroundMusic.Pause();
            return;
        }
        if (tryFindMusicByTag)
        {
            var musicGo = GameObject.FindGameObjectWithTag("Music");
            var musicSrc = musicGo != null ? musicGo.GetComponent<AudioSource>() : null;
            if (musicSrc != null && musicSrc.isPlaying)
            {
                musicSrc.Pause();
                return;
            }
        }
        // Fallback if nothing found
        AudioListener.pause = true;

        if (forcePauseAllSources)
        {
            var sources = FindObjectsOfType<AudioSource>(true);
            for (int i = 0; i < sources.Length; i++)
            {
                var s = sources[i];
                if (s != null && s.isPlaying) s.Pause();
            }
        }
    }

    private void FreezeWorld()
    {
        // Freeze physics simulation
        var bodies = FindObjectsOfType<Rigidbody2D>(true);
        for (int i = 0; i < bodies.Length; i++)
        {
            var rb = bodies[i];
            if (rb != null) rb.simulated = false;
        }
        // Stop animators
        var anims = FindObjectsOfType<Animator>(true);
        for (int i = 0; i < anims.Length; i++)
        {
            var a = anims[i];
            if (a != null) a.speed = 0f;
        }
    }

    private void DisableScripts()
    {
        if (disableAllScriptsInScene)
        {
            var allBehaviours = FindObjectsOfType<MonoBehaviour>(true);
            for (int i = 0; i < allBehaviours.Length; i++)
            {
                var b = allBehaviours[i];
                if (b == null) continue;
                if (b == this) continue; // keep this component alive
                b.enabled = false;
            }
            if (debugLogs) Debug.Log("[GameOverProximity] Disabled ALL scripts in scene.");
            return;
        }

        if (disablePlayerScriptsOnFreeze)
        {
            GameObject playerGo = player != null ? player.gameObject : null;
            if (playerGo == null)
            {
                var found = GameObject.FindGameObjectWithTag(playerTag);
                playerGo = found;
            }
            if (playerGo != null)
            {
                DisableMonoBehavioursOn(playerGo);
                if (debugLogs) Debug.Log("[GameOverProximity] Disabled player scripts.");
            }
        }

        if (disableEnemyScriptsOnFreeze)
        {
            if (useEnemyTag)
            {
                var enemies = GameObject.FindGameObjectsWithTag(enemyTag);
                for (int i = 0; i < enemies.Length; i++)
                {
                    var e = enemies[i];
                    if (e != null) DisableMonoBehavioursOn(e);
                }
            }
            // Also disable by layer if configured
            var allBehaviours = FindObjectsOfType<MonoBehaviour>(true);
            for (int i = 0; i < allBehaviours.Length; i++)
            {
                var b = allBehaviours[i];
                if (b == null) continue;
                var go = b.gameObject;
                if (((1 << go.layer) & enemyLayers.value) != 0)
                {
                    b.enabled = false;
                }
            }
            if (debugLogs) Debug.Log("[GameOverProximity] Disabled enemy scripts.");
        }
    }

    private void DisableMonoBehavioursOn(GameObject go)
    {
        var behaviours = go.GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < behaviours.Length; i++)
        {
            var b = behaviours[i];
            if (b == null) continue;
            if (b == this) continue; // don't disable the detector itself
            b.enabled = false;
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Visualize the detection radius in the editor
        Gizmos.color = new Color(1f, 0f, 0f, 0.35f);
        var pos = player != null ? player.position : transform.position;
        Gizmos.DrawWireSphere(pos, triggerRadius);
    }
}
