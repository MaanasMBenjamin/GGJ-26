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

    [Tooltip("Outer radius of the player light in world units.")]
    [SerializeField] private float lightRadius = 5f;
    [Tooltip("Inner radius ratio (0..1) relative to outer radius for soft falloff.")]
    [SerializeField] private float innerRadiusRatio = 0.4f;
    [SerializeField] private float lightIntensity = 1f; // 0..1
    [SerializeField] private float smooth = 8f; // smoothing for changes

    private void Awake()
    {
        if (playerLight == null && autoFindChildLight)
        {
            playerLight = GetComponentInChildren<Light2D>();
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
        playerLight.intensity = Mathf.Lerp(playerLight.intensity, Mathf.Clamp01(lightIntensity), SmoothFactor(smooth));
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
}
