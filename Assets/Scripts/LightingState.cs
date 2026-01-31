/// <summary>
/// Global lighting state used to coordinate local player/enemy lights
/// with scene-wide effects (flicker/blackout).
/// </summary>
public static class LightingState
{
    // When false, player/enemy local lights should be off.
    public static bool LocalLightsEnabled = true;

    public static void SetLocalLightsEnabled(bool enabled)
    {
        LocalLightsEnabled = enabled;
    }
}
