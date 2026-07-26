using UnityEngine;

// Always plays the floating animation, facing whichever side the player is
// currently on - "BossFloatLeft" when the player is to the left, otherwise
// "BossFloatRight" (both Animator states already set up in Boss.controller).
public class BossAnimator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private Transform player;

    const string MovingLeftState = "BossFloatLeft";
    const string MovingRightState = "BossFloatRight";

    private string currentAnimState;

    void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
    }

    void Start()
    {
        if (player == null)
        {
            PlayerMovement p = FindAnyObjectByType<PlayerMovement>();
            if (p != null) player = p.transform;
        }
    }

    void Update()
    {
        if (player == null) return;

        bool playerToLeft = player.position.x < transform.position.x;
        PlayState(playerToLeft ? MovingRightState : MovingLeftState);
    }

    void PlayState(string stateName)
    {
        if (animator == null) return;
        if (currentAnimState == stateName) return;

        animator.Play(stateName);
        currentAnimState = stateName;
    }
}
