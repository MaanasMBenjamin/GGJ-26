using UnityEngine;

public class PlayerMoment : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    private Rigidbody2D playerRB;
    private Animator plyAnim;

    public static PlayerMoment Instance { get; private set; }

    // Speed multiplier for mask abilities
    private float speedMultiplier = 1f;
    private float baseMoveSpeed;

    private void Awake()
    {
        // Singleton pattern - destroy duplicates
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        playerRB = GetComponent<Rigidbody2D>();
        plyAnim = GetComponent<Animator>();
        baseMoveSpeed = moveSpeed;
    }

    private void Update()
    {
        float hor = Input.GetAxisRaw("Horizontal");
        float ver = Input.GetAxisRaw("Vertical");

        Vector2 moveDir = new Vector2(hor, ver).normalized;
        
        // Apply speed with multiplier (for mask abilities)
        float currentSpeed = baseMoveSpeed * speedMultiplier;
        playerRB.linearVelocity = moveDir * currentSpeed;


        // Animator parameters
        plyAnim.SetFloat("moveX", hor);
        plyAnim.SetFloat("moveY", ver);
        plyAnim.SetBool("isMoving", moveDir != Vector2.zero);
    }

private void OnTriggerEnter2D(Collider2D other)
{
    // Skip if we collided with ourselves or another player
    if (other.gameObject == gameObject) return;
    
    // Try to get MaskLogic directly - if it has the component, it's a mask
    if (other.TryGetComponent<MaskLogic>(out MaskLogic mask))
    {
        Debug.Log($"<color=green>HOLY RELIC DETECTED: {other.name}</color>");
        mask.OnCollected(this);
    }
    // Silent for everything else - no spam in console
}
    

    /// <summary>
    /// Apply a speed multiplier (used by mask abilities)
    /// </summary>
    public void ApplySpeedMultiplier(float multiplier)
    {
        speedMultiplier = multiplier;
    }

    /// <summary>
    /// Get current move speed
    /// </summary>
    public float GetCurrentSpeed()
    {
        return baseMoveSpeed * speedMultiplier;
    }
}
