using System.Collections;
using UnityEngine;

public class StageClock : MonoBehaviour
{
    [SerializeField] private GameObject secondHand;
    [SerializeField] private GameObject minuteHand;

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
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void FixedUpdate()
    {

    }

    public bool IsTimeLeft()
    {
        return timerLeft > 0f;
    }

    public void RotateSecondsHand() 
    {
    
    }

    public void RotateMinutesHand()
    {

    }
}
