using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns mask pickups at runtime.
/// </summary>
public class MaskSpawner : MonoBehaviour
{
    [Header("Spawn Config")]
    [SerializeField] private GameObject maskPrefab;
    [SerializeField] private int spawnCount = 3;
    [SerializeField] private int sacrificeCount = 1; // how many OrangeSacrifice masks to spawn
    [SerializeField] private float spawnRadius = 8f;
    [SerializeField] private float minSeparation = 2.5f;
    [SerializeField] private bool debugLogs = true;
    [SerializeField] private bool warnIfOffscreen = true;

    [Header("Spawn Triggering")]
    [Tooltip("If true, masks spawn when the Player enters this spawner's area, only once.")]
    [SerializeField] private bool spawnOnEnter = true;
    [SerializeField] private string playerTag = "Player";
    [Tooltip("Auto-add a BoxCollider2D trigger to detect Player enter if no collider is present.")]
    [SerializeField] private bool autoAddTrigger = true;
    private bool hasSpawned = false;

    [Header("Viewport Rules")]
    [Tooltip("Skip spawning if any masks are already visible in the current camera viewport (prevents duplicates across spawners).")]
    [SerializeField] private bool preventDuplicationWithinViewport = true;
    [Tooltip("Ensure spawned positions are inside the camera viewport when spawning on enter.")]
    [SerializeField] private bool enforceViewportBounds = true;

    [Header("Randomization Options")]
    [Tooltip("If true, use random positions inside radius; if false, use manual points.")]
    [SerializeField] private bool randomSpawning = true;
    [Tooltip("If true, choose a random ability type per spawn and ignore sacrificeCount.")]
    [SerializeField] private bool alwaysRandomAbility = true;
    [Tooltip("Manual spawn points used when randomSpawning is false. If empty, child transforms are used.")]
    [SerializeField] private Transform[] manualSpawnPoints;

    private readonly List<Vector2> spawnedPositions = new List<Vector2>();

    private void Start()
    {
        if (maskPrefab == null) { Debug.LogWarning("[MaskSpawner] maskPrefab not assigned"); return; }

        if (spawnOnEnter)
        {
            // Ensure we have a trigger collider to detect player entering this area
            var col = GetComponent<Collider2D>();
            if (col == null && autoAddTrigger)
            {
                var box = gameObject.AddComponent<BoxCollider2D>();
                box.isTrigger = true;
                // Size roughly covers the spawn area
                box.size = new Vector2(spawnRadius * 2f, spawnRadius * 2f);
            }
            else if (col != null)
            {
                col.isTrigger = true;
            }
            if (debugLogs) Debug.Log("[MaskSpawner] Waiting for player enter to spawn (once).");
        }
        else
        {
            SpawnMasks();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!spawnOnEnter) return;
        if (hasSpawned) return;
        if (!other.CompareTag(playerTag)) return;
        if (preventDuplicationWithinViewport && IsAnyMaskInViewport())
        {
            if (debugLogs) Debug.Log("[MaskSpawner] Skipping spawn: mask already present in viewport.");
            hasSpawned = true;
            return;
        }
        SpawnMasks();
        hasSpawned = true;
    }

