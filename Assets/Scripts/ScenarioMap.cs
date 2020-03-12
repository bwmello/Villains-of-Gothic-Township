using System;  // for Math.abs()
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScenarioMap : MonoBehaviour
{
    readonly System.Random random = new System.Random();

    [SerializeField]
    GameObject clockHand;

    private int currentRound = 1;
    private readonly float uncoverTime = 2.0f;

    // Start is called before the first frame update
    void Start()
    {
        float startingClockHandAngle = -(currentRound * 30) + 2;
        clockHand.transform.eulerAngles = new Vector3(0, 0, startingClockHandAngle);
    }

    public void StartHeroTurn()
    {
        float currentClockHandAngle = -(currentRound * 30) + 2;
        currentRound += 1;
        float newClockHandAngle = -(currentRound * 30) + 2;
        StartCoroutine(TurnClockHand(currentClockHandAngle, newClockHandAngle));
    }

    IEnumerator TurnClockHand(float currentAngle, float newAngle)
    {
        float t = 0;

        while (t < 1f)
        {
            t += Time.deltaTime * uncoverTime;

            float angle = Mathf.LerpAngle(currentAngle, newAngle, t);
            clockHand.transform.eulerAngles = new Vector3(0, 0, angle);

            yield return null;
        }

        yield return 0;
    }

    readonly Queue<string> villainRiver = new Queue<string>(new string[] { "UZI", "CHAINS", "PISTOLS", /*"REINFORCEMENT",*/ "CROWBAR", "SHOTGUN", "BARN" });
    //private string[] villainRiver = new string[] { "UZI", "CHAIN", "PISTOLS", "REINFORCEMENT", "CROWBAR", "SHOTGUN", "BARN" };
    public void EndHeroTurn()
    {
        //List<GameObject> heroZones = new List<GameObject>();
        //List<GameObject> computerZones = new List<GameObject>();
        //List<GameObject> bombZones = new List<GameObject>();
        //List<GameObject> primedBombZones = new List<GameObject>();
        //foreach (GameObject zone in GameObject.FindGameObjectsWithTag("ZoneInfoPanel"))
        //{
        //    ZoneInfo zoneInfo = zone.GetComponent<ZoneInfo>();
        //    if (zoneInfo.HasHeroes()) { heroZones.Add(zone); }
        //    if (zoneInfo.HasToken("Computer")) { computerZones.Add(zone); }
        //    if (zoneInfo.HasToken("Bomb")) { bombZones.Add(zone); }
        //    if (zoneInfo.HasToken("PrimedBomb")) { primedBombZones.Add(zone); }
        //}

        for (int i = 0; i < 2; i++)
        {
            string unitTag = villainRiver.Dequeue();
            villainRiver.Enqueue(unitTag);
            foreach (GameObject unit in GameObject.FindGameObjectsWithTag(unitTag))
            {
                Unit unitInfo = unit.GetComponent<Unit>();
                unitInfo.TakeUnitTurn();
            }
        }

        StartHeroTurn();
    }
}
