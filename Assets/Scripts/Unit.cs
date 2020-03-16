using System;  // for [Serializable]
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;  // for dictionary.ElementAt
using TMPro;  // for TMP_Text to update the henchmen quantity from UnitRows

public class Unit : MonoBehaviour
{
    public int lifePoints = 1;
    public int defense;
    public int reinforcementCost = 1;
    public int protectedByAllies = 0;  // TODO for PISTOLS, popup prompt if removing protected Unit while they have this many allies in their zone OR auto redirect attack and alert player

    public int size = 1;
    public int menace = 1;
    public int supportRerolls = 0;  // TODO Barn has 1 here, providing 1 free reroll for each ally in zone (maybe track on ZoneInfo, updating when unit with support moved in/out

    public int movePoints;
    public int ignoreTerrainDifficulty = 0;
    public int ignoreElevation = 0;
    public int ignoreSize = 0;
    public int wallBreaker = 0;  // TODO for SuperBarn

    public int munitionSpecialist = 0;

    public int martialArtsSuccesses = 0;
    public int circularStrike = 0;  // TODO for CHAINS, if hero removed after MELEE with another hero in that zone, popup prompt saying up to this many additional successes carry over

    public int marskmanSuccesses = 0;
    public int pointBlankRerolls = 0;

    GameObject targetedLineOfSightZone;

    [Serializable]
    public class ActionProficiency
    {
        public string action;
        public GameObject[] dice;
    }
    public ActionProficiency[] actionProficiencies;  // Unity can't expose dictionaries in the inspector, so Dictionary<string, GameObject[]> actionProficiencies not possible without addon
    private Dictionary<string, GameObject[]> validActionProficiencies;

    class ActionResult
    {
        public Dice die;
        public int successes;

        public ActionResult(Dice resultDie = null, int resultSuccesses = 0)
        {
            die = resultDie;
            successes = resultSuccesses;
        }
    }

    readonly List<string> actionPriorities = new List<string>() { "MANIPULATION", "THOUGHT", "MELEE", "RANGED" };
    private Dictionary<string, double> actionsWeightTable = new Dictionary<string, double>()
    {
        { "MANIPULATE_BOMB", 70 },
        { "THOUGHT_COMPUTER", 60 },
        { "OBJECTIVE_REROLL", 7 },  // * totalRerolls
        { "OBJECTIVE_HINDRANCE", -10 },  // * totalHindrance
        { "RANGED", 40 },
        { "MELEE", 40 },
        { "ATTACK_BONUSDIE", 10 },  // * totalBonusDice
        { "ATTACK_REROLL", 5 },  // * totalRerolls
        { "ATTACK_HINDRANCE", -7 },  // * totalHindrance
        { "GUARD_PRIMEDBOMB", 15 },
        { "GUARD_BOMB", 10 },
        { "GUARD_COMPUTER", 5 }
    };

    void Start()
    {
        validActionProficiencies = GetValidActionProficiencies();
    }

    public void TakeUnitTurn()
    {
        GameObject currentZone = transform.parent.gameObject;

        Dictionary<GameObject, int> possibleDestinations = GetPossibleDestinations(currentZone, 0);
        if (possibleDestinations.Count > 0)
        {
            // // Below for debugging GetPossibleDestinations()
            //string possibleDestinationsDebugString = name;
            //foreach (KeyValuePair<GameObject, int> pair in possibleDestinations)
            //{
            //    possibleDestinationsDebugString += "  ZONE: " + pair.Key.name + " - " + pair.Value.ToString();
            //    //Debug.Log(pair.Key.name + pair.Value.ToString());
            //}

            List<ZoneInfo> possibleDestinationsInfo = new List<ZoneInfo>();
            foreach (GameObject destination in possibleDestinations.Keys)
            {
                possibleDestinationsInfo.Add(destination.GetComponent<ZoneInfo>());
            }

            ZoneInfo chosenDestination = null;
            string chosenAction = "";
            foreach (string actionType in actionPriorities)
            {
                if (validActionProficiencies.ContainsKey(actionType))
                {
                    chosenDestination = GetDestinationForAction(actionType, possibleDestinationsInfo);
                    if (chosenDestination != null)
                    {
                        chosenAction = actionType;
                        break;
                    }
                }
            }

            if (chosenDestination != null)
            {
                MoveToken(currentZone, chosenDestination.gameObject);
                int actionSuccesses = PerformAction(chosenAction);
                Debug.Log("Moved " + tag + " from " + currentZone.name + " to " + chosenDestination.name + " and performed " + chosenAction + " with " + actionSuccesses.ToString() + " successes");
            }
        }
    }

