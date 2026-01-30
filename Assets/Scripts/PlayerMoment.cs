using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;

    private Rigidbody2D rb;
    private Animator anim;
    private Vector2 moveInput;

    private PlayerInputActions inputActions;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();

        inputActions = new PlayerInputActions();
    }

    void OnEnable()
    {
        inputActions.Player.Enable();
    }

    void OnDisable()
    {
        inputActions.Player.Disable();
    }

    void Update()
    {
        moveInput = inputActions.Player.Move.ReadValue<Vector2>();

        // Animator parameters
        anim.SetFloat("MoveX", moveInput.x);
        anim.SetFloat("MoveY", moveInput.y);
        anim.SetBool("IsMoving", moveInput != Vector2.zero);

        // Flip sprite when moving left/right
        if (moveInput.x > 0)
        {
            transform.localScale = new Vector3(1, 1, 1); // Face right
        }
        else if (moveInput.x < 0)
        {
            transform.localScale = new Vector3(-1, 1, 1); // Face left (flipped)
        }
    }

    void FixedUpdate()
    {
        rb.MovePosition(rb.position + moveInput.normalized * moveSpeed * Time.fixedDeltaTime);
    }
}
