using UnityEngine;

// Enum for mask types
public enum MaskType
{
    Green,      // Speed boost
    Orange,     // Sacrifice - die if not picked, advantage during cooldown
    White       // Invisibility - enemies can't see player
}

public class Mask : MonoBehaviour
{
    [Header("Mask Settings")]
    [SerializeField] private MaskType maskType;
    [SerializeField] private float cooldownDuration = 30f;
    [SerializeField] private float abilityDuration = 30f;

    [Header("Visual Settings")]
    [SerializeField] private SpriteRenderer maskSprite;
    [SerializeField] private Color hiddenColor = Color.gray;  // Color when not picked (mystery)

    [Header("Speed Mask Settings")]
    [SerializeField] private float speedMultiplier = 2f;

    // Properties
    public MaskType Type => maskType;
    public float CooldownDuration => cooldownDuration;
    public float AbilityDuration => abilityDuration;
    public float SpeedMultiplier => speedMultiplier;

    private Color originalColor;
    private bool isRevealed = false;

    private void Awake()
    {
        if (maskSprite == null)
            maskSprite = GetComponent<SpriteRenderer>();

        // Store original color based on mask type
        originalColor = GetMaskColor();

        // Hide the true color until picked up
        HideMaskColor();
    }

    /// <summary>
    /// Get the color based on mask type
    /// </summary>
    public Color GetMaskColor()
    {
        switch (maskType)
        {
            case MaskType.Green:
                return Color.green;
            case MaskType.Orange:
                return new Color(1f, 0.5f, 0f); // Orange
            case MaskType.White:
                return Color.white;
            default:
                return Color.gray;
        }
    }

    /// <summary>
    /// Hide the mask's true color (mystery mask)
    /// </summary>
    public void HideMaskColor()
    {
        if (maskSprite != null)
        {
            maskSprite.color = hiddenColor;
        }
        isRevealed = false;
    }

    /// <summary>
    /// Reveal the mask's true color when picked up
    /// </summary>
    public void RevealMaskColor()
    {
        if (maskSprite != null)
        {
            maskSprite.color = originalColor;
        }
        isRevealed = true;
    }

    /// <summary>
    /// Check if mask color is revealed
    /// </summary>
    public bool IsRevealed()
    {
        return isRevealed;
    }

    /// <summary>
    /// Get ability description for UI
    /// </summary>
    public string GetAbilityDescription()
    {
        switch (maskType)
        {
            case MaskType.Green:
                return "SPEED BOOST! Move faster for " + abilityDuration + " seconds!";
            case MaskType.Orange:
                return "SACRIFICE! You have advantage but will die when cooldown ends!";
            case MaskType.White:
                return "INVISIBLE! Enemies can't see you for " + abilityDuration + " seconds!";
            default:
                return "Unknown mask...";
        }
    }

    /// <summary>
    /// Called when player picks up this mask
    /// </summary>
    public void OnPickup(PlayerMaskSystem player)
    {
        // Reveal the color
        RevealMaskColor();

        // Tell player to equip this mask
        player.EquipMask(this);

        // Disable the pickup (player now has it)
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Called when mask is dropped
    /// </summary>
    public void OnDrop(Vector3 dropPosition)
    {
        transform.position = dropPosition;
        gameObject.SetActive(true);
        // Keep color revealed since player already knows what it is
    }
}
