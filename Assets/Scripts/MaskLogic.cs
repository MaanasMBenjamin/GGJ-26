using UnityEngine;

public class MaskLogic : MonoBehaviour
{
    [Header("Mask Stats")]
    [Tooltip("Multiplier to apply to the player when collected (e.g., 1.5 for 50% boost)")]
    [SerializeField] private float speedBoost = 1.5f;
    
    [Tooltip("Visual effect prefab to spawn on collection (optional)")]
    [SerializeField] private GameObject collectionVFX;
    private AltarLogic parentAltar;
    private bool _isCollected = false; // Safety switch for double-trigger bugs

    public void SetAltar(AltarLogic altar)
    {
        parentAltar = altar;
    }
    
    public void OnCollected(PlayerMoment player)
    {
        if (_isCollected) return;
        _isCollected = true;

        Debug.Log("Mask Collected!");

        // Apply the effect to the player
        player.ApplySpeedMultiplier(speedBoost);

        // Notify the altar to start cooldown and respawn a new mask
        if (parentAltar != null)
        {
            parentAltar.OnMaskCollected();
        }

        // Spawn effects and destroy mask
        if (collectionVFX != null)
            Instantiate(collectionVFX, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }
}