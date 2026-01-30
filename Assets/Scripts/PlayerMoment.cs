using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.RuleTile.TilingRuleOutput;

public class PlayerMoment : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 5f;
    private Rigidbody2D playerRB;
    private Animator plyAnim;
    private bool grounded;

    private void Awake()
    {
        playerRB = GetComponent<Rigidbody2D>();
        plyAnim = GetComponent<Animator>();
    }

    private void Update()
    {
        float keyCheck = Keyboard.current.aKey.isPressed ? -1 :
        Keyboard.current.dKey.isPressed ? 1 : 0;
        playerRB.linearVelocity = new Vector2(keyCheck * moveSpeed, playerRB.linearVelocity.y);

        if (keyCheck != 0)
        {
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Sign(keyCheck) * Mathf.Abs(scale.x);
            transform.localScale = scale;
        }

        //Animation
        plyAnim.SetBool("runPM", keyCheck != 0);
    }


}
