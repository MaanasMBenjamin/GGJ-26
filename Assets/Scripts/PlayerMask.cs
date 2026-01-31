using System;
using UnityEngine;

/// <summary>
/// Attach to Player. Handles equipping masks and applying effects.
/// </summary>
public class PlayerMask : MonoBehaviour
{
    [Header("Player Mask Settings")]
    [SerializeField] private float speedMultiplierOnGreen = 1.6f;
    [SerializeField] private bool killOnSacrificeTimeout = true;
    [Tooltip("Minimum duration any equipped mask should last.")]
    [SerializeField] private float minAbilityDurationSeconds = 30f;
    [SerializeField] private bool debugLogs = true;

    [Header("Mask Blink Control (Optional)")]
    [Tooltip("If true, override all masks' blink flash duration from here.")]
    [SerializeField] private bool overrideMaskBlinkDuration = false;
    [SerializeField] private float maskBlinkDurationSeconds = 2f;

    private PlayerMoment playerMoment;
    private float maskTimer;
    private MaskType currentMask = (MaskType)(-1);

    public static bool IsInvisible { get; private set; }
    public static bool IsSacrificeEquipped { get; private set; }

    public event Action OnPlayerDied;

    private void Awake()
    {
        playerMoment = GetComponent<PlayerMoment>();
    }

    private void Start()
    {
        if (overrideMaskBlinkDuration)
        {
            var masks = UnityEngine.Object.FindObjectsByType<Mask>(FindObjectsSortMode.None);
            foreach (var m in masks)
            {
                if (m != null) m.SetBlinkDuration(maskBlinkDurationSeconds);
            }
            if (debugLogs) Debug.Log($"[PlayerMask] Overrode blink duration for {masks.Length} masks to {maskBlinkDurationSeconds:F2}s");
        }
    }

    private int lastLoggedSecond = -1;

    private void Update()
    {
        if (currentMask < 0) return;
        if (maskTimer > 0f)
        {
            maskTimer -= Time.deltaTime;
            if (maskTimer < 0f) maskTimer = 0f;
        }

        // Debug countdown once per whole second
        if (debugLogs)
        {
            int whole = Mathf.FloorToInt(maskTimer);
            if (whole != lastLoggedSecond)
            {
                lastLoggedSecond = whole;
                Debug.Log($"[PlayerMask] Mask {currentMask} countdown: {whole}s remaining");
            }
        }

        // Handle per-mask runtime behavior
        switch (currentMask)
        {
            case MaskType.GreenSpeed:
                // nothing needed per-frame beyond timer ticking
                if (maskTimer <= 0f)
                {
                    ApplySpeed(1f);
                    if (debugLogs) Debug.Log("[PlayerMask] GreenSpeed expired: speed reset");
                }
                break;
            case MaskType.WhiteInvisibility:
                if (maskTimer <= 0f)
                {
                    SetInvisible(false);
                    if (debugLogs) Debug.Log("[PlayerMask] WhiteInvisibility expired: invisibility off");
                }
                break;
            case MaskType.OrangeSacrifice:
                if (maskTimer <= 0f)
                {
                    if (killOnSacrificeTimeout)
                    {
                        if (debugLogs) Debug.Log("[PlayerMask] OrangeSacrifice timeout reached: player will die");
                        KillPlayer();
                    }
                }
                break;
        }
    }

    public void EquipMask(Mask mask)
    {
        if (mask == null) return;
        float cd = Mathf.Max(0.01f, mask.GetCooldownSeconds());
        float cdEffective = Mathf.Max(cd, Mathf.Max(0.01f, minAbilityDurationSeconds));
        var type = mask.GetMaskType();

        // Reset old mask effects
        ResetEffects();

        currentMask = type;
        maskTimer = cdEffective;

        if (debugLogs) Debug.Log($"[PlayerMask] Picked mask: {type} cooldown {cd:F1}s → effective {cdEffective:F1}s (min {minAbilityDurationSeconds:F1}s)");

        if (type == MaskType.GreenSpeed)
        {
            ApplySpeed(speedMultiplierOnGreen);
            SetInvisible(false);
            IsSacrificeEquipped = false;
            if (debugLogs) Debug.Log($"[PlayerMask] Ability applied: Speed x{speedMultiplierOnGreen:F2}");
        }
        else if (type == MaskType.WhiteInvisibility)
        {
            ApplySpeed(1f);
            SetInvisible(true);
            IsSacrificeEquipped = false;
            if (debugLogs) Debug.Log("[PlayerMask] Ability applied: Invisibility ON");
        }
        else if (type == MaskType.OrangeSacrifice)
        {
            ApplySpeed(1f);
            SetInvisible(false);
            IsSacrificeEquipped = true;
            if (debugLogs) Debug.Log("[PlayerMask] Ability applied: Sacrifice (colors revealed, death on timeout)");
        }

        // Ensure map masks immediately reflect new state (sacrifice → reveal, others → hide)
        Mask.RefreshRevealGate();
    }

    private void ApplySpeed(float multiplier)
    {
        if (playerMoment != null)
        {
            playerMoment.ApplySpeedMultiplier(multiplier);
        }
    }

    private void SetInvisible(bool value)
    {
        IsInvisible = value;
        // Optionally adjust player visuals here (e.g., alpha) if desired
    }

    private void ResetEffects()
    {
        // Reset common state when switching masks
        ApplySpeed(1f);
        SetInvisible(false);
        IsSacrificeEquipped = false;
    }

    private void KillPlayer()
    {
        if (debugLogs) Debug.Log("[PlayerMask] Player died due to sacrifice timeout");
        OnPlayerDied?.Invoke();
        Destroy(gameObject);
    }
}
