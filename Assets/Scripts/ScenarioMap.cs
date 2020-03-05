using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScenarioMap : MonoBehaviour
{
    [SerializeField]
    GameObject clockHand;

    private int currentRound = 0;
    private readonly float uncoverTime = 3.0f;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void StartHeroTurn()
    {
        float currentClockHandAngle = -(currentRound * 30) + 2;
        currentRound += 1;
        float newClockHandAngle = -(currentRound * 30) + 2;
        StartCoroutine(TurnClockHand(currentClockHandAngle, newClockHandAngle));
        //clockHand.transform.eulerAngles = new Vector3(0, 0, newClockHandAngle);
    }

    IEnumerator TurnClockHand(float currentAngle, float newAngle)
    {
        float t = 0;
        var uncoverTime = 2;

        while (t < 1f)
        {
            t += Time.deltaTime * uncoverTime;

            float angle = Mathf.LerpAngle(currentAngle, newAngle, t);
            clockHand.transform.eulerAngles = new Vector3(0, 0, angle);

            yield return null;
        }

        yield return 0;
    }
}
