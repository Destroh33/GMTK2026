using UnityEngine;

public enum PowerupType
{
    Heal,
    MoveSpeed,
    DashSpeed,
    FireRate,
    Damage,
    //.. etc etc etc
}
[CreateAssetMenu(fileName = "PowerupData", menuName = "Scriptable Objects/PowerupData")]
public class PowerupData : ScriptableObject
{
    public PowerupType type;
    public float factor; //multiplication vs addition is handled by checking the poweruptype
}
