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
        for (int i = 0; i < 2; i++)
        {
            string unitTypeToActivate = GetVillainTileToActivate();
            foreach (GameObject unit in GameObject.FindGameObjectsWithTag(unitTypeToActivate))
            {
                unit.GetComponent<Unit>().ActivateUnit();
            }

            villainRiver.Remove(unitTypeToActivate);
            villainRiver.Add(unitTypeToActivate);
        }
        //// Below useful for debugging single unit
        //int i = 0;
        //foreach (GameObject unit in GameObject.FindGameObjectsWithTag("CROWBAR"))
        //{
        //    i++;
        //    if (i == 1)
        //    {
        //        continue;
        //    }
        //    unit.GetComponent<Unit>().ActivateUnit();
        //    break;
        //}

        StartHeroTurn();
    }

    string GetVillainTileToActivate()
    {
        double totalWeightOfMostValuableUnitTurn = 0;
        string mostValuableUnitType = null;
        for (int j = 0; j < 3; j++)  // Only first 3 tiles compared
        {
            string unitTag = villainRiver[j];
            double currentWeightOfUnitTurn = 0;
            foreach (GameObject unit in GameObject.FindGameObjectsWithTag(unitTag))
            {
                currentWeightOfUnitTurn += unit.GetComponent<Unit>().GetMostValuableActionWeight();
            }
            if (currentWeightOfUnitTurn > totalWeightOfMostValuableUnitTurn)
            {
                totalWeightOfMostValuableUnitTurn = currentWeightOfUnitTurn;
                mostValuableUnitType = unitTag;
            }
            Debug.Log("GetVillainTileToActivate(): Weight of " + unitTag + ": " + currentWeightOfUnitTurn.ToString());
        }
        if (mostValuableUnitType == null)
        {
            string villainRiverString = "";
            foreach (string unitType in villainRiver)
            {
                villainRiverString += unitType + ", ";
            }
            Debug.LogError("ERROR! ScenarioMap.GetVillainTileToActivate() returned null. Are there no tiles in the river to activate? villainRiver: " + villainRiverString);
        }
        return mostValuableUnitType;
    }

    void CallReinforcements()
    {
        // TODO implement after the worthiness of a turn can be evaluated
    }
}