    private Dictionary<GameObject, int> GetPossibleDestinations(GameObject currentZone, int movePointsPreviouslyUsed, Dictionary<GameObject, int> possibleDestinations = null)
    {
        if (possibleDestinations is null)
        {
            possibleDestinations = new Dictionary<GameObject, int> { { currentZone, 0 } };
        }
        ZoneInfo currentZoneInfo = currentZone.GetComponent<ZoneInfo>();

        List<GameObject> allAdjacentZones = currentZoneInfo.adjacentZones;
        allAdjacentZones.AddRange(currentZoneInfo.steeplyAdjacentZones);
        foreach (GameObject potentialZone in allAdjacentZones)
        {
            ZoneInfo potentialZoneInfo = potentialZone.GetComponent<ZoneInfo>();

            if (potentialZoneInfo.GetCurrentOccupancy() >= potentialZoneInfo.maxOccupancy)
            {
                continue;  // Skip this potentialZone if potentialZone is at maxOccupancy
            }

            int terrainDifficultyCost = 0;
            if (currentZoneInfo.steeplyAdjacentZones.Contains(potentialZone))
            {
                terrainDifficultyCost = currentZoneInfo.terrainDifficulty >= ignoreTerrainDifficulty ? currentZoneInfo.terrainDifficulty - ignoreTerrainDifficulty : 0;
            }

            int elevationCost = Math.Abs(currentZoneInfo.elevation - potentialZoneInfo.elevation);
            elevationCost = elevationCost >= ignoreElevation ? elevationCost - ignoreElevation : 0;

            int sizeCost = currentZoneInfo.GetCurrentHindrance(true, transform.gameObject);
            sizeCost = sizeCost >= ignoreSize ? sizeCost - ignoreSize : 0;

            int totalMovementCost = 1 + terrainDifficultyCost + elevationCost + sizeCost + movePointsPreviouslyUsed;
            if (movePoints >= totalMovementCost)  // if unit can move here
            {
                if (possibleDestinations.ContainsKey(potentialZone))
                {
                    if (possibleDestinations[potentialZone] > totalMovementCost)
                    {
                        possibleDestinations[potentialZone] = totalMovementCost;
                        if (movePoints > totalMovementCost)
                        {
                            possibleDestinations = GetPossibleDestinations(potentialZone, totalMovementCost, possibleDestinations);
                        }
                    }
                }
                else
                {
                    possibleDestinations[potentialZone] = totalMovementCost;
                    if (movePoints > totalMovementCost)
                    {
                        possibleDestinations = GetPossibleDestinations(potentialZone, totalMovementCost, possibleDestinations);
                    }
                }
            }
        }
        return possibleDestinations;
    }

    public Dictionary<string, GameObject[]> GetValidActionProficiencies()
    {
        Dictionary<string, GameObject[]> validActionProficiencies = new Dictionary<string, GameObject[]>();
        foreach (ActionProficiency proficiency in actionProficiencies)
        {
            if (proficiency.dice.Length > 0)
            {
                validActionProficiencies.Add(proficiency.action, proficiency.dice);
            }
        }
        return validActionProficiencies;
    }

    public ZoneInfo GetDestinationForAction(string action, List<ZoneInfo> possibleDestinationsInfo)
    {
        switch (action)
        {
            case "MANIPULATION":
                foreach (ZoneInfo zone in possibleDestinationsInfo)
                {
                    if (zone.HasToken("Bomb"))
                    {
                        return zone;
                    }
                }
                break;
            case "THOUGHT":
                foreach (ZoneInfo zone in possibleDestinationsInfo)
                {
                    if (zone.HasToken("Computer"))
                    {
                        return zone;
                    }
                }
                break;
            case "MELEE":
                foreach (ZoneInfo zone in possibleDestinationsInfo)
                {
                    if (zone.HasHeroes())
                    {
                        return zone;
                    }
                }
                break;
            case "RANGED":  // TODO: Account for elevation bonuses
                foreach (ZoneInfo zone in possibleDestinationsInfo)
                {
                    foreach (GameObject losZone in zone.lineOfSightZones)
                    {
                        if (losZone.GetComponent<ZoneInfo>().HasHeroes())
                        {
                            targetedLineOfSightZone = losZone;  // Not so great way of making this available to PerformAction("RANGED")
                            return zone;
                        }
                    }
                }
                break;
        }
        return null;
    }

