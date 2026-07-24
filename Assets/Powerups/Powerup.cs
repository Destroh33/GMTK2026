using UnityEngine;

public class Powerup : MonoBehaviour
{
    [SerializeField] PowerupData data;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    private void OnTriggerEnter2D(Collider2D other) {
        if(other.gameObject.tag == "Player")
        {
            PlayerMovement playerMovement = other.gameObject.GetComponent<PlayerMovement>();
            PlayerHealth playerHealth = other.gameObject.GetComponent<PlayerHealth>();
            switch(data.type)
            {
                case PowerupType.Heal:
                    playerHealth.Heal((int)data.factor); //heal expects int
                    break;
                case PowerupType.MoveSpeed:
                    playerMovement.ModifyMoveSpeed(data.factor);
                    break;
                case PowerupType.DashSpeed:
                    playerMovement.ModifyDashSpeed(data.factor);
                    break;
                case PowerupType.FireRate:
                    ///implement later
                    break;
                case PowerupType.Damage:
                    //implement later
                    break;
            }

            Destroy(gameObject);
        }
    }
}
