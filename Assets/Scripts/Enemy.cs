using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Patrol Settings")]
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float patrolDistance = 5f;  // How far to walk before turning

    [Header("Detection Settings")]
    [SerializeField] private float detectionRadius = 4f;   // Medium range - starts following
    [SerializeField] private float chaseRadius = 6f;       // How far enemy will chase before giving up
    [SerializeField] private float chaseSpeed = 3.5f;

    [Header("References")]
    [SerializeField] private Rigidbody2D enemyRB;
    [SerializeField] private Animator enemyAnim;
    [SerializeField] private SpriteRenderer enemySprite;

    [Header("Layer Settings")]
    [SerializeField] private LayerMask playerLayer;

    // Private variables
    private Vector2 startPosition;
    private Vector2 moveDirection;
    private Transform playerTarget;
    private bool isChasing = false;
    private bool movingRight = true;

    // Animation parameter names (change these to match your animator)
    private readonly string ANIM_MOVE_X = "moveX";
    private readonly string ANIM_MOVE_Y = "moveY";
    private readonly string ANIM_IS_MOVING = "isMoving";

    private void Awake()
    {
        // Get components if not assigned
        if (enemyRB == null)
            enemyRB = GetComponent<Rigidbody2D>();

        if (enemyAnim == null)
            enemyAnim = GetComponent<Animator>();

        if (enemySprite == null)
            enemySprite = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        // Store starting position for patrol
        startPosition = transform.position;
        moveDirection = Vector2.right;
    }

    private void Update()
    {
        // Check for player in detection range
        CheckForPlayer();

        // Update animations
        UpdateAnimation();
    }

    private void FixedUpdate()
    {
        if (isChasing && playerTarget != null)
        {
            ChasePlayer();
        }
        else
        {
            Patrol();
        }
    }

    /// <summary>
    /// Check if player is within detection radius
    /// </summary>
    private void CheckForPlayer()
    {
        // Find player in detection radius
        Collider2D playerCollider = Physics2D.OverlapCircle(transform.position, detectionRadius, playerLayer);

        if (playerCollider != null)
        {
            // Check if player is invisible (from mask)
            PlayerMaskSystem maskSystem = playerCollider.GetComponent<PlayerMaskSystem>();
            if (maskSystem != null && maskSystem.IsInvisible())
            {
                // Can't see invisible player
                StopChasing();
                return;
            }

            // Player detected! Start chasing
            playerTarget = playerCollider.transform;
            isChasing = true;
        }
        else if (isChasing && playerTarget != null)
        {
            // Player left detection radius - check if still in chase radius
            float distanceToPlayer = Vector2.Distance(transform.position, playerTarget.position);
            
            if (distanceToPlayer > chaseRadius)
            {
                // Too far, stop chasing
                StopChasing();
            }
        }
    }

    /// <summary>
    /// Stop chasing and return to patrol
    /// </summary>
    private void StopChasing()
    {
        isChasing = false;
        playerTarget = null;
    }

    /// <summary>
    /// Patrol back and forth on X axis
    /// </summary>
    private void Patrol()
    {
        // Calculate current distance from start
        float distanceFromStart = transform.position.x - startPosition.x;

        // Check if need to turn around
        if (movingRight && distanceFromStart >= patrolDistance)
        {
            movingRight = false;
            moveDirection = Vector2.left;
        }
        else if (!movingRight && distanceFromStart <= -patrolDistance)
        {
            movingRight = true;
            moveDirection = Vector2.right;
        }

        // Move enemy
        enemyRB.linearVelocity = moveDirection * patrolSpeed;

        // Flip sprite based on direction
        FlipSprite(moveDirection.x);
    }

    /// <summary>
    /// Chase the player
    /// </summary>
    private void ChasePlayer()
    {
        if (playerTarget == null)
            return;

        // Get direction to player
        Vector2 directionToPlayer = (playerTarget.position - transform.position).normalized;

        // Move towards player
        enemyRB.linearVelocity = directionToPlayer * chaseSpeed;

        // Update move direction for animation
        moveDirection = directionToPlayer;

        // Flip sprite based on direction
        FlipSprite(directionToPlayer.x);
    }

    /// <summary>
    /// Flip sprite based on movement direction
    /// </summary>
    private void FlipSprite(float xDirection)
    {
        if (enemySprite == null)
            return;

        if (xDirection > 0)
        {
            // Moving right
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x);
            transform.localScale = scale;
        }
        else if (xDirection < 0)
        {
            // Moving left
            Vector3 scale = transform.localScale;
            scale.x = -Mathf.Abs(scale.x);
            transform.localScale = scale;
        }
    }

    /// <summary>
    /// Update animator parameters
    /// </summary>
    private void UpdateAnimation()
    {
        if (enemyAnim == null)
            return;

        // Set movement direction
        enemyAnim.SetFloat(ANIM_MOVE_X, moveDirection.x);
        enemyAnim.SetFloat(ANIM_MOVE_Y, moveDirection.y);

        // Set if moving
        bool isMoving = enemyRB.linearVelocity.magnitude > 0.1f;
        enemyAnim.SetBool(ANIM_IS_MOVING, isMoving);
    }

    /// <summary>
    /// Called when enemy touches something
    /// </summary>
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Check if hit player
        if (collision.gameObject.CompareTag("Player"))
        {
            // Check if player has sacrifice advantage
            PlayerMaskSystem maskSystem = collision.gameObject.GetComponent<PlayerMaskSystem>();
            if (maskSystem != null && maskSystem.HasSacrificeAdvantage())
            {
                // Player has advantage - enemy can't hurt them
                Debug.Log("Player has sacrifice advantage! Enemy can't hurt them.");
                return;
            }

            // Damage or kill player here
            Debug.Log("Enemy touched player!");
            // You can add: maskSystem.KillPlayer(); or health system
        }
    }

    /// <summary>
    /// Draw gizmos in editor for easy setup
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // Draw detection radius (yellow)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        // Draw chase radius (red)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, chaseRadius);

        // Draw patrol path (green)
        Gizmos.color = Color.green;
        Vector3 startPos = Application.isPlaying ? (Vector3)startPosition : transform.position;
        Vector3 leftPoint = startPos + Vector3.left * patrolDistance;
        Vector3 rightPoint = startPos + Vector3.right * patrolDistance;
        Gizmos.DrawLine(leftPoint, rightPoint);
        Gizmos.DrawSphere(leftPoint, 0.2f);
        Gizmos.DrawSphere(rightPoint, 0.2f);
    }
}