    public void MoveToken(GameObject origin, GameObject destination)
    {
        if (CompareTag("BARN") || CompareTag("SUPERBARN"))
        {
            foreach (Transform row in origin.transform)
            {
                if (row.CompareTag(tag))
                {
                    row.gameObject.SetActive(false);
                    origin.GetComponent<ZoneInfo>().supportRerolls -= supportRerolls;
                    break;
                }
            }
            foreach (Transform row in destination.transform)
            {
                if (row.CompareTag(tag))
                {
                    TMP_Text unitNumber = row.Find("UnitNumber").GetComponent<TMP_Text>();
                    unitNumber.text = lifePoints.ToString();
                    row.gameObject.SetActive(true);
                    origin.GetComponent<ZoneInfo>().supportRerolls += supportRerolls;
                    break;
                }
            }
        }
        else
        {
            foreach (Transform row in origin.transform)
            {
                if (row.CompareTag(tag))
                {
                    TMP_Text unitNumber = row.Find("UnitNumber").GetComponent<TMP_Text>();

                    unitNumber.text = (Int32.Parse(unitNumber.text) - 1).ToString();
                    if (unitNumber.text == "0")
                    {
                        row.gameObject.SetActive(false);
                    }
                    origin.GetComponent<ZoneInfo>().supportRerolls -= supportRerolls;
                    break;
                }
            }
            foreach (Transform row in destination.transform)
            {
                if (row.CompareTag(tag))
                {
                    TMP_Text unitNumber = row.Find("UnitNumber").GetComponent<TMP_Text>();
                    unitNumber.text = (Int32.Parse(unitNumber.text) + 1).ToString();
                    row.gameObject.SetActive(true);
                    origin.GetComponent<ZoneInfo>().supportRerolls += supportRerolls;
                    break;
                }
            }
        }
    }

    private int PerformAction(string action)
    {
        int actionSuccesses = 0;
        int requiredSuccesses;
        GameObject currentZone = transform.parent.gameObject;
        ZoneInfo currentZoneInfo = currentZone.GetComponent<ZoneInfo>();
        int rerolls = currentZoneInfo.supportRerolls - supportRerolls;

        switch (action)
        {
            case "MANIPULATION":
                requiredSuccesses = 3;
                actionSuccesses += munitionSpecialist;
                actionSuccesses = RollAndReroll(validActionProficiencies[action], actionSuccesses, rerolls, requiredSuccesses);
                actionSuccesses -= currentZoneInfo.GetCurrentHindrance(false, transform.gameObject);
                break;

            case "THOUGHT":
                requiredSuccesses = 3;
                actionSuccesses = RollAndReroll(validActionProficiencies[action], actionSuccesses, rerolls, requiredSuccesses);
                actionSuccesses -= currentZoneInfo.GetCurrentHindrance(false, transform.gameObject);
                break;

            case "MELEE":
                actionSuccesses = RollAndReroll(validActionProficiencies[action], actionSuccesses, rerolls);
                if (actionSuccesses > 0)
                {
                    actionSuccesses += martialArtsSuccesses;
                }
                break;

            case "RANGED":  // TODO: Account for elevation bonuses and hindrance
                if  (targetedLineOfSightZone != null)
                {
                    GameObject[] dicePool = validActionProficiencies[action];
                    ZoneInfo targetedLineOfSightZoneInfo = targetedLineOfSightZone.GetComponent<ZoneInfo>();

                    if (currentZoneInfo.elevation > targetedLineOfSightZoneInfo.elevation)
                    {
                        List<GameObject> tempDicePool = new List<GameObject>();
                        tempDicePool.AddRange(dicePool);
                        tempDicePool.Add(currentZoneInfo.elevationDie);
                        dicePool = tempDicePool.ToArray();
                    }

                    if (currentZone == targetedLineOfSightZone)
                    {
                        rerolls += pointBlankRerolls;
                    }

                    actionSuccesses = RollAndReroll(dicePool, actionSuccesses, rerolls);
                    if (actionSuccesses > 0)
                    {
                        actionSuccesses += marskmanSuccesses;
                    }
                    actionSuccesses -= currentZoneInfo.GetCurrentHindrance(false, transform.gameObject);
                }
                else
                {
                    Debug.LogError("ERROR! RANGED action was performed while targetedLineOfSightZone was null, so henchman just wasted its action wildly firing its gun into the air.");
                }
                break;
        }
        return actionSuccesses;
    }

