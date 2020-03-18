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
        { "MANIPULATION", 70 },
        { "THOUGHT", 60 },
        { "OBJECTIVE_REROLL", 7 },  // * totalRerolls
        { "OBJECTIVE_HINDRANCE", -15 },  // * totalHindrance
        { "MELEE", 40 },
        { "RANGED", 40 },
        { "ATTACK_BONUSDIE", 10 },  // * totalBonusDice
        { "ATTACK_REROLL", 5 },  // * totalRerolls
        { "ATTACK_HINDRANCE", -9 },  // * totalHindrance
        { "GUARD_PRIMEDBOMB", 15 },  // // TODO triple this if there are no more bombs and only 2 primedbombs left
        { "GUARD_BOMB", 10 },
        { "GUARD_COMPUTER", 5 }
    };
    public class UnitPossibleAction
    {
        public Unit myUnit;
        public string actionType;
        public double actionWeight;
        public GameObject destinationZone;
        public GameObject targetedZone;

        public UnitPossibleAction(Unit theUnit, string unitActionType, double unitActionWeight, GameObject unitDestinationZone, GameObject unitTargetedZone = null)
        {
            myUnit = theUnit;
            actionType = unitActionType;
            actionWeight = unitActionWeight;
            destinationZone = unitDestinationZone;
            targetedZone = unitTargetedZone;
        }
    }

    void Start()
    {
        validActionProficiencies = GetValidActionProficiencies();
    }

    public void ActivateUnit()
    {
        GameObject currentZone = transform.parent.gameObject;

        Dictionary<GameObject, int> possibleDestinations = GetPossibleDestinations(currentZone, 0);
        List<ZoneInfo> possibleDestinationsInfo = new List<ZoneInfo>();
        foreach (GameObject destination in possibleDestinations.Keys)
        {
            possibleDestinationsInfo.Add(destination.GetComponent<ZoneInfo>());
        }

        List<UnitPossibleAction> allPossibleUnitActions = GetPossibleActions(possibleDestinationsInfo);
        if (allPossibleUnitActions != null && allPossibleUnitActions.Count > 0)
        {
            Dictionary<string, UnitPossibleAction> uniquePossibleActions = new Dictionary<string, UnitPossibleAction>();
            UnitPossibleAction chosenAction = null;

            foreach (UnitPossibleAction unitAction in allPossibleUnitActions)
            {
                if (chosenAction == null || unitAction.actionWeight > chosenAction.actionWeight)
                {
                    chosenAction = unitAction;
                }
            }
            MoveToken(currentZone, chosenAction.destinationZone);
            PerformAction(chosenAction);
        }
        else
        {
            // TODO Make a function which takes possibleDestinations as a parameter and generates the possibleDestinations beyond that.
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

            int sizeCost = currentZoneInfo.GetCurrentHindrance(transform.gameObject, true);
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
        //string possibleDestinationsDebugString = name;
        //foreach (KeyValuePair<GameObject, int> pair in possibleDestinations)
        //{
        //    possibleDestinationsDebugString += "  ZONE: " + pair.Key.name + " - " + pair.Value.ToString();
        //    //Debug.Log(pair.Key.name + pair.Value.ToString());
        //}
        return possibleDestinations;
    }

    private List<UnitPossibleAction> GetPossibleActions(List<ZoneInfo> possibleDestinations)
    {
        List<UnitPossibleAction> allPossibleActions = new List<UnitPossibleAction>();

        foreach (ZoneInfo destination in possibleDestinations)
        {
            int destinationHindrance = destination.GetCurrentHindrance(this.gameObject);
            foreach (string actionType in validActionProficiencies.Keys)
            {
                double actionWeight = 0;

                if (destination.HasToken("PrimedBomb"))
                {
                    actionWeight += actionsWeightTable["GUARD_PRIMEDBOMB"];
                }
                if (destination.HasToken("Bomb"))
                {
                    actionWeight += actionsWeightTable["GUARD_BOMB"];
                }
                if (destination.HasToken("Computer"))
                {
                    actionWeight += actionsWeightTable["GUARD_COMPUTER"];
                }

                switch (actionType)
                {
                    case "MANIPULATION":
                        if (destination.HasToken("Bomb"))
                        {
                            actionWeight += actionsWeightTable[actionType];
                            actionWeight += destination.supportRerolls * actionsWeightTable["OBJECTIVE_REROLL"];
                            actionWeight += destinationHindrance * actionsWeightTable["OBJECTIVE_HINDRANCE"];
                            allPossibleActions.Add(new UnitPossibleAction(this, actionType, actionWeight, destination.gameObject));
                        }
                        break;
                    case "THOUGHT":
                        if (destination.HasToken("Computer"))
                        {
                            actionWeight += actionsWeightTable[actionType];
                            actionWeight += destination.supportRerolls * actionsWeightTable["OBJECTIVE_REROLL"];
                            actionWeight += destinationHindrance * actionsWeightTable["OBJECTIVE_HINDRANCE"];
                            allPossibleActions.Add(new UnitPossibleAction(this, actionType, actionWeight, destination.gameObject));
                        }
                        break;
                    case "MELEE":
                        if (destination.HasHeroes())
                        {
                            actionWeight += actionsWeightTable[actionType];
                            actionWeight += destination.supportRerolls * actionsWeightTable["ATTACK_REROLL"];
                            allPossibleActions.Add(new UnitPossibleAction(this, actionType, actionWeight, destination.gameObject));
                        }
                        break;
                    case "RANGED":
                        GameObject targetedZone = destination.GetLineOfSightZoneWithHero();
                        if (targetedZone != null)
                        {
                            actionWeight += actionsWeightTable[actionType];
                            actionWeight += destination.supportRerolls * actionsWeightTable["ATTACK_REROLL"];
                            if (pointBlankRerolls > 0 && targetedZone == destination.gameObject)
                            {
                                actionWeight += pointBlankRerolls * actionsWeightTable["ATTACK_REROLL"];
                            }
                            actionWeight += destinationHindrance * actionsWeightTable["ATTACK_HINDRANCE"];
                            if (destination.elevation > targetedZone.GetComponent<ZoneInfo>().elevation)
                            {
                                actionWeight += actionsWeightTable["ATTACK_BONUSDIE"];
                            }
                            allPossibleActions.Add(new UnitPossibleAction(this, actionType, actionWeight, destination.gameObject, targetedZone));
                        }
                        break;
                }
            }
        }

        return allPossibleActions;
    }

    public double GetMostValuableActionWeight()
    {
        double mostValuableActionWeight = 0;
        GameObject currentZone = transform.parent.gameObject;

        Dictionary<GameObject, int> possibleDestinations = GetPossibleDestinations(currentZone, 0);
        List<ZoneInfo> possibleDestinationsInfo = new List<ZoneInfo>();
        foreach (GameObject destination in possibleDestinations.Keys)
        {
            possibleDestinationsInfo.Add(destination.GetComponent<ZoneInfo>());
        }

        List<UnitPossibleAction> allPossibleUnitActions = GetPossibleActions(possibleDestinationsInfo);
        if (allPossibleUnitActions != null && allPossibleUnitActions.Count > 0)
        {
            Dictionary<string, UnitPossibleAction> uniquePossibleActions = new Dictionary<string, UnitPossibleAction>();

            foreach (UnitPossibleAction unitAction in allPossibleUnitActions)
            {
                if (unitAction.actionWeight > mostValuableActionWeight)
                {
                    mostValuableActionWeight = unitAction.actionWeight;
                }
            }
        }
        return mostValuableActionWeight;
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

    public void MoveToken(GameObject origin, GameObject destination)  // TODO Add wall break movement
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

    public void PerformAction(UnitPossibleAction unitTurn)
    {
        int actionSuccesses = 0;
        int requiredSuccesses;
        GameObject currentZone = transform.parent.gameObject;
        ZoneInfo currentZoneInfo = currentZone.GetComponent<ZoneInfo>();
        int rerolls = currentZoneInfo.supportRerolls - supportRerolls;

        MoveToken(transform.parent.gameObject, unitTurn.destinationZone);

        switch (unitTurn.actionType)
        {
            case "MANIPULATION":
                requiredSuccesses = 3;
                actionSuccesses += munitionSpecialist;
                actionSuccesses = RollAndReroll(validActionProficiencies[unitTurn.actionType], actionSuccesses, rerolls, requiredSuccesses);
                actionSuccesses -= currentZoneInfo.GetCurrentHindrance(transform.gameObject);
                break;

            case "THOUGHT":
                requiredSuccesses = 3;
                actionSuccesses = RollAndReroll(validActionProficiencies[unitTurn.actionType], actionSuccesses, rerolls, requiredSuccesses);
                actionSuccesses -= currentZoneInfo.GetCurrentHindrance(transform.gameObject);
                break;

            case "MELEE":
                actionSuccesses = RollAndReroll(validActionProficiencies[unitTurn.actionType], actionSuccesses, rerolls);
                if (actionSuccesses > 0)
                {
                    actionSuccesses += martialArtsSuccesses;
                }
                break;

            case "RANGED":
                if (unitTurn.targetedZone != null)
                {
                    GameObject[] dicePool = validActionProficiencies[unitTurn.actionType];
                    ZoneInfo targetedLineOfSightZoneInfo = unitTurn.targetedZone.GetComponent<ZoneInfo>();

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
                    actionSuccesses -= currentZoneInfo.GetCurrentHindrance(transform.gameObject);
                }
                else
                {
                    Debug.LogError("ERROR! RANGED action was performed while targetedLineOfSightZone was null, so henchman just wasted its action wildly firing its gun into the air.");
                }
                break;
        }
        Debug.Log("Moved " + tag + " from " + currentZone.name + " to " + unitTurn.destinationZone.name + " and performed " + unitTurn.actionType + " with " + actionSuccesses.ToString() + " successes");
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
