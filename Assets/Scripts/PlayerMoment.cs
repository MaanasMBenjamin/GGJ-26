using UnityEngine;

public class PlayerMoment : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    private Rigidbody2D playerRB;
    private Animator plyAnim;

    private void Awake()
    {
        playerRB = GetComponent<Rigidbody2D>();
        plyAnim = GetComponent<Animator>();
    }

    private void Update()
    {
        float hor = Input.GetAxisRaw("Horizontal");
        float ver = Input.GetAxisRaw("Vertical");

        Vector2 moveDir = new Vector2(hor, ver).normalized;
        playerRB.linearVelocity = moveDir * moveSpeed;


        // Animator parameters
        plyAnim.SetFloat("moveX", hor);
        plyAnim.SetFloat("moveY", ver);
        plyAnim.SetBool("isMoving", moveDir != Vector2.zero);
    }

}
