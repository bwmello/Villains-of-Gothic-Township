using System;  // for Math.abs()
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScenarioMap : MonoBehaviour
{
    readonly System.Random random = new System.Random();

    [SerializeField]
    GameObject clockHand;
    [SerializeField]
    List<GameObject> spawnZones;

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

    public List<string> villainRiver = new List<string>() { "UZI", "CHAINS", "PISTOLS", /*"REINFORCEMENT",*/ "CROWBAR", "SHOTGUN", "BARN" };
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
            string villainRiverString = "";
            foreach (string unitType in villainRiver)
            {
                villainRiverString += unitType + ", ";
            }
            Debug.Log(villainRiverString);

            for (int j = 0; j < 3; j++)
            {
                string unitTag = villainRiver[j];
                foreach (GameObject unit in GameObject.FindGameObjectsWithTag(unitTag))
                {
                    Unit unitInfo = unit.GetComponent<Unit>();
                    unitInfo.TakeUnitTurn();
                }
                villainRiver.Remove(unitTag);
                villainRiver.Add(unitTag);
                break;  // TODO Compare and pick most effective unit for turn instead of just the first tile
            }
        }

        StartHeroTurn();
    }

    void CallReinforcements()
    {
        // TODO implement after the worthiness of a turn can be evaluated
    }
}
