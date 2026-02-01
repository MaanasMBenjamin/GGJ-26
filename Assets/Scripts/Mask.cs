using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public enum MaskType
{
    GreenSpeed,
    WhiteInvisibility,
    OrangeSacrifice
}
/// <summary>
/// Attach to a mask pickup. Handles color reveal, blink coordination, and pickup behavior.
/// </summary>
public class Mask : MonoBehaviour
{
    [Header("Mask Setup")]
    [SerializeField] private MaskType type = MaskType.GreenSpeed;
    [SerializeField] private float abilityCooldownSeconds = 15f; // editable per mask
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private bool debugLogs = true;

    [Header("Visuals")]
    [SerializeField] private float blinkDuration = 2f;

    [Header("Visuals (Sprite-based)")]
    [SerializeField] private Sprite blinkSprite;
    [SerializeField] private Sprite greenSprite;
    [SerializeField] private Sprite whiteSprite;
    [SerializeField] private Sprite orangeSprite;
    private Sprite originalSprite;
    [SerializeField] private bool blinkByHidingIfNoSprite = true;

    [Header("Visibility")]
    [SerializeField] private bool useUnlitMaterial = true;
    [SerializeField] private bool overrideSortingOrder = true;
    [SerializeField] private int sortingOrder = 5;

    [Header("Blink Orchestration")]
    [SerializeField] private float blinkIntervalSeconds = 3f;
    [SerializeField] private bool blinkEnabled = true; // per-mask opt-out
    [SerializeField] private bool hideMasksOutsideBlink = true; // hide all masks except the one currently blinking
    [SerializeField] private bool gatePickupByVisibility = true; // when hidden, player cannot pick

    private static readonly List<Mask> masks = new List<Mask>();
    private static bool coordinatorRunning;
    private static bool blinkEnabledGlobal = true; // global control
    private bool picked;
    private List<Collider2D> triggerColliders;

    [Header("Glow Light (URP 2D)")]
    [SerializeField] private Light2D glowLight;
    [SerializeField] private bool autoCreateGlowLight = false; // disabled by default
    [SerializeField] private bool respectGlobalLightingState = true;
    [Tooltip("True: glow matches sprite silhouette (Sprite Light). False: circular halo (Point Light).")]
    [SerializeField] private bool useSpriteShapedGlow = true;
    [Tooltip("Hide the base SpriteRenderer when sprite-shaped glow is used to avoid a double-layer look.")]
    [SerializeField] private bool hideSpriteWhenGlowEnabled = true;
    [Tooltip("Show a crisp silhouette using the SpriteRenderer instead of Light2D glow.")]
    [SerializeField] private bool useCrispSilhouetteInsteadOfGlow = false;
    [Tooltip("Disable Light2D glow entirely for masks.")]
    [SerializeField] private bool disableGlowCompletely = true;
    [Tooltip("2D Renderer blend style index for the glow (0=Additive by default).")]
    [SerializeField] private int glowBlendStyleIndex = 0;
    [SerializeField] private float glowRadius = 2.5f; // used for Point light
    [SerializeField, Range(0f,1f)] private float glowInnerRadiusRatio = 0.5f; // used for Point light
    [SerializeField, Range(0f,1f)] private float glowIntensity = 0.8f;

    private int spawnIndex;