    private int RollAndReroll(GameObject[] dicePool, int actionSuccesses, int rerolls, int requiredSuccesses)
    {
        List<ActionResult> currentActionResults = new List<ActionResult>();

        foreach (GameObject die in dicePool)
        {
            Dice dieInfo = die.GetComponent<Dice>();
            ActionResult currentActionResult = new ActionResult(dieInfo, dieInfo.Roll());
            actionSuccesses += currentActionResult.successes;
            currentActionResults.Add(currentActionResult);
        }

        while (actionSuccesses < requiredSuccesses)
        {
            ActionResult mostDisappointingResult = new ActionResult();
            double averageVsResultDifference = 10;  // Any suitably high number (over 3) works
            int mostDisappointingResultIndex = 0;
            int counter = 0;

            foreach (ActionResult myActionResult in currentActionResults)
            {
                if (myActionResult.die.rerollable || rerolls > 0)
                {
                    double myAverageVsResultDifference = myActionResult.die.averageSuccesses - myActionResult.successes;
                    if (myAverageVsResultDifference < averageVsResultDifference)
                    {
                        averageVsResultDifference = myAverageVsResultDifference;
                        mostDisappointingResult = myActionResult;
                        mostDisappointingResultIndex = counter;
                    }
                }
                counter++;
            }

            if (mostDisappointingResult.die == null)
            {
                break;
            }
            else
            {
                if (mostDisappointingResult.die.rerollable)
                {
                    mostDisappointingResult.die.rerollable = false;
                }
                else
                {
                    rerolls--;
                }
                int rerolledDieSuccesses = mostDisappointingResult.die.Roll();
                actionSuccesses = actionSuccesses - mostDisappointingResult.successes + rerolledDieSuccesses;
                currentActionResults.RemoveAt(mostDisappointingResultIndex);
                currentActionResults.Insert(mostDisappointingResultIndex, new ActionResult(mostDisappointingResult.die, rerolledDieSuccesses));
            }
        }

        return actionSuccesses;
    }

    private int RollAndReroll(GameObject[] dicePool, int actionSuccesses, int rerolls)
    {
        List<ActionResult> currentActionResults = new List<ActionResult>();

        foreach (GameObject die in dicePool)
        {
            Dice dieInfo = die.GetComponent<Dice>();
            int currentRollSuccesses = dieInfo.Roll();
            if (dieInfo.rerollable && currentRollSuccesses < dieInfo.averageSuccesses)
            {
                currentRollSuccesses = dieInfo.Roll();
            }
            ActionResult currentActionResult = new ActionResult(dieInfo, currentRollSuccesses);
            actionSuccesses += currentActionResult.successes;
            currentActionResults.Add(currentActionResult);
        }

        while (rerolls > 0)
        {
            ActionResult mostDisappointingResult = new ActionResult();
            double averageVsResultDifference = 10;  // Any suitably high number (over 3) works
            int mostBelowAverageResultIndex = 0;
            int counter = 0;

            foreach (ActionResult myActionResult in currentActionResults)
            {
                double myAverageVsResultDifference = myActionResult.die.averageSuccesses - myActionResult.successes;
                if (myAverageVsResultDifference < 0 && myAverageVsResultDifference < averageVsResultDifference)  // Don't keep rerolling the same die if you already have average or above successes.
                {
                    averageVsResultDifference = myAverageVsResultDifference;
                    mostDisappointingResult = myActionResult;
                    mostBelowAverageResultIndex = counter;
                }
                counter++;
            }

            if (mostDisappointingResult.die == null)
            {
                break;
            }
            else
            {
                if (mostDisappointingResult.die.rerollable)
                {
                    mostDisappointingResult.die.rerollable = false;
                }
                else
                {
                    rerolls--;
                }
                int rerolledDieSuccesses = mostDisappointingResult.die.Roll();
                actionSuccesses = actionSuccesses - mostDisappointingResult.successes + rerolledDieSuccesses;
                currentActionResults.RemoveAt(mostBelowAverageResultIndex);
                currentActionResults.Insert(mostBelowAverageResultIndex, new ActionResult(mostDisappointingResult.die, rerolledDieSuccesses));
            }
        }

        return actionSuccesses;
    }
}