    private void SpawnMasks()
    {
        if (preventDuplicationWithinViewport && IsAnyMaskInViewport())
        {
            if (debugLogs) Debug.Log("[MaskSpawner] Skipping spawn: mask already present in viewport.");
            return;
        }
        // Build spawn points and ability types based on options
        var points = new List<Vector2>();
        var typesToSpawn = new List<MaskType>();
        int total = Mathf.Max(0, spawnCount);
        if (randomSpawning)
        {
            typesToSpawn = BuildTypes(total);
            for (int i = 0; i < total; i++)
            {
                Vector2 pos;
                if (!TryFindPosition(out pos))
                {
                    // fallback: place at center offset
                    pos = (Vector2)transform.position + Random.insideUnitCircle * (spawnRadius * 0.25f);
                }
                // Enforce viewport bounds if enabled
                if (enforceViewportBounds && Camera.main != null && !IsOnScreen(pos))
                {
                    // Pull position toward camera center until on-screen
                    Vector3 camCenter = Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, Mathf.Max(0.01f, Camera.main.nearClipPlane+10f)));
                    Vector2 towardCenter = Vector2.Lerp(pos, (Vector2)camCenter, 0.6f);
                    if (IsOnScreen(towardCenter)) pos = towardCenter;
                    else pos = (Vector2)camCenter + Random.insideUnitCircle * (spawnRadius * 0.2f);
                }
                points.Add(pos);
            }
        }
        else
        {
            // Manual points: use provided array or child transforms
            var manual = new List<Transform>();
            if (manualSpawnPoints != null && manualSpawnPoints.Length > 0)
            {
                manual.AddRange(manualSpawnPoints);
            }
            else
            {
                foreach (Transform t in GetComponentsInChildren<Transform>())
                {
                    if (t == transform) continue;
                    manual.Add(t);
                }
            }
            if (manual.Count == 0)
            {
                if (debugLogs) Debug.LogWarning("[MaskSpawner] No manual spawn points found; nothing to spawn.");
                return;
            }
            total = manual.Count;
            typesToSpawn = BuildTypes(total);
            foreach (var t in manual)
            {
                points.Add(t.position);
            }
        }

        for (int i = 0; i < points.Count; i++)
        {
            Vector2 pos = points[i];
            var go = Instantiate(maskPrefab, pos, Quaternion.identity, transform);
            var mask = go.GetComponent<Mask>();
            if (mask != null)
            {
                mask.SetType(typesToSpawn[i]);
            }
            spawnedPositions.Add(pos);
            if (debugLogs) Debug.Log($"[MaskSpawner] Spawned {typesToSpawn[i]} at {pos}");

            if (warnIfOffscreen && Camera.main != null)
            {
                Vector3 vp = Camera.main.WorldToViewportPoint(pos);
                bool onScreen = vp.z > 0f && vp.x >= 0f && vp.x <= 1f && vp.y >= 0f && vp.y <= 1f;
                if (!onScreen && !enforceViewportBounds)
                {
                    Debug.LogWarning($"[MaskSpawner] Spawned mask is offscreen (viewport {vp}). Move the spawner closer or reduce spawnRadius.");
                }
            }
        }
    }

    private List<MaskType> BuildTypes(int total)
    {
        var list = new List<MaskType>(total);
        if (alwaysRandomAbility)
        {
            for (int i = 0; i < total; i++)
            {
                int r = Random.Range(0, 3);
                list.Add(r == 0 ? MaskType.GreenSpeed : (r == 1 ? MaskType.WhiteInvisibility : MaskType.OrangeSacrifice));
            }
            return list;
        }
        // Deterministic: sacrificeCount then alternate green/white
        int sac = Mathf.Clamp(sacrificeCount, 0, total);
        for (int i = 0; i < sac; i++) list.Add(MaskType.OrangeSacrifice);
        for (int i = sac; i < total; i++) list.Add(((i - sac) % 2 == 0) ? MaskType.GreenSpeed : MaskType.WhiteInvisibility);
        return list;
    }

    private bool TryFindPosition(out Vector2 pos)
    {
        const int maxAttempts = 50;
        for (int i = 0; i < maxAttempts; i++)
        {
            Vector2 candidate = (Vector2)transform.position + Random.insideUnitCircle * spawnRadius;
            // Enforce on-screen candidates when requested
            if (enforceViewportBounds && Camera.main != null && !IsOnScreen(candidate))
            {
                continue;
            }
            bool farEnough = true;
            foreach (var p in spawnedPositions)
            {
                if (Vector2.Distance(candidate, p) < minSeparation)
                {
                    farEnough = false;
                    break;
                }
            }
            if (farEnough)
            {
                pos = candidate;
                return true;
                
            }
        }
        pos = Vector2.zero;
        return false;
    }

    private bool IsOnScreen(Vector2 pos)
    {
        if (Camera.main == null) return true; // if no camera, consider on-screen
        Vector3 vp = Camera.main.WorldToViewportPoint(pos);
        return vp.z > 0f && vp.x >= 0f && vp.x <= 1f && vp.y >= 0f && vp.y <= 1f;
    }

    private bool IsAnyMaskInViewport()
    {
        if (Camera.main == null) return false;
        var masks = UnityEngine.Object.FindObjectsByType<Mask>(FindObjectsSortMode.None);
        foreach (var m in masks)
        {
            if (m == null) continue;
            if (IsOnScreen(m.transform.position)) return true;
        }
        return false;
    }
}