    private void Awake()
    {
        if (spriteRenderer == null)
        {
            // Prefer SpriteRenderer on the same GameObject; fallback to children
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }
        }
        if (spriteRenderer == null)
        {
            Debug.LogWarning("[Mask] SpriteRenderer missing on mask prefab; assign a SpriteRenderer to render the mask.");
        }
        else
        {
            // Ensure renderer is enabled
            spriteRenderer.enabled = true;
            // Force pixel-art crisp sampling to avoid blurry glow cookie
            if (spriteRenderer.sprite != null && spriteRenderer.sprite.texture != null)
            {
                var tex = spriteRenderer.sprite.texture;
                tex.filterMode = FilterMode.Point;
                tex.anisoLevel = 0;
            }
            // No runtime tinting; we swap sprites instead.

            if (useUnlitMaterial)
            {
                // Try URP 2D Sprite-Unlit; fallback to legacy Sprites/Default
                Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit");
                if (shader == null) shader = Shader.Find("Sprites/Default");
                if (shader != null)
                {
                    spriteRenderer.material = new Material(shader);
                }
            }
            if (overrideSortingOrder)
            {
                spriteRenderer.sortingOrder = sortingOrder;
            }
        }
        // Capture original sprite for fallback and prefer the generic blink sprite as the default look
        originalSprite = spriteRenderer != null ? spriteRenderer.sprite : null;
        // If the prefab's sprite is already set to an ability sprite, swap to blinkSprite as the neutral default
        if (blinkSprite != null)
        {
            if (originalSprite == null || originalSprite == greenSprite || originalSprite == whiteSprite || originalSprite == orangeSprite)
            {
                originalSprite = blinkSprite;
                spriteRenderer.sprite = originalSprite;
            }
        }
        if (originalSprite == null)
        {
            // Fallback order prefers blink (neutral), then any ability sprite
            originalSprite = blinkSprite ?? greenSprite ?? whiteSprite ?? orangeSprite;
            if (originalSprite != null)
            {
                spriteRenderer.sprite = originalSprite;
            }
            else
            {
                Debug.LogWarning("[Mask] No sprite assigned; mask may be invisible.");
            }
        }
        ApplyHiddenSprite();
        // Start hidden only when blink is enabled globally and colors are hidden
        if (hideMasksOutsideBlink && blinkEnabledGlobal && !PlayerMask.IsSacrificeEquipped && spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
            SetPickupEnabled(false);
        }

        spawnIndex = masks.Count;
        masks.Add(this);

        if (debugLogs) Debug.Log($"[Mask] Registered #{spawnIndex} type={type} cooldown={abilityCooldownSeconds:F1}s");

        if (!coordinatorRunning)
        {
            coordinatorRunning = true;
            StartCoroutine(BlinkCoordinatorLoop());
        }

