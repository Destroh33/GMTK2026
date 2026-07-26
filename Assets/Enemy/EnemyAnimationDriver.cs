using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyAnimationDriver : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private bool flipToFaceMovement = true;
    [SerializeField] private bool spriteFacesLeftByDefault = true;
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
        {
            bool moveRight = velocity.x > 0;
            spriteRenderer.flipX = spriteFacesLeftByDefault ? moveRight : !moveRight;
        }
    }
}
