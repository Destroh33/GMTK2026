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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        timerLeft = totalTimer;
        currentSecSpeed = defaultHandSpeed;
        currentMinSpeed = defaultHandSpeed;

        secondHandRB = secondHand.GetComponent<Rigidbody2D>();
        minuteHandRB = minuteHand.GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        int dirSec = secondHandRotatingClockwise ? 1 : -1;
        int dirMin = minuteHandRotatingClockwise ? 1 : -1;

        secondHandRB.AddTorque(currentSecSpeed * timeScale * dirSec, ForceMode2D.Force);
        minuteHandRB.AddTorque(currentMinSpeed * timeScale * dirMin, ForceMode2D.Force);

        secondHandRB.angularVelocity = Mathf.Clamp(secondHandRB.angularVelocity, -maxHandSpeed, maxHandSpeed);
        minuteHandRB.angularVelocity = Mathf.Clamp(minuteHandRB.angularVelocity, -maxHandSpeed, maxHandSpeed);

        if (dirSec > 0)
        {
            timerLeft -= Time.fixedDeltaTime;
        }
        else 
        {
            timerLeft += Time.fixedDeltaTime;
        }

        if (dirMin > 0)
        {
            //do nothing
        }
        else 
        {
            timerLeft += Time.fixedDeltaTime * 60f; //tune this?
        }

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
