using UnityEngine;

public class AltarLogic : MonoBehaviour
{
    [Header("Mask Spawning")]
    [Tooltip("Array of mask prefabs to spawn randomly (up to 3)")]
    [SerializeField] private GameObject[] maskPrefabs;
    
    [Tooltip("Where the mask spawns relative to the altar")]
    [SerializeField] private Transform spawnPoint;
    
    [Header("Cooldown Settings")]
    [Tooltip("Time in seconds before a new mask spawns")]
    [SerializeField] private float cooldownDuration = 5f;
    
    [Header("Visual Feedback (Optional)")]
    [SerializeField] private SpriteRenderer altarRenderer;
    [SerializeField] private Color activeColor = Color.white;
    [SerializeField] private Color cooldownColor = Color.gray;
    
    private GameObject currentMask;
    private bool isOnCooldown = false;
    private float cooldownTimer;

    private void Start()
    {
        // Use altar position if no spawn point assigned
        if (spawnPoint == null)
            spawnPoint = transform;
        
        // Get renderer for visual feedback
        if (altarRenderer == null)
            altarRenderer = GetComponent<SpriteRenderer>();
        
        // Spawn initial mask
        SpawnRandomMask();
    }

    private void Update()
    {
        if (isOnCooldown)
        {
            cooldownTimer -= Time.deltaTime;
            // Debug.Log($"<colour=green>{cooldownDuration}>");
            if (cooldownTimer <= 0f)
            {
                isOnCooldown = false;
                SpawnRandomMask();
                UpdateVisuals();
            }
        }
    }

    /// <summary>
    /// Called when the mask on this altar is collected
    /// </summary>
    public void OnMaskCollected()
    {
        currentMask = null;
        StartCooldown();
    }

    private void StartCooldown()
    {
        isOnCooldown = true;
        cooldownTimer = cooldownDuration;
        UpdateVisuals();
        Debug.Log($"<color=yellow>Altar cooldown started: {cooldownDuration}s</color>");
    }

    private void SpawnRandomMask()
    {
        if (maskPrefabs == null || maskPrefabs.Length == 0)
        {
            Debug.LogError("AltarLogic: No mask prefabs assigned!");
            return;
        }

        // Pick a random mask from the array
        int randomIndex = Random.Range(0, maskPrefabs.Length);
        GameObject selectedPrefab = maskPrefabs[randomIndex];

        if (selectedPrefab == null)
        {
            Debug.LogError($"AltarLogic: Mask prefab at index {randomIndex} is null!");
            return;
        }

        // Spawn the mask
        currentMask = Instantiate(selectedPrefab, spawnPoint.position, Quaternion.identity);
        
        // Link the mask to this altar
        if (currentMask.TryGetComponent<MaskLogic>(out MaskLogic mask))
        {
            mask.SetAltar(this);
        }

        Debug.Log($"<color=green>Altar spawned: {selectedPrefab.name}</color>");
    }

    private void UpdateVisuals()
    {
        if (altarRenderer != null)
        {
            altarRenderer.color = isOnCooldown ? cooldownColor : activeColor;
        }
    }

    /// <summary>
    /// Get remaining cooldown time (for UI purposes)
    /// </summary>
    public float GetRemainingCooldown()
    {
        return isOnCooldown ? cooldownTimer : 0f;
    }

    /// <summary>
    /// Check if altar is on cooldown
    /// </summary>
    public bool IsOnCooldown()
    {
        return isOnCooldown;
    }
}