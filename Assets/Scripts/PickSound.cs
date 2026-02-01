using UnityEngine;

/// <summary>
/// Plays pickup sounds using AudioSource components.
/// Attach to a Mask prefab to auto-play a sound when the Player picks it up.
/// Also exposes a public method to play a generic item pickup sound.
/// </summary>
public class PickSound : MonoBehaviour
{
    [Header("Audio Sources")]
    [Tooltip("AudioSource used for mask pickup sound. Assign a source with its clip set.")]
    [SerializeField] private AudioSource maskAudioSource;
    [Tooltip("AudioSource used for generic item pickup sound. Assign a source with its clip set.")]
    [SerializeField] private AudioSource itemAudioSource;

    [Header("Settings")]
    [SerializeField, Range(0f, 1f)] private float volume = 1f;
    [SerializeField] private string playerTag = "Player";
    [Tooltip("If true, plays the mask pickup sound automatically when the Player enters this trigger.")]
    [SerializeField] private bool playMaskOnTrigger = true;
    [Tooltip("If true, plays the item pickup sound automatically when the Player enters this trigger.")]
    [SerializeField] private bool playItemOnTrigger = false;

    [Header("Optional")]
    [Tooltip("If true, tries to use an AudioSource on this GameObject for mask sound when none is assigned.")]
    [SerializeField] private bool autoUseSelfAudioSourceForMask = true;
    [Tooltip("If true, tries to use an AudioSource on this GameObject for item sound when none is assigned.")]
    [SerializeField] private bool autoUseSelfAudioSourceForItem = false;
    [Tooltip("If true, ensures there's a Collider2D set as trigger on this GameObject.")]
    [SerializeField] private bool autoMarkColliderAsTrigger = true;

    [Header("Pickup Actions")]
    [Tooltip("If true, destroys this GameObject on player pickup.")]
    [SerializeField] private bool destroyOnPickup = true;
    [Tooltip("If true, waits until the played audio clip finishes before destroying.")]
    [SerializeField] private bool destroyAfterAudio = true;
    [Tooltip("Fallback delay for destroy when no clip is available or destroyAfterAudio is false.")]
    [SerializeField] private float destroyDelaySeconds = 0f;

    private void Awake()
    {
        if (maskAudioSource == null && autoUseSelfAudioSourceForMask)
        {
            maskAudioSource = GetComponent<AudioSource>();
        }
        if (itemAudioSource == null && autoUseSelfAudioSourceForItem)
        {
            itemAudioSource = GetComponent<AudioSource>();
        }
        if (autoMarkColliderAsTrigger)
        {
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;

        if (playMaskOnTrigger)
        {
            PlayMaskPickupSound();
        }
        if (playItemOnTrigger)
        {
            PlayItemPickupSound();
        }

        if (destroyOnPickup)
        {
            float delay = destroyDelaySeconds;
            if (destroyAfterAudio)
            {
                // Prefer the source we just used; if both toggles, pick item first
                AudioSource src = null;
                if (playItemOnTrigger && itemAudioSource != null) src = itemAudioSource;
                else if (playMaskOnTrigger && maskAudioSource != null) src = maskAudioSource;
                if (src != null && src.clip != null)
                {
                    // Adjust for pitch
                    float length = src.clip.length;
                    float pitch = Mathf.Approximately(src.pitch, 0f) ? 1f : src.pitch;
                    delay = length / pitch;
                }
            }
            if (delay <= 0f)
            {
                Destroy(gameObject);
            }
            else
            {
                Destroy(gameObject, delay);
            }
        }
    }

    /// <summary>
    /// Plays the mask pickup clip at this object's position.
    /// Call this manually if you don't want to use trigger auto-play.
    /// </summary>
    public void PlayMaskPickupSound()
    {
        if (maskAudioSource == null) return;
        maskAudioSource.volume = volume;
        maskAudioSource.Play();
    }

    /// <summary>
    /// Plays the generic item pickup clip at this object's position.
    /// Use from other item scripts on pickup.
    /// </summary>
    public void PlayItemPickupSound()
    {
        if (itemAudioSource == null) return;
        itemAudioSource.volume = volume;
        itemAudioSource.Play();
    }
}
