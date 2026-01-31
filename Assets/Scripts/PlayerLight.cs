using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Attach to the Player. Controls a 2D point light that moves with the player
/// and exposes serialized controls for radius and intensity.
/// Requires URP 2D with a Light2D component.
/// </summary>
public class PlayerLight : MonoBehaviour
{
    [Header("Player Light (URP 2D)")]
    [SerializeField] private Light2D playerLight; // assign a Light2D (Point) on the player or child
    [SerializeField] private bool autoFindChildLight = true; // find first Light2D in children
    [SerializeField] private bool autoCreateLightIfMissing = true; // create a child Point Light if none is found
    [SerializeField] private bool respectGlobalLightingState = true; // disable during scene flicker

    [Tooltip("Outer radius of the player light in world units.")]
    [SerializeField] private float lightRadius = 5f;
    [Tooltip("Inner radius ratio (0..1) relative to outer radius for soft falloff.")]
    [SerializeField] private float innerRadiusRatio = 0.4f;
    [SerializeField] private float lightIntensity = 1f; // 0..1
    [SerializeField] private float smooth = 8f; // smoothing for changes

    [Header("Local Blink")]
    [Tooltip("Default blink frequency in Hz for local light pulsing.")]
    [SerializeField] private float defaultBlinkFrequency = 6f;
    [Tooltip("Default min factor (0..1) for blink intensity.")]
    [SerializeField, Range(0f,1f)] private float defaultBlinkMinFactor = 0f;
    [Tooltip("Default max factor (0..1) for blink intensity.")]
    [SerializeField, Range(0f,1f)] private float defaultBlinkMaxFactor = 1f;

    private bool blinkActive;
    private float blinkEndTime;
    private float blinkFrequency;
    private float blinkMinFactor;
    private float blinkMaxFactor;
    private bool overrideGate; // allows local light even when global gate is off

    private void Awake()
    {
        if (playerLight == null && autoFindChildLight)
        {
            playerLight = GetComponentInChildren<Light2D>();
        }

        if (playerLight == null && autoCreateLightIfMissing)
        {
            var go = new GameObject("Player Light 2D");
            go.transform.SetParent(transform);
            go.transform.localPosition = Vector3.zero;
            playerLight = go.AddComponent<Light2D>();
            playerLight.lightType = Light2D.LightType.Point;
            playerLight.intensity = Mathf.Clamp01(lightIntensity);
            playerLight.pointLightOuterRadius = Mathf.Max(0.01f, lightRadius);
            playerLight.pointLightInnerRadius = Mathf.Clamp01(innerRadiusRatio) * playerLight.pointLightOuterRadius;
        }

        // Prevent first-frame flash when global lighting disables local lights
        if (respectGlobalLightingState && playerLight != null && !LightingState.LocalLightsEnabled)
        {
            playerLight.intensity = 0f;
        }

        
    }

    private void LateUpdate()
    {
        if (playerLight == null) return;
        if (playerLight.lightType != Light2D.LightType.Point)
        {
            playerLight.lightType = Light2D.LightType.Point;
        }

        float outer = Mathf.Max(0.01f, lightRadius);
        float inner = Mathf.Clamp01(innerRadiusRatio) * outer;

        playerLight.pointLightOuterRadius = Mathf.Lerp(playerLight.pointLightOuterRadius, outer, SmoothFactor(smooth));
        playerLight.pointLightInnerRadius = Mathf.Lerp(playerLight.pointLightInnerRadius, inner, SmoothFactor(smooth));

        // Update blink state expiry
        if (blinkActive && Time.time >= blinkEndTime)
        {
            blinkActive = false;
            // do not clear overrideGate here; scene may want to keep it on until gate is enabled
        }

        float baseIntensity = Mathf.Clamp01(lightIntensity);

        // Determine whether gate allows light
        bool gateAllows = !respectGlobalLightingState || LightingState.LocalLightsEnabled || overrideGate;

        float targetIntensity = gateAllows ? baseIntensity : 0f;

        if (gateAllows && blinkActive)
        {
            // Pulse between min and max using PingPong
            float phase = Mathf.PingPong(Time.time * blinkFrequency, 1f);
            float factor = Mathf.Lerp(blinkMinFactor, blinkMaxFactor, phase);
            targetIntensity = baseIntensity * Mathf.Clamp01(factor);
        }

        playerLight.intensity = Mathf.Lerp(playerLight.intensity, targetIntensity, SmoothFactor(smooth));
    }

    private static float SmoothFactor(float s)
    {
        return 1f - Mathf.Exp(-s * Time.deltaTime);
    }

    public void SetRadius(float radius)
    {
        lightRadius = radius;
    }

    public void SetIntensity(float intensity)
    {
        lightIntensity = intensity;
    }

    /// <summary>
    /// Start a local blink for this player light.
    /// During the blink, the light can override the global local-light gate.
    /// </summary>
    public void StartBlink(float duration, float frequency = -1f, float minFactor = -1f, float maxFactor = -1f, bool overrideGate = true)
    {
        blinkActive = true;
        blinkEndTime = Time.time + Mathf.Max(0f, duration);
        blinkFrequency = (frequency > 0f) ? frequency : defaultBlinkFrequency;
        blinkMinFactor = (minFactor >= 0f) ? Mathf.Clamp01(minFactor) : defaultBlinkMinFactor;
        blinkMaxFactor = (maxFactor >= 0f) ? Mathf.Clamp01(maxFactor) : defaultBlinkMaxFactor;
        this.overrideGate = overrideGate;

        // Ensure light exists and is a point light
        if (playerLight != null && playerLight.lightType != Light2D.LightType.Point)
        {
            playerLight.lightType = Light2D.LightType.Point;
        }
    }

    /// <summary>
    /// Explicitly allow or disallow this local light while the global gate is off.
    /// </summary>
    public void SetGateOverride(bool enabled)
    {
        overrideGate = enabled;
    }
}
