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
    public List<string> villainRiver;

    [Serializable]
    public class UnitPool
    {
        public GameObject unit;
        public int total;

        public UnitPool(GameObject newUnit, int newTotal)
        {
            unit = newUnit;
            total = newTotal;
        }
    }
    public UnitPool[] villainPool;  // Unity can't expose dictionaries in the inspector, so Dictionary<string, GameObject[]> actionProficiencies not possible without addon

    private int currentRound = 1;
    private int reinforcementPoints = 5;


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

    // The flow: Player uses UI to end turn -> EndHeroTurn() -> StartVillainTurn() -> StartHeroTurn() -> Player takes their next turn
    public void EndHeroTurn()
    {
        // Dredge the river, removing any tiles with 0 units on the map
        foreach (string unitTag in new List<string>(villainRiver))
        {
            if (unitTag != "REINFORCEMENT" && GameObject.FindGameObjectsWithTag(unitTag).Length == 0)
            {
                Debug.Log("EndHeroTurn() DREDGING " + unitTag);
                villainRiver.Remove(unitTag);
            }
        }

        StartVillainTurn();
    }

    public void StartVillainTurn()
    {
        for (int i = 0; i < 2; i++)
        {
            string unitTypeToActivate = GetVillainTileToActivate(i);
            if (unitTypeToActivate == "REINFORCEMENT")
            {
                CallReinforcements();
            }
            else
            {
                foreach (GameObject unit in GameObject.FindGameObjectsWithTag(unitTypeToActivate))
                {
                    unit.GetComponent<Unit>().ActivateUnit();
                }
            }

            villainRiver.Remove(unitTypeToActivate);
            villainRiver.Add(unitTypeToActivate);
        }
        // // Below useful for debugging single unit
        //int i = 0;
        //foreach (GameObject unit in GameObject.FindGameObjectsWithTag("SHOTGUN"))
        //{
        //    //i++;
        //    //if (i < 4)
        //    //{
        //    //    continue;
        //    //}
        //    unit.GetComponent<Unit>().ActivateUnit();
        //    //break;
        //}

        StartHeroTurn();
    }

    string GetVillainTileToActivate(int unitsAlreadyActivated)
    {
        //string debugVillainRiverString = "GetVillainTileToActivate  villainRiver: ";
        //foreach (string unitTag in villainRiver)
        //{
        //    debugVillainRiverString += " " + unitTag + " ";
        //}
        //Debug.Log(debugVillainRiverString);
        double totalWeightOfMostValuableUnitTurn = 0;
        string mostValuableUnitType = null;
        string debugVillainTilesString = "GetVillainTileToActivate() each unit weight {  ";
        for (int j = 0; j < 3; j++)  // Only first 3 tiles compared
        {
            string unitTag = villainRiver[j];
            double currentWeightOfUnitTurn = 0;

            if (unitTag == "REINFORCEMENT")
            {
                if (unitsAlreadyActivated == 0)  // Only reinforce if first unit activation
                {
                    List<Tuple<UnitPool, double, GameObject>> reinforcementsAvailable = GetAvailableReinforcements();
                    int reinforcementPointsRemaining = reinforcementPoints;
                    foreach (Tuple<UnitPool, double, GameObject> unitPool in reinforcementsAvailable)
                    {
                        Unit unitInfo = unitPool.Item1.unit.GetComponent<Unit>();
                        int numToReinforce = (int)Math.Floor((double)reinforcementPointsRemaining / (double)unitInfo.reinforcementCost);
                        if (numToReinforce > unitPool.Item1.total)
                        {
                            numToReinforce = unitPool.Item1.total;
                        }
                        reinforcementPointsRemaining -= numToReinforce * unitInfo.reinforcementCost;
                        currentWeightOfUnitTurn += unitPool.Item2 * numToReinforce;
                        if (reinforcementPointsRemaining == 0)
                        {
                            break;
                        }
                    }
                    GameObject superBarn = GameObject.FindGameObjectWithTag("SUPERBARN");
                    if (superBarn != null)
                    {
                        currentWeightOfUnitTurn += superBarn.GetComponent<Unit>().GetMostValuableActionWeight();
                    }
                }
            }
            else
            {
                foreach (GameObject unit in GameObject.FindGameObjectsWithTag(unitTag))
                {
                    currentWeightOfUnitTurn += unit.GetComponent<Unit>().GetMostValuableActionWeight();
                }
            }

            if (currentWeightOfUnitTurn > totalWeightOfMostValuableUnitTurn)
            {
                totalWeightOfMostValuableUnitTurn = currentWeightOfUnitTurn;
                mostValuableUnitType = unitTag;
            }
            debugVillainTilesString += unitTag + ": " + currentWeightOfUnitTurn.ToString() + "  ";
        }
        Debug.Log(debugVillainTilesString + "}");

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
        List<Tuple<UnitPool, double, GameObject>> reinforcementsAvailable = GetAvailableReinforcements();
        int reinforcementPointsRemaining = reinforcementPoints;
        int i = 0;
        while (reinforcementPoints > 0 && i < reinforcementsAvailable.Count)
        {
            Unit unitInfo = reinforcementsAvailable[i].Item1.unit.GetComponent<Unit>();
            int numToReinforce = (int)Math.Floor((double)reinforcementPointsRemaining / (double)unitInfo.reinforcementCost);
            if (numToReinforce > reinforcementsAvailable[i].Item1.total)
            {
                numToReinforce = reinforcementsAvailable[i].Item1.total;
            }
            for (int j = 0; j < numToReinforce; j++)
            {
                Instantiate(reinforcementsAvailable[i].Item1.unit, reinforcementsAvailable[i].Item3.transform);  // spawn Unit
                Debug.Log("CallReinforcements() spawning " + reinforcementsAvailable[i].Item1.unit.tag + " at " + reinforcementsAvailable[i].Item3.name);
            }
            reinforcementPointsRemaining -= numToReinforce * unitInfo.reinforcementCost;
            i++;
        }
        GameObject superBarn = GameObject.FindGameObjectWithTag("SUPERBARN");
        if (superBarn != null)
        {
            Unit superBarnInfo = superBarn.GetComponent<Unit>();
            superBarnInfo.ActivateUnit();
            superBarnInfo.lifePoints -= 2;
            if (superBarnInfo.lifePoints < 1)
            {
                Destroy(superBarn);
            }
        }
    }

    List<Tuple<UnitPool, double, GameObject>> GetAvailableReinforcements()
    {
        List<Tuple<UnitPool, double, GameObject>> reinforcementsAvailable = new List<Tuple<UnitPool, double, GameObject>>();
        //string villainPoolDebugString = "GetAvailableReinforcements() reinforcementPoints: " + reinforcementPoints.ToString() + "villainPool: {";
        foreach (UnitPool unitPool in villainPool)
        {
            if (villainRiver.Contains(unitPool.unit.tag))  // If unit tile has not been dredged from villainRiver
            {
                //villainPoolDebugString += " " + unitPool.unit.tag + " onMap:" + GameObject.FindGameObjectsWithTag(unitPool.unit.tag).Length.ToString() + " of total:" + unitPool.total.ToString();
                int inReserve = unitPool.total - GameObject.FindGameObjectsWithTag(unitPool.unit.tag).Length;
                if (inReserve > 0)
                {
                    double highestSpawnActionWeight = 0;
                    GameObject chosenSpawnZone = null;
                    Boolean activateableThisTurn = false;
                    for (int i = 0; i < 4; i++)
                    {
                        if (villainRiver[i] != "REINFORCEMENT" && unitPool.unit.CompareTag(villainRiver[i]))
                        {
                            activateableThisTurn = true;
                            break;
                        }
                    }
                    foreach (GameObject spawnZone in spawnZones)
                    {
                        GameObject tempUnit = Instantiate(unitPool.unit, spawnZone.transform);  // TODO maybe make this invisible in case it flickers
                        double currentSpawnActionWeight = tempUnit.GetComponent<Unit>().GetMostValuableActionWeight();
                        DestroyImmediate(tempUnit);
                        if (currentSpawnActionWeight > highestSpawnActionWeight)
                        {
                            chosenSpawnZone = spawnZone;
                            highestSpawnActionWeight = currentSpawnActionWeight;
                        }
                    }
                    if (!activateableThisTurn)
                    {
                        highestSpawnActionWeight *= .9;  // Only 90% as valuable if Unit can't be activated this turn
                    }
                    reinforcementsAvailable.Add(new Tuple<UnitPool, double, GameObject>(new UnitPool(unitPool.unit, inReserve), highestSpawnActionWeight, chosenSpawnZone));
                }
            }
        }
        //Debug.Log(villainPoolDebugString);
        reinforcementsAvailable.Sort((x, y) => x.Item2.CompareTo(y.Item2));  // Hopefully this sorts the list by the most valuable weight. https://stackoverflow.com/questions/4668525/sort-listtupleint-int-in-place

        string reinforcementsAvailableDebugString = "GetAvailableReinforcements() reinforcementsAvailable: [";
        foreach (Tuple<UnitPool, double, GameObject> reinforcement in reinforcementsAvailable)
        {
            reinforcementsAvailableDebugString += " { " + reinforcement.Item1.total + " of " + reinforcement.Item1.unit.tag + " with weight " + reinforcement.Item2 + " at spawn " + reinforcement.Item3 + "} ";
        }
        Debug.Log(reinforcementsAvailableDebugString + "]");

        return reinforcementsAvailable;
    }

    IEnumerator TurnClockHand(float currentAngle, float newAngle)
    {
        float t = 0;
        float uncoverTime = 2.0f;

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
