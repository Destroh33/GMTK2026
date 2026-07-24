using System.Collections;
using UnityEngine;

public class StageClock : MonoBehaviour
{
    [SerializeField] private GameObject secondHand;
    private Rigidbody2D secondHandRB;
    [SerializeField] private GameObject minuteHand;
    private Rigidbody2D minuteHandRB;

    [SerializeField] float timeScale;
    [SerializeField] private float totalTimer;
    [SerializeField] public float timerLeft;

    [SerializeField] private float currentSecSpeed;
    [SerializeField] private float currentMinSpeed;
    [SerializeField] private float defaultHandSpeed;
    [SerializeField] private float maxHandSpeed;

    [SerializeField] private bool secondHandRotatingClockwise;
    [SerializeField] private bool minuteHandRotatingClockwise;

    //public static Vector2 VelocityVec;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        timerLeft = totalTimer;
        currentSecSpeed = defaultHandSpeed;
        currentMinSpeed = defaultHandSpeed;

        secondHandRB = secondHand.GetComponent<Rigidbody2D>();
        minuteHandRB = minuteHand.GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void FixedUpdate()
    {
        secondHandRB.AddTorque(currentSecSpeed * timeScale, ForceMode2D.Force);
        minuteHandRB.AddTorque(currentMinSpeed * timeScale, ForceMode2D.Force);

        secondHandRB.angularVelocity = Mathf.Clamp(secondHandRB.angularVelocity, 0, maxHandSpeed);
        minuteHandRB.angularVelocity = Mathf.Clamp(minuteHandRB.angularVelocity, 0, maxHandSpeed);

    }

    public bool IsTimeLeft()
    {
        return timerLeft > 0f;
    }


    public void SetTimeScale(float newTimeScale) 
    {
        timeScale = newTimeScale;
    }
}
