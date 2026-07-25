using UnityEngine;
using UnityEngine.InputSystem;

public class PowerupInteractor : MonoBehaviour
{
    public void OnInteract(InputValue value)
    {
        if (!value.isPressed) return;

        Powerup target = Powerup.NearestInRange(transform.position);
        if (target != null) target.Claim();
    }
}