        // Setup glow light
        if (!disableGlowCompletely && glowLight == null && autoCreateGlowLight)
        {
            var go = new GameObject("Mask Glow 2D");
            go.transform.SetParent(transform);
            go.transform.localPosition = Vector3.zero;
            glowLight = go.AddComponent<Light2D>();
            glowLight.lightType = useSpriteShapedGlow ? Light2D.LightType.Sprite : Light2D.LightType.Point;
        }
        if (glowLight != null)
        {
            if (!useCrispSilhouetteInsteadOfGlow && !disableGlowCompletely)
            {
                ApplyGlowSettings();
                UpdateGlowColor();
                ApplyGlowGate();
                ApplySpriteVisibilityForGlow();
            }
            else
            {
                // Crisp silhouette mode: disable glow and show sprite only
                glowLight.enabled = false;
            }
        }
        if (useCrispSilhouetteInsteadOfGlow && spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
            SetPickupEnabled(true);
            if (PlayerMask.IsSacrificeEquipped) ApplyAbilitySprite(); else ApplyHiddenSprite();
        }
    }

    private void OnDestroy()
    {
        masks.Remove(this);
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        var pm = other.GetComponent<PlayerMask>();
        if (pm == null) return;
        if (picked) return; // already picked, ignore further triggers
        // Only allow pickup when the mask is currently visible (if gated)
        if (gatePickupByVisibility && (spriteRenderer == null || !spriteRenderer.enabled))
        {
            if (debugLogs) Debug.Log("[Mask] Pickup ignored: mask not visible");
            return;
        }
        if (debugLogs) Debug.Log($"[Mask] {type} picked by player");
        picked = true;
        SetPickupEnabled(false);
        pm.EquipMask(this);
        Destroy(gameObject);
    }

    public MaskType GetMaskType() => type;
    public float GetCooldownSeconds() => abilityCooldownSeconds;

    // UI helper: return the sprite that represents this mask's ability color/icon
    public Sprite GetAbilitySpriteForHud()
    {
        switch (type)
        {
            case MaskType.GreenSpeed: return greenSprite;
            case MaskType.WhiteInvisibility: return whiteSprite;
            case MaskType.OrangeSacrifice: return orangeSprite;
            default: return null;
        }
    }

    // UI helper: current sprite shown on this mask's renderer (exact pickup look)
    public Sprite GetCurrentSprite()
    {
        return spriteRenderer != null ? spriteRenderer.sprite : null;
    }

    public static void RefreshRevealGate()
    {
        bool reveal = PlayerMask.IsSacrificeEquipped;
        if (reveal)
        {
            Debug.Log("[Mask] Reveal gate ON: showing ability sprites while sacrifice equipped");
        }
        else
        {
            Debug.Log("[Mask] Reveal gate OFF: showing original sprites");
        }
        foreach (var m in masks)
        {
            if (m == null) continue;
            if (reveal)
            {
                m.ApplyAbilitySprite();
                if (m.spriteRenderer != null) m.spriteRenderer.enabled = true; // ensure visible while revealed
                m.SetPickupEnabled(true);
            }
            else
            {
                m.ApplyHiddenSprite();
                if (m.hideMasksOutsideBlink && blinkEnabledGlobal) m.SetPickupEnabled(false);
            }
        }
    }

    public void SetType(MaskType newType)
    {
        type = newType;
        if (spriteRenderer != null)
        {
            if (PlayerMask.IsSacrificeEquipped) ApplyAbilitySprite(); else ApplyHiddenSprite();
        }
    }

    private void ApplyHiddenSprite()
    {
        if (spriteRenderer == null) return;
        if (!PlayerMask.IsSacrificeEquipped)
        {
            SetSpriteSafe(originalSprite);
        }
        else
        {
            ApplyAbilitySprite();
        }
    }

    private void ApplyAbilitySprite()
    {
        if (spriteRenderer == null) return;
        Sprite target = originalSprite;
        switch (type)
        {
            case MaskType.GreenSpeed: target = greenSprite != null ? greenSprite : originalSprite; break;
            case MaskType.WhiteInvisibility: target = whiteSprite != null ? whiteSprite : originalSprite; break;
            case MaskType.OrangeSacrifice: target = orangeSprite != null ? orangeSprite : originalSprite; break;
        }
        SetSpriteSafe(target);
    }

    private IEnumerator BlinkCoordinatorLoop()
    {
        while (true)
        {
            // Wait for interval
            yield return new WaitForSeconds(Mathf.Max(0.01f, blinkIntervalSeconds));

            // If sacrifice equipped, skip blinking (masks should be revealed)
            if (PlayerMask.IsSacrificeEquipped) continue;

            // Global blink disabled? skip
            if (!blinkEnabledGlobal) continue;

            if (debugLogs) Debug.Log("[Mask] Blink cycle: only the active mask is visible");

            // Hide all masks first when in hidden mode
            if (hideMasksOutsideBlink)
            {
                for (int i = 0; i < masks.Count; i++)
                {
                    var mHide = masks[i];
                    if (mHide != null && mHide.spriteRenderer != null)
                    {
                        mHide.spriteRenderer.enabled = false;
                        mHide.SetPickupEnabled(false);
                    }
                }
            }

            // Show each mask for its blink duration then hide again
            for (int i = 0; i < masks.Count; i++)
            {
                var m = masks[i];
                if (m == null || !m.blinkEnabled || m.spriteRenderer == null) { yield return new WaitForSeconds(0.05f); continue; }

                // Choose a safe sprite to show: blink → original → ability fallback
                Sprite toShow = m.blinkSprite != null ? m.blinkSprite : (m.originalSprite != null ? m.originalSprite : m.GetAbilitySpriteForHud());
                m.SetSpriteSafe(toShow);
                m.spriteRenderer.enabled = true;
                m.SetPickupEnabled(true);

                float dur = Mathf.Max(0.01f, m.blinkDuration);
                yield return new WaitForSeconds(dur);

                // restore hidden after blink when not revealed
                if (!PlayerMask.IsSacrificeEquipped)
                {
                    m.SetSpriteSafe(m.originalSprite);
                }
                if (hideMasksOutsideBlink)
                {
                    m.spriteRenderer.enabled = false;
                    m.SetPickupEnabled(false);
                }

                yield return new WaitForSeconds(0.1f);
            }
        }
    }

    private IEnumerator BlinkOnce()
    {
        if (spriteRenderer == null) yield break;
        // When colors are hidden, use blinkColor for a quick flash, then revert
        Sprite original = spriteRenderer.sprite;
        if (blinkSprite != null) SetSpriteSafe(blinkSprite);
        else if (blinkByHidingIfNoSprite) spriteRenderer.enabled = false;
        yield return new WaitForSeconds(Mathf.Max(0.01f, blinkDuration));
        if (!PlayerMask.IsSacrificeEquipped)
        {
            SetSpriteSafe(original);
        }
        else
        {
            ApplyAbilitySprite();
        }
        if (!spriteRenderer.enabled) spriteRenderer.enabled = true;
        if (debugLogs) Debug.Log($"[Mask] BlinkOnce on #{spawnIndex}");
    }

    private void OnDrawGizmos()
    {
        // Draw a small marker to help locate masks in the scene view
        Gizmos.color = new Color(1f, 1f, 0f, 0.9f);
        Gizmos.DrawSphere(transform.position, 0.08f);
    }

    /// <summary>
    /// Set how long a single blink flash lasts (seconds).
    /// </summary>
    public void SetBlinkDuration(float seconds)
    {
        blinkDuration = Mathf.Max(0.01f, seconds);
        if (debugLogs) Debug.Log($"[Mask] BlinkDuration set to {blinkDuration:F2}s");
    }

    /// <summary>
    /// Enable or disable blinking globally for all masks.
    /// </summary>
    public static void SetBlinkEnabledGlobal(bool enabled)
    {
        blinkEnabledGlobal = enabled;
        if (enabled)
        {
            Debug.Log("[Mask] Global blink ENABLED");
            // When enabling blink, respect hidden mode: hide masks if sacrifice is not equipped
            foreach (var m in masks)
            {
                if (m == null || m.spriteRenderer == null) continue;
                if (!PlayerMask.IsSacrificeEquipped && m.hideMasksOutsideBlink)
                {
                    m.spriteRenderer.enabled = false;
                    m.SetPickupEnabled(false);
                }
            }
        }
        else
        {
            Debug.Log("[Mask] Global blink DISABLED (masks stay visible)");
            // When disabling blink, show masks in their current gate state (hidden or revealed)
            foreach (var m in masks)
            {
                if (m == null || m.spriteRenderer == null) continue;
                m.ApplyHiddenSprite();
                m.spriteRenderer.enabled = true;
                m.SetPickupEnabled(true);
            }
        }
    }

    private void SetSpriteSafe(Sprite s)
    {
        if (spriteRenderer == null) return;
        spriteRenderer.sprite = s;
        if (s != null && s.texture != null)
        {
            var tex = s.texture;
            tex.filterMode = FilterMode.Point;
            tex.anisoLevel = 0;
        }
    }

    private void ApplyGlowSettings()
    {
        if (glowLight == null) return;
        glowLight.lightType = useSpriteShapedGlow ? Light2D.LightType.Sprite : Light2D.LightType.Point;
        glowLight.blendStyleIndex = Mathf.Clamp(glowBlendStyleIndex, 0, 3);
        if (useSpriteShapedGlow)
        {
            // Shape the light to the sprite so glow hugs the silhouette
            if (spriteRenderer != null && spriteRenderer.sprite != null)
            {
                glowLight.lightCookieSprite = spriteRenderer.sprite;
            }
            // Reduce blur for a crisp silhouette glow
            glowLight.falloffIntensity = 0f;
        }
        else
        {
            glowLight.pointLightOuterRadius = Mathf.Max(0.01f, glowRadius);
            glowLight.pointLightInnerRadius = Mathf.Clamp01(glowInnerRadiusRatio) * glowLight.pointLightOuterRadius;
            glowLight.falloffIntensity = 1f;
        }
        glowLight.intensity = Mathf.Clamp01(glowIntensity);
    }

    private void ApplySpriteVisibilityForGlow()
    {
        if (spriteRenderer == null) return;
        bool hide = useSpriteShapedGlow && hideSpriteWhenGlowEnabled;
        spriteRenderer.enabled = !hide;
    }

    private void UpdateGlowColor()
    {
        if (glowLight == null || disableGlowCompletely) return;
        if (!glowLight.enabled) return; // in crisp silhouette mode, glow is disabled
        // Glow color de-emphasized; default to white to avoid inspector clutter
        glowLight.color = Color.white;
    }

    private void ApplyGlowGate()
    {
        if (glowLight == null || disableGlowCompletely) return;
        if (!glowLight.enabled) return;
        if (respectGlobalLightingState && !LightingState.LocalLightsEnabled)
        {
            glowLight.intensity = 0f;
        }
        else
        {
            glowLight.intensity = Mathf.Clamp01(glowIntensity);
        }
    }

    private void Update()
    {
        // keep glow gated correctly when glow is in use
        if (!useCrispSilhouetteInsteadOfGlow && !disableGlowCompletely)
        {
            ApplyGlowGate();
        }
    }

    private void SetPickupEnabled(bool enabled)
    {
        if (triggerColliders == null)
        {
            triggerColliders = new List<Collider2D>();
            var cols = GetComponentsInChildren<Collider2D>(true);
            foreach (var c in cols)
            {
                triggerColliders.Add(c);
            }
        }
        foreach (var c in triggerColliders)
        {
            if (c != null) c.enabled = enabled;
        }
    }
}
