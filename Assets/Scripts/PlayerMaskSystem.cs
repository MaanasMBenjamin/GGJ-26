using UnityEngine;
using System;

public class PlayerMaskSystem : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerMoment playerMovement;  // Reference to movement script
    [SerializeField] private SpriteRenderer playerSprite;
    
    [Header("Pickup Settings")]
    [SerializeField] private float pickupRange = 1.5f;
    [SerializeField] private KeyCode pickupKey = KeyCode.E;
    [SerializeField] private LayerMask maskLayer;

    [Header("Current Mask Info (Read Only)")]
    [SerializeField] private Mask currentMask;
    [SerializeField] private bool hasActiveMask = false;

    // Ability state
    private bool isAbilityActive = false;
    private float abilityTimer = 0f;
    private float cooldownTimer = 0f;
    private bool isOnCooldown = false;

    // Original values (to restore after ability ends)
    private float originalMoveSpeed;
    private Color originalPlayerColor;

    // Events for UI or other systems
    public event Action<MaskType, float> OnAbilityActivated;   // mask type, duration
    public event Action<MaskType> OnAbilityEnded;
    public event Action<float> OnCooldownUpdated;              // remaining time
    public event Action OnPlayerDeath;
    public event Action<Mask> OnMaskPickedUp;

    // Public properties
    public bool HasMask => currentMask != null;
    public bool IsAbilityActive => isAbilityActive;
    public bool IsOnCooldown => isOnCooldown;
    public float CooldownRemaining => cooldownTimer;
    public Mask CurrentMask => currentMask;

    private void Awake()
    {
        // Get references if not set
        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMoment>();

        if (playerSprite == null)
            playerSprite = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        // Store original values
        if (playerSprite != null)
            originalPlayerColor = playerSprite.color;
    }

    private void Update()
    {
        // Check for mask pickup
        CheckForMaskPickup();

        // Handle ability timer
        if (isAbilityActive)
        {
            abilityTimer -= Time.deltaTime;
            if (abilityTimer <= 0)
            {
                EndAbility();
            }
        }

        // Handle cooldown timer
        if (isOnCooldown)
        {
            cooldownTimer -= Time.deltaTime;
            OnCooldownUpdated?.Invoke(cooldownTimer);

            if (cooldownTimer <= 0)
            {
                CooldownEnded();
            }
        }
    }

    /// <summary>
    /// Check if player can pick up a nearby mask
    /// </summary>
    private void CheckForMaskPickup()
    {
        if (Input.GetKeyDown(pickupKey))
        {
            // Find masks in range
            Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, pickupRange, maskLayer);

            foreach (Collider2D col in colliders)
            {
                Mask mask = col.GetComponent<Mask>();
                if (mask != null)
                {
                    // Drop current mask if we have one
                    if (currentMask != null)
                    {
                        DropCurrentMask();
                    }

                    // Pick up the new mask
                    mask.OnPickup(this);
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Called by Mask when player picks it up
    /// </summary>
    public void EquipMask(Mask mask)
    {
        currentMask = mask;
        hasActiveMask = true;

        Debug.Log("Picked up " + mask.Type + " mask! " + mask.GetAbilityDescription());

        // Trigger event
        OnMaskPickedUp?.Invoke(mask);

        // Automatically activate ability
        ActivateAbility();
    }

    /// <summary>
    /// Drop the current mask
    /// </summary>
    public void DropCurrentMask()
    {
        if (currentMask != null)
        {
            // If ability is active, end it first
            if (isAbilityActive)
            {
                EndAbility();
            }

            // Drop mask near player
            Vector3 dropPos = transform.position + new Vector3(1f, 0f, 0f);
            currentMask.OnDrop(dropPos);
            currentMask = null;
            hasActiveMask = false;
        }
    }

    /// <summary>
    /// Activate the current mask's ability
    /// </summary>
    public void ActivateAbility()
    {
        if (currentMask == null || isAbilityActive || isOnCooldown)
            return;

        isAbilityActive = true;
        abilityTimer = currentMask.AbilityDuration;

        switch (currentMask.Type)
        {
            case MaskType.Green:
                ActivateSpeedBoost();
                break;
            case MaskType.Orange:
                ActivateSacrifice();
                break;
            case MaskType.White:
                ActivateInvisibility();
                break;
        }

        OnAbilityActivated?.Invoke(currentMask.Type, currentMask.AbilityDuration);
        Debug.Log(currentMask.Type + " ability activated for " + currentMask.AbilityDuration + " seconds!");
    }

    /// <summary>
    /// End the current ability and start cooldown
    /// </summary>
    private void EndAbility()
    {
        if (currentMask == null)
            return;

        MaskType endedType = currentMask.Type;

        switch (currentMask.Type)
        {
            case MaskType.Green:
                DeactivateSpeedBoost();
                break;
            case MaskType.Orange:
                // Sacrifice keeps going until cooldown ends
                break;
            case MaskType.White:
                DeactivateInvisibility();
                break;
        }

        isAbilityActive = false;
        OnAbilityEnded?.Invoke(endedType);

        // Start cooldown
        StartCooldown();
    }

    /// <summary>
    /// Start the cooldown period
    /// </summary>
    private void StartCooldown()
    {
        if (currentMask == null)
            return;

        isOnCooldown = true;
        cooldownTimer = currentMask.CooldownDuration;
        Debug.Log("Cooldown started: " + cooldownTimer + " seconds");
    }

    /// <summary>
    /// Called when cooldown ends
    /// </summary>
    private void CooldownEnded()
    {
        isOnCooldown = false;

        // Special case: Orange mask kills player when cooldown ends
        if (currentMask != null && currentMask.Type == MaskType.Orange)
        {
            Debug.Log("SACRIFICE COMPLETE! Player dies...");
            KillPlayer();
        }
        else
        {
            Debug.Log("Cooldown ended! Mask ability ready again.");
        }
    }

    // ================== ABILITY IMPLEMENTATIONS ==================

    /// <summary>
    /// GREEN MASK: Speed boost
    /// </summary>
    private void ActivateSpeedBoost()
    {
        Debug.Log("SPEED BOOST ACTIVATED! Moving faster!");
        
        // Change player color to show speed boost
        if (playerSprite != null)
            playerSprite.color = Color.green;

        // Apply speed boost
        if (playerMovement != null)
            playerMovement.ApplySpeedMultiplier(currentMask.SpeedMultiplier);
    }

    private void DeactivateSpeedBoost()
    {
        Debug.Log("Speed boost ended!");
        
        if (playerSprite != null)
            playerSprite.color = originalPlayerColor;

        if (playerMovement != null)
            playerMovement.ApplySpeedMultiplier(1f);
    }

    /// <summary>
    /// ORANGE MASK: Sacrifice (advantage during cooldown, then death)
    /// </summary>
    private void ActivateSacrifice()
    {
        Debug.Log("SACRIFICE ACTIVATED! You have power but will die when cooldown ends!");
        
        // Visual feedback
        if (playerSprite != null)
            playerSprite.color = new Color(1f, 0.5f, 0f); // Orange

        // Give player advantages during sacrifice
        if (playerMovement != null)
            playerMovement.ApplySpeedMultiplier(1.5f);
    }

    private void DeactivateSacrifice()
    {
        if (playerSprite != null)
            playerSprite.color = originalPlayerColor;

        if (playerMovement != null)
            playerMovement.ApplySpeedMultiplier(1f);
    }

    /// <summary>
    /// WHITE MASK: Invisibility (enemies can't see player)
    /// </summary>
    private void ActivateInvisibility()
    {
        Debug.Log("INVISIBILITY ACTIVATED! Enemies can't see you!");
        
        // Make player semi-transparent
        if (playerSprite != null)
        {
            Color c = playerSprite.color;
            c.a = 0.3f; // 30% visible
            playerSprite.color = c;
        }

        // Set player tag so enemies ignore them
        gameObject.tag = "Invisible";
    }

    private void DeactivateInvisibility()
    {
        Debug.Log("Invisibility ended! Enemies can see you again!");
        
        if (playerSprite != null)
        {
            playerSprite.color = originalPlayerColor;
        }

        gameObject.tag = "Player";
    }

    /// <summary>
    /// Kill the player (used by Sacrifice mask and SacrificeZone)
    /// </summary>
    public void KillPlayer()
    {
        Debug.Log("PLAYER DIED!");
        OnPlayerDeath?.Invoke();
        
        // Deactivate any active abilities
        if (currentMask != null && currentMask.Type == MaskType.Orange)
        {
            DeactivateSacrifice();
        }

        // Disable the player (implement your own death logic)
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Check if player is invisible (for enemy AI)
    /// </summary>
    public bool IsInvisible()
    {
        return isAbilityActive && currentMask != null && currentMask.Type == MaskType.White;
    }

    /// <summary>
    /// Check if player has sacrifice advantage (for enemy AI)
    /// </summary>
    public bool HasSacrificeAdvantage()
    {
        return currentMask != null && currentMask.Type == MaskType.Orange && (isAbilityActive || isOnCooldown);
    }

    // Gizmo to show pickup range in editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRange);
    }
}
