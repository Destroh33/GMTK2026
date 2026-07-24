using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float moveSpeedModifier = 1f; //used by powerup to multiply base movespeed
    [SerializeField] private float acceleration = 60f;
    [SerializeField] private float deceleration = 45f;
    [SerializeField] private float turnBoost = 1.6f;

    [Header("Dodge Roll / Dash")]
    [SerializeField] private float dashSpeed = 12f;
    [SerializeField] private float dashSpeedModifier = 1f; //used by powerup to multiply base dash speed
    [SerializeField] private float dashDuration = 0.15f;
    [SerializeField] private float dashCooldown = 0.7f;
    [SerializeField] private float dashCoyoteTime = 0.1f;
    [SerializeField] private AnimationCurve dashSpeedCurve = new AnimationCurve(
        new Keyframe(0f, 1.35f), new Keyframe(0.6f, 1f), new Keyframe(1f, 0.55f));
    [SerializeField] private float dashEndMomentum = 0.55f;
    [SerializeField] private float dashInputBufferTime = 0.12f;


    [Header("Dash Collision")]
    [SerializeField] private string dashingLayer = "Dashing";

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
    private int defaultLayer;
    private int dashingLayerIndex;
    private Vector2 velocity;
    private float dashBufferTimer;

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

        defaultLayer = gameObject.layer;
        dashingLayerIndex = LayerMask.NameToLayer(dashingLayer);
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
        if (!value.isPressed) return;
        dashBufferTimer = dashInputBufferTime;
        TryStartDash();
    }

    void TryStartDash()
    {
        if (isDashing || cooldownTimer > 0f) return;

        bool hasDir = moveInput.sqrMagnitude > 0.01f || timeSinceMoved <= dashCoyoteTime;
        if (!hasDir) return;

        dashDir = lastMoveDir;
        isDashing = true;
        dashTimer = dashDuration;
        cooldownTimer = dashCooldown;
        dashBufferTimer = 0f;
        SetLayerRecursive(dashingLayerIndex >= 0 ? dashingLayerIndex : defaultLayer);
    }

    void Update()
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;

        if (dashBufferTimer > 0f)
        {
            dashBufferTimer -= Time.deltaTime;
            TryStartDash();
        }

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
        float dt = Time.fixedDeltaTime;

        if (isDashing)
        {
            dashTimer -= dt;

            if (dashTimer > 0f)
            {
                float t = 1f - Mathf.Clamp01(dashTimer / dashDuration);
                float speed = dashSpeed * dashSpeedModifier * dashSpeedCurve.Evaluate(t);
                velocity = dashDir * speed;
                rb.MovePosition(rb.position + velocity * dt);
                return;
            }

            isDashing = false;
            SetLayerRecursive(defaultLayer);
            velocity = dashDir * (dashSpeed * dashSpeedModifier * dashEndMomentum);
        }

        Vector2 target = moveInput * (moveSpeed * moveSpeedModifier);
        bool hasInput = moveInput.sqrMagnitude > 0.01f;

        float rate = hasInput ? acceleration : deceleration;

        if (hasInput && Vector2.Dot(velocity, target) < 0f)
            rate *= turnBoost;

        velocity = Vector2.MoveTowards(velocity, target, rate * dt);

        rb.MovePosition(rb.position + velocity * dt);
    }

    void SetLayerRecursive(int layer)
    {
        gameObject.layer = layer;
        foreach (Collider2D col in GetComponentsInChildren<Collider2D>(true))
            col.gameObject.layer = layer;
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

    public void ModifyMoveSpeed(float factor)
    {
        moveSpeedModifier *= factor;   
    }

    public void ModifyDashSpeed(float factor)
    {
        dashSpeedModifier *= factor;
    }
}
