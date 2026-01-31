using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Controls room darkness using a Global Light2D.
/// Place this on any GameObject in the scene and assign the Global Light2D.
/// Darkness 0 = bright, 1 = fully dark (intensity = 0).
/// </summary>
public class SceneLightController : MonoBehaviour
{
    [Header("Global Light (URP 2D)")]
    [SerializeField] private Light2D globalLight;
    [SerializeField] private bool autoFindGlobalLight = true;

    [Header("Room Darkness")]
    [Tooltip("0 = bright room, 1 = fully dark.")]
    [SerializeField, Range(0f, 1f)] private float darkness = 0.8f;
    [SerializeField] private bool darknessEnabled = true; // disable to keep full light

    [Header("Startup Blink → Blackout")]
    [SerializeField] private bool startupBlinkEnabled = true;
    [SerializeField] private float startupDelaySeconds = 3f;
    [SerializeField] private float blinkDurationMin = 2f;
    [SerializeField] private float blinkDurationMax = 4f;
    [SerializeField] private float flickerFrequency = 12f; // higher = faster flicker
    [SerializeField, Range(0f, 1f)] private float blinkMinFactor = 0.1f; // min intensity factor during flicker
    [SerializeField, Range(0f, 1f)] private float blinkMaxFactor = 1f;   // max intensity factor during flicker

    private bool blinkingActive;
    private float blinkTimer;
    private float blinkDuration;
    private float flickerSeed;
    private bool lightsCutOff; // true when blackout has occurred

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
    }

    private void Start()
    {
        if (startupBlinkEnabled)
        {
            StartCoroutine(StartBlinkAfterDelay());
        }
    }

    private void Update()
    {
        TickBlinking();
        ApplyLighting();
    }

    private void OnValidate()
    {
        ApplyLighting();
    }

    private void TickBlinking()
    {
        if (!startupBlinkEnabled || lightsCutOff) return;
        if (blinkingActive)
        {
            blinkTimer += Time.deltaTime;
            if (blinkTimer >= blinkDuration)
            {
                blinkingActive = false;
                lightsCutOff = true; // full blackout after flicker period
            }
        }
    }

    private void ApplyLighting()
    {
        if (globalLight == null) return;
        if (globalLight.lightType != Light2D.LightType.Global)
        {
            globalLight.lightType = Light2D.LightType.Global;
        }

        if (lightsCutOff)
        {
            globalLight.intensity = 0f;
            return;
        }

        float baseIntensity = darknessEnabled ? Mathf.Clamp01(1f - darkness) : 1f;

        if (blinkingActive)
        {
            // Smooth flicker using Perlin noise between min/max factors
            float n = Mathf.PerlinNoise(Time.time * flickerFrequency, flickerSeed);
            float factor = Mathf.Lerp(blinkMinFactor, blinkMaxFactor, n);
            globalLight.intensity = baseIntensity * Mathf.Clamp01(factor);
        }
        else
        {
            globalLight.intensity = baseIntensity;
        }
    }

    private IEnumerator StartBlinkAfterDelay()
    {
        yield return new WaitForSeconds(Mathf.Max(0f, startupDelaySeconds));
        blinkDuration = Random.Range(Mathf.Min(blinkDurationMin, blinkDurationMax), Mathf.Max(blinkDurationMin, blinkDurationMax));
        flickerSeed = Random.value * 100f;
        blinkingActive = true;
        blinkTimer = 0f;
    }

    public void SetDarkness(float value)
    {
        darkness = Mathf.Clamp01(value);
        ApplyLighting();
    }

    public void SetDarknessEnabled(bool enabled)
    {
        darknessEnabled = enabled;
        ApplyLighting();
    }

    public void ResetBlackout()
    {
        lightsCutOff = false;
        blinkingActive = false;
        ApplyLighting();
    }
}
