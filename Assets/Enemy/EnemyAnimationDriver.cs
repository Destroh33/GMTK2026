using UnityEngine;

// Drives the "moving" bool + sprite flip for enemies whose animator has an
// idle/walk pair. ShieldEnemy handles its own animator, so it doesn't need this.
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyAnimationDriver : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private bool flipToFaceMovement = true;
    [SerializeField] private float movingThreshold = 0.05f;

    static readonly int MovingHash = Animator.StringToHash("moving");

    private Animator animator;
    private Rigidbody2D rb;
    private bool hasMovingParam;

    void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        // Single-state animators (bat, leech) have no "moving" bool; setting a
        // parameter that doesn't exist spams warnings every frame.
        foreach (AnimatorControllerParameter p in animator.parameters)
        {
            if (p.type == AnimatorControllerParameterType.Bool && p.nameHash == MovingHash)
            {
                hasMovingParam = true;
                break;
            }
        }
    }

    void FixedUpdate()
    {
        Vector2 velocity = rb.linearVelocity;

        if (hasMovingParam)
            animator.SetBool(MovingHash, velocity.sqrMagnitude > movingThreshold * movingThreshold);

        if (flipToFaceMovement && spriteRenderer != null && Mathf.Abs(velocity.x) > movingThreshold)
            spriteRenderer.flipX = velocity.x > 0;
    }
}
