using UnityEngine;

public class PlayerMoment : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    private Rigidbody2D playerRB;
    private Animator plyAnim;

    // Speed multiplier for mask abilities
    private float speedMultiplier = 1f;
    private float baseMoveSpeed;

    private void Awake()
    {
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
