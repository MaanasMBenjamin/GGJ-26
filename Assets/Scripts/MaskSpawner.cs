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
    [SerializeField] private float spawnRadius = 8f;
    [SerializeField] private float minSeparation = 2.5f;
    [SerializeField] private bool debugLogs = true;
    [SerializeField] private bool warnIfOffscreen = true;

    private readonly List<Vector2> spawnedPositions = new List<Vector2>();

    private void Start()
    {
        if (maskPrefab == null) { Debug.LogWarning("[MaskSpawner] maskPrefab not assigned"); return; }

        // Ensure at least one of each if count >= 3
        var baseTypes = new List<MaskType> { MaskType.GreenSpeed, MaskType.WhiteInvisibility, MaskType.OrangeSacrifice };
        var typesToSpawn = new List<MaskType>();
        if (spawnCount >= 3)
        {
            typesToSpawn.AddRange(baseTypes);
            for (int i = 3; i < spawnCount; i++)
            {
                typesToSpawn.Add(baseTypes[Random.Range(0, baseTypes.Count)]);
            }
        }
        else
        {
            for (int i = 0; i < spawnCount; i++)
            {
                typesToSpawn.Add(baseTypes[i % baseTypes.Count]);
            }
        }

        for (int i = 0; i < typesToSpawn.Count; i++)
        {
            Vector2 pos;
            if (!TryFindPosition(out pos))
            {
                // fallback: place at center offset
                pos = (Vector2)transform.position + Random.insideUnitCircle * (spawnRadius * 0.25f);
            }
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
                if (!onScreen)
                {
                    Debug.LogWarning($"[MaskSpawner] Spawned mask is offscreen (viewport {vp}). Move the spawner closer or reduce spawnRadius.");
                }
            }
        }
    }

    private bool TryFindPosition(out Vector2 pos)
    {
        const int maxAttempts = 50;
        for (int i = 0; i < maxAttempts; i++)
        {
            Vector2 candidate = (Vector2)transform.position + Random.insideUnitCircle * spawnRadius;
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
}
