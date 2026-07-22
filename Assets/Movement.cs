using UnityEngine;
using UnityEngine.InputSystem;

public class Movement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Dodge Roll / Dash")]
    [SerializeField] private float dashSpeed = 12f;
    [SerializeField] private float dashDuration = 0.15f;
    [SerializeField] private float dashCooldown = 0.7f;
    [SerializeField] private float dashCoyoteTime = 0.1f;

    [Header("Sprite Facing")]
    [SerializeField] private SpriteRenderer bodySprite;
    [SerializeField] private bool spriteFacesLeftByDefault = false;

    [Header("Animation")]
    [SerializeField] private Animator animator;

    private static readonly int IsMovingHash = Animator.StringToHash("isMoving");
    private static readonly int IsDashingHash = Animator.StringToHash("isDashing");

    private Rigidbody2D rb;
    private Camera cam;
    private Vector2 moveInput;
    private Vector2 lookScreenPos;

    private bool isDashing;
    private float dashTimer;
    private float cooldownTimer;
    private Vector2 dashDir;
    private Vector2 lastMoveDir;
    private float timeSinceMoved;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.rotation = 0f;
        cam = Camera.main;
        if (bodySprite == null)
            bodySprite = GetComponentInChildren<SpriteRenderer>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
        if (moveInput.sqrMagnitude > 0.01f)
        {
            lastMoveDir = moveInput.normalized;
            timeSinceMoved = 0f;
        }
    }

    public void OnLook(InputValue value)
    {
        lookScreenPos = value.Get<Vector2>();
    }

    public void OnDash(InputValue value)
    {
        if (!value.isPressed || isDashing || cooldownTimer > 0f) return;

        bool hasDir = moveInput.sqrMagnitude > 0.01f || timeSinceMoved <= dashCoyoteTime;
        if (!hasDir) return;

        dashDir = lastMoveDir;
        isDashing = true;
        dashTimer = dashDuration;
        cooldownTimer = dashCooldown;
    }

    void Update()
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;

        timeSinceMoved += Time.deltaTime;

        FaceAimDirection();

        if (animator != null)
        {
            animator.SetBool(IsDashingHash, isDashing);
            animator.SetBool(IsMovingHash, moveInput.sqrMagnitude > 0.01f);
        }
    }

    void FixedUpdate()
    {
        if (isDashing)
        {
            dashTimer -= Time.fixedDeltaTime;
            if (dashTimer <= 0f)
                isDashing = false;
            else
            {
                rb.MovePosition(rb.position + dashSpeed * Time.fixedDeltaTime * dashDir);
                return;
            }
        }

        rb.MovePosition(rb.position + moveSpeed * Time.fixedDeltaTime * moveInput);
    }

    Vector2 AimDirection()
    {
        if (cam == null) return Vector2.zero;
        Vector3 world = cam.ScreenToWorldPoint(new Vector3(lookScreenPos.x, lookScreenPos.y, -cam.transform.position.z));
        return (Vector2)world - rb.position;
    }

    void FaceAimDirection()
    {
        if (bodySprite == null) return;
        float x = AimDirection().x;
        if (Mathf.Abs(x) < 0.001f) return;

        bool aimRight = x > 0f;
        bodySprite.flipX = spriteFacesLeftByDefault ? aimRight : !aimRight;
    }
}
