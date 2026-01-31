using UnityEngine;

/// <summary>
/// SacrificeZone - Place this in areas where Orange mask must be picked up
/// If player doesn't pick up the Orange mask within the zone, they die!
/// </summary>
public class SacrificeZone : MonoBehaviour
{
    [Header("Zone Settings")]
    [SerializeField] private float timeToPickUp = 5f;  // Seconds to pick up mask before death
    [SerializeField] private bool killIfNoPick = true;
    
    [Header("Visual Feedback")]
    [SerializeField] private SpriteRenderer zoneVisual;
    [SerializeField] private Color warningColor = Color.red;
    [SerializeField] private Color safeColor = Color.green;

    [Header("Required Mask in Zone")]
    [SerializeField] private Mask sacrificeMask;  // The orange mask in this zone

    private float timer = 0f;
    private bool playerInZone = false;
    private bool maskPicked = false;
    private PlayerMaskSystem playerMaskSystem;

    private void Start()
    {
        // Subscribe to mask pickup event if mask exists
        if (sacrificeMask != null)
        {
            // We'll check in Update instead
        }

        // Set initial visual
        if (zoneVisual != null)
            zoneVisual.color = warningColor;
    }

    private void Update()
    {
        if (playerInZone && !maskPicked && killIfNoPick)
        {
            timer += Time.deltaTime;

            // Flash warning as time runs out
            if (zoneVisual != null)
            {
                float flashSpeed = Mathf.Lerp(1f, 10f, timer / timeToPickUp);
                float alpha = Mathf.Abs(Mathf.Sin(Time.time * flashSpeed));
                Color c = warningColor;
                c.a = Mathf.Lerp(0.3f, 1f, alpha);
                zoneVisual.color = c;
            }

            // Time's up!
            if (timer >= timeToPickUp)
            {
                KillPlayer();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInZone = true;
            playerMaskSystem = other.GetComponent<PlayerMaskSystem>();
            timer = 0f;

            Debug.Log("DANGER! Pick up the mask or die in " + timeToPickUp + " seconds!");
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player") && playerMaskSystem != null)
        {
            // Check if player picked up the sacrifice mask
            if (playerMaskSystem.HasMask && 
                playerMaskSystem.CurrentMask != null && 
                playerMaskSystem.CurrentMask.Type == MaskType.Orange)
            {
                maskPicked = true;
                
                // Safe! (for now...)
                if (zoneVisual != null)
                    zoneVisual.color = safeColor;
                    
                Debug.Log("Mask picked! You're safe... for now.");
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // If leaving without mask = instant death
            if (!maskPicked && killIfNoPick)
            {
                Debug.Log("You left the zone without the mask!");
                KillPlayer();
            }
            
            playerInZone = false;
            timer = 0f;
        }
    }

    private void KillPlayer()
    {
        if (playerMaskSystem != null)
        {
            Debug.Log("SACRIFICE ZONE: Player didn't pick mask in time!");
            playerMaskSystem.KillPlayer();
        }
    }

    // Gizmo to show zone in editor
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f); // Orange transparent
        
        // Draw based on collider type
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box != null)
        {
            Gizmos.DrawCube(transform.position + (Vector3)box.offset, box.size);
        }

        CircleCollider2D circle = GetComponent<CircleCollider2D>();
        if (circle != null)
        {
            Gizmos.DrawSphere(transform.position + (Vector3)circle.offset, circle.radius);
        }
    }
}
