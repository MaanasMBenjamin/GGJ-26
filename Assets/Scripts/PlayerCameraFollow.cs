using UnityEngine;

/// <summary>
/// Attach this to the Player. Makes the camera follow the player
/// and lets you control how wide the player can see via a serialized field.
/// Works best with an Orthographic camera for top-down games.
/// </summary>
public class PlayerCameraFollow : MonoBehaviour
{
    [Header("Camera Reference")]
    [SerializeField] private Camera targetCamera; // if null, uses Camera.main

    [Header("Follow Settings")]
    [SerializeField] private Vector2 offset = Vector2.zero; // positional offset from player
    [SerializeField] private float followSmooth = 8f;        // higher = snappier

    [Header("View Width (Orthographic)")]
    [Tooltip("Desired view width (in world units). Only applies to Orthographic cameras.")]
    [SerializeField] private bool controlViewWidth = true;
    [SerializeField] private float viewWidth = 12f;         // how wide the player can see
    [SerializeField] private float minViewWidth = 6f;       // optional clamp
    [SerializeField] private float maxViewWidth = 30f;      // optional clamp
    [SerializeField] private float zoomSmooth = 6f;         // smoothness of width changes

    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }

    private void LateUpdate()
    {
        if (targetCamera == null) return;

        // Follow player with smoothing; keep current camera Z (usually -10)
        float z = targetCamera.transform.position.z;
        Vector3 targetPos = new Vector3(transform.position.x + offset.x,
                                        transform.position.y + offset.y,
                                        z);
        targetCamera.transform.position = SmoothLerp(targetCamera.transform.position, targetPos, followSmooth);

        // Control visible width for orthographic cameras
        if (controlViewWidth && targetCamera.orthographic)
        {
            float clampedWidth = Mathf.Clamp(viewWidth, minViewWidth, maxViewWidth);
            // orthographicSize is half of vertical size; width = 2 * size * aspect
            float targetSize = (clampedWidth * 0.5f) / targetCamera.aspect;
            float newSize = Mathf.Lerp(targetCamera.orthographicSize, targetSize, SmoothFactor(zoomSmooth));
            targetCamera.orthographicSize = newSize;
        }
    }

    // Exponential smoothing factor for frame-rate independent feel
    private static float SmoothFactor(float smooth)
    {
        return 1f - Mathf.Exp(-smooth * Time.deltaTime);
    }

    private static Vector3 SmoothLerp(Vector3 from, Vector3 to, float smooth)
    {
        return Vector3.Lerp(from, to, SmoothFactor(smooth));
    }

    /// <summary>
    /// Set desired view width (world units) at runtime.
    /// </summary>
    public void SetViewWidth(float width)
    {
        viewWidth = width;
    }

    /// <summary>
    /// Get current view width (world units) if orthographic; otherwise 0.
    /// </summary>
    public float GetCurrentViewWidth()
    {
        if (targetCamera != null && targetCamera.orthographic)
        {
            return targetCamera.orthographicSize * targetCamera.aspect * 2f;
        }
        return 0f;
    }
}
