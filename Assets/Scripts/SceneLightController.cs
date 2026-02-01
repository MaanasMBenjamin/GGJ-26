using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Controls global light to stay bright and orchestrates local startup blink:
/// enemies blink first, then player light blinks. No room darkness.
/// </summary>
[DefaultExecutionOrder(-100)]
public class SceneLightController : MonoBehaviour
{
    [Header("Global Light (URP 2D)")]
    [SerializeField] private Light2D globalLight;
    [SerializeField] private bool autoFindGlobalLight = true;
    
    [Header("Global Darkness (Optional)")]
    [Tooltip("Enable to dim the entire scene using Global Light2D intensity.")]
    [SerializeField] private bool darknessEnabled = false;
    [Tooltip("0 = bright scene, 1 = fully dark (global intensity 0).")]
    [SerializeField, Range(0f,1f)] private float darkness = 0f;

    [Header("Local Startup Blink (Enemies → Player)")]
    [SerializeField] private bool localStartupBlinkEnabled = true;
    [SerializeField] private float startupDelaySeconds = 0.5f;
    [SerializeField] private float enemyBlinkDuration = 1.2f; // editable duration
    [SerializeField] private float playerBlinkDuration = 1.2f; // editable duration
    [SerializeField] private bool playerBlinkEnabled = false; // if false, player light turns on without blinking
    [SerializeField] private float playerOnDelaySeconds = 1.9f; // delay before turning player light on when no blink
    [SerializeField] private bool playerOnAtStart = true; // if true, player light is visible from game start
    [SerializeField] private float blinkFrequency = 6f;
    [SerializeField, Range(0f,1f)] private float blinkMinFactor = 0f;
    [SerializeField, Range(0f,1f)] private float blinkMaxFactor = 1f;

    private PlayerLight playerLightScript;
    private Enemy[] enemies;

    private void Awake()
    {
        if (globalLight == null && autoFindGlobalLight)
        {
            // Find first Global Light2D in scene (using non-sorted, fast API)
            Light2D[] lights = UnityEngine.Object.FindObjectsByType<Light2D>(FindObjectsSortMode.None);
            foreach (var l in lights)
            {
                if (l.lightType == Light2D.LightType.Global)
                {
                    globalLight = l;
                    break;
                }
            }
        }

        // Ensure global light is correctly typed
        if (globalLight != null)
        {
            globalLight.lightType = Light2D.LightType.Global;
        }

        // Prepare local lights gate for startup blink sequence
        LightingState.SetLocalLightsEnabled(!localStartupBlinkEnabled);
    }

    private void Start()
    {
        // Cache references
        var players = UnityEngine.Object.FindObjectsByType<PlayerLight>(FindObjectsSortMode.None);
        if (players != null && players.Length > 0)
        {
            playerLightScript = players[0];
        }
        enemies = UnityEngine.Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);

        // If player should be on from start, allow it regardless of the global local-light gate
        if (playerOnAtStart && playerLightScript != null)
        {
            playerLightScript.SetGateOverride(true);
        }

        if (localStartupBlinkEnabled)
        {
            StartCoroutine(RunLocalStartupBlinkSequence());
        }
    }

    private void Update()
    {
        // Apply optional darkness to global light
        if (globalLight != null)
        {
            if (globalLight.lightType != Light2D.LightType.Global)
            {
                globalLight.lightType = Light2D.LightType.Global;
            }
            float target = darknessEnabled ? Mathf.Clamp01(1f - darkness) : 1f;
            globalLight.intensity = target;
        }
    }

    private void OnValidate()
    {
        if (globalLight != null)
        {
            globalLight.lightType = Light2D.LightType.Global;
            float target = darknessEnabled ? Mathf.Clamp01(1f - darkness) : 1f;
            globalLight.intensity = target;
        }
    }

    private IEnumerator RunLocalStartupBlinkSequence()
    {
        yield return new WaitForSeconds(Mathf.Max(0f, startupDelaySeconds));

        // Ensure scene stays bright
        if (globalLight != null)
        {
            globalLight.lightType = Light2D.LightType.Global;
            globalLight.intensity = 1f;
        }

        // Enemy lights blink first
        if (enemies != null && enemies.Length > 0)
        {
            foreach (var e in enemies)
            {
                if (e != null) e.StartLightBlink(enemyBlinkDuration, blinkFrequency, blinkMinFactor, blinkMaxFactor, true);
            }
            yield return new WaitForSeconds(Mathf.Max(0f, enemyBlinkDuration));
            // After blink ends, keep enemies on even while gate is still off
            foreach (var e in enemies)
            {
                if (e != null) e.SetGateOverride(true);
            }
        }

        // Then player light: blink if enabled, otherwise turn on (optionally from start) without blink
        if (playerLightScript != null)
        {
            if (playerBlinkEnabled && playerBlinkDuration > 0f)
            {
                playerLightScript.StartBlink(playerBlinkDuration, blinkFrequency, blinkMinFactor, blinkMaxFactor, true);
                yield return new WaitForSeconds(Mathf.Max(0f, playerBlinkDuration));
                LightingState.SetLocalLightsEnabled(true);
                // Cleanup: enemies no longer need gate override
                if (enemies != null)
                {
                    foreach (var e in enemies)
                    {
                        if (e != null) e.SetGateOverride(false);
                    }
                }
            }
            else
            {
                if (playerOnAtStart)
                {
                    // Player already visible; enable gate immediately after enemy blink
                    LightingState.SetLocalLightsEnabled(true);
                    // Player no longer needs override
                    playerLightScript.SetGateOverride(false);
                }
                else
                {
                    // Wait specified delay, then enable gate so lights are steady-on
                    yield return new WaitForSeconds(Mathf.Max(0f, playerOnDelaySeconds));
                    LightingState.SetLocalLightsEnabled(true);
                }
                // Cleanup: enemies no longer need gate override
                if (enemies != null)
                {
                    foreach (var e in enemies)
                    {
                        if (e != null) e.SetGateOverride(false);
                    }
                }
            }
        }
        else
        {
            // No player light found; still respect the delay before enabling gate
            yield return new WaitForSeconds(Mathf.Max(0f, playerOnDelaySeconds));
            LightingState.SetLocalLightsEnabled(true);
            if (enemies != null)
            {
                foreach (var e in enemies)
                {
                    if (e != null) e.SetGateOverride(false);
                }
            }
        }
    }

    // Public setters to adjust darkness from UI or code
    public void SetDarkness(float value)
    {
        darkness = Mathf.Clamp01(value);
        if (globalLight != null)
        {
            float target = darknessEnabled ? Mathf.Clamp01(1f - darkness) : 1f;
            globalLight.intensity = target;
        }
    }

    public void SetDarknessEnabled(bool enabled)
    {
        darknessEnabled = enabled;
        if (globalLight != null)
        {
            float target = darknessEnabled ? Mathf.Clamp01(1f - darkness) : 1f;
            globalLight.intensity = target;
        }
    }
}
