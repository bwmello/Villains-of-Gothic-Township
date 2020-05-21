using System;  // for [Serializable]
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;  // for TMP_Text to update the henchmen quantity from UnitRows

public class Unit : MonoBehaviour
{
    public int lifePoints = 1;
    public int lifePointsMax = 1;
    public int defense;
    public int reinforcementCost = 1;
    public int protectedByAllies = 0;  // TODO for PISTOLS, popup prompt if removing protected Unit while they have this many allies in their zone OR auto redirect attack and alert player

    public int size = 1;
    public int menace = 1;
    public int supportRerolls = 0;

    public int movePoints;
    public int ignoreTerrainDifficulty = 0;
    public int ignoreElevation = 0;
    public int ignoreSize = 0;
    public int wallBreaker = 0;

    public int munitionSpecialist = 0;

    public int martialArtsSuccesses = 0;
    public int circularStrike = 0;  // TODO for CHAINS, if hero removed after MELEE with another hero in that zone, popup prompt saying up to this many additional successes carry over
    public int counterAttack = 0;  // TODO for SHOTGUN, if hero moves into space with SHOTGUN, reminder that after melee attack against SHOTGUN is resolved, SHOTGUN gets free melee attack vs hero with number of yellow dice = counterattack

    public int marskmanSuccesses = 0;
    public int pointBlankRerolls = 0;

    [Serializable]
    public class ActionProficiency
    {
        public string action;
        public GameObject[] dice;
    }
    public ActionProficiency[] actionProficiencies;  // Unity can't expose dictionaries in the inspector, so Dictionary<string, GameObject[]> actionProficiencies not possible without addon
    public Dictionary<string, GameObject[]> validActionProficiencies;


    void Awake()  // Need to happen on Instantiate for potential spawn/reinforcement evaluation, so Start() not good enough
    {
        transform.name = transform.tag;  // Needed so that when Instantiated is named UZI or CHAINS instead of UZI(Clone) or CHAINS(Clone)
        validActionProficiencies = GetValidActionProficiencies();
    }

    GameObject GetZone()
    {
        return transform.parent.parent.parent.gameObject;  // Grabs ZoneInfoPanel instead of UnitsContainer. If changes in future, only need to change this function.
    }

    public IEnumerator ActivateUnit()
    {
        GameObject currentZone = GetZone();
        Dictionary<GameObject, MovementPath> possibleDestinations = GetPossibleDestinations(currentZone);
        List<UnitPossibleAction> allPossibleUnitActions = GetPossibleActions(possibleDestinations);

        if (allPossibleUnitActions != null && allPossibleUnitActions.Count > 0)
        {
            UnitPossibleAction chosenAction = null;

            foreach (UnitPossibleAction unitAction in allPossibleUnitActions)
            {
                if (chosenAction == null || unitAction.actionWeight > chosenAction.actionWeight)
                {
                    chosenAction = unitAction;
                }
            }
            if (currentZone != chosenAction.destinationZone)
            {
                chosenAction.pathTaken.zones.Add(chosenAction.destinationZone);  // Otherwise token is never animated moving the last zone to the destination
                yield return StartCoroutine(MoveToken(chosenAction.pathTaken));
            }
            yield return StartCoroutine(PerformAction(chosenAction));
        }
        else
        {
            GameObject destinationZone = GetPartialMoveAndWeight(possibleDestinations).Item1;
            if (destinationZone != null && currentZone != destinationZone)
            {
                possibleDestinations[destinationZone].zones.Add(destinationZone);  // Otherwise token is never animated moving the last zone to the destination
                yield return StartCoroutine(MoveToken(possibleDestinations[destinationZone]));
            }
        }
        yield return 0;
    }


    public class MovementPath
    {
        public List<GameObject> zones = new List<GameObject>();
        public int movementSpent = 0;
    }

    private Dictionary<GameObject, MovementPath> GetPossibleDestinations(GameObject currentZone, Dictionary<GameObject, MovementPath> possibleDestinations = null, HashSet<GameObject> alreadyPossibleZones = null)
    {
        if (possibleDestinations is null)
        {
            possibleDestinations = new Dictionary<GameObject, MovementPath> { { currentZone, new MovementPath() } };
        }

        ZoneInfo currentZoneInfo = currentZone.GetComponent<ZoneInfo>();
        List<GameObject> allAdjacentZones = new List<GameObject>(currentZoneInfo.adjacentZones);
        allAdjacentZones.AddRange(currentZoneInfo.steeplyAdjacentZones);
        if (wallBreaker > 0)
        {
            allAdjacentZones.AddRange(currentZoneInfo.wall1AdjacentZones);
            if (wallBreaker > 1)
            {
                allAdjacentZones.AddRange(currentZoneInfo.wall2AdjacentZones);
                if (wallBreaker > 2)
                {
                    allAdjacentZones.AddRange(currentZoneInfo.wall3AdjacentZones);
                    if (wallBreaker > 3)
                    {
                        allAdjacentZones.AddRange(currentZoneInfo.wall4AdjacentZones);
                        if (wallBreaker > 4)
                        {
                            allAdjacentZones.AddRange(currentZoneInfo.wall5AdjacentZones);
                        }
                    }
                }
            }
        }

        if (alreadyPossibleZones != null)
        {
            foreach (GameObject zone in alreadyPossibleZones)
            {
                allAdjacentZones.Remove(zone);
            }
        }

        foreach (GameObject potentialZone in allAdjacentZones)
        {
            ZoneInfo potentialZoneInfo = potentialZone.GetComponent<ZoneInfo>();

            if (potentialZoneInfo.GetCurrentOccupancy() >= potentialZoneInfo.maxOccupancy)
            {
                continue;  // Skip this potentialZone if potentialZone is at maxOccupancy
            }

            int terrainDifficultyCost = currentZoneInfo.terrainDifficulty >= ignoreTerrainDifficulty ? currentZoneInfo.terrainDifficulty - ignoreTerrainDifficulty : 0;
            int sizeCost = currentZoneInfo.GetCurrentHindrance(transform.gameObject, true);
            sizeCost = sizeCost >= ignoreSize ? sizeCost - ignoreSize : 0;
            int elevationCost = 0;
            if (currentZoneInfo.steeplyAdjacentZones.Contains(potentialZone))
            {
                elevationCost = Math.Abs(currentZoneInfo.elevation - potentialZoneInfo.elevation);
                elevationCost = elevationCost >= ignoreElevation ? elevationCost - ignoreElevation : 0;
            }
            int wallBreakCost = 0;
            if (currentZoneInfo.wall1AdjacentZones.Contains(potentialZone) || currentZoneInfo.wall2AdjacentZones.Contains(potentialZone) || currentZoneInfo.wall3AdjacentZones.Contains(potentialZone) || currentZoneInfo.wall4AdjacentZones.Contains(potentialZone) || currentZoneInfo.wall5AdjacentZones.Contains(potentialZone))
            {
                wallBreakCost = 2;
            }
            int totalMovementCost = 1 + terrainDifficultyCost + sizeCost + elevationCost + wallBreakCost + possibleDestinations[currentZone].movementSpent;

            if (movePoints >= totalMovementCost)  // if unit can move here
            {
                if (possibleDestinations.ContainsKey(potentialZone))
                {
                    if (possibleDestinations[potentialZone].movementSpent > totalMovementCost)
                    {
                        possibleDestinations[potentialZone].zones = new List<GameObject>(possibleDestinations[currentZone].zones);
                        possibleDestinations[potentialZone].zones.Add(currentZone);
                        possibleDestinations[potentialZone].movementSpent = totalMovementCost;
                        if (movePoints > totalMovementCost)
                        {
                            possibleDestinations = GetPossibleDestinations(potentialZone, possibleDestinations, alreadyPossibleZones);
                        }
                    }
                }
                else
                {
                    possibleDestinations.Add(potentialZone, new MovementPath());
                    possibleDestinations[potentialZone].zones = new List<GameObject>(possibleDestinations[currentZone].zones);
                    possibleDestinations[potentialZone].zones.Add(currentZone);
                    possibleDestinations[potentialZone].movementSpent = totalMovementCost;
                    if (movePoints > totalMovementCost)
                    {
                        possibleDestinations = GetPossibleDestinations(potentialZone, possibleDestinations, alreadyPossibleZones);
                    }
                }
            }
        }
        return possibleDestinations;
    }

    private Tuple<GameObject, double> GetPartialMoveAndWeight(Dictionary<GameObject, MovementPath> reachableDestinations)
    {
        double mostValuableActionWeight = 0;
        GameObject chosenDestination = null;

        foreach (MovementPath movementPath in reachableDestinations.Values)
        {
            movementPath.movementSpent = 0;  // In getting nextPossibleDestinations, don't want to account for previously spent movement, so set it to 0.
        }

        foreach (GameObject reachableZone in reachableDestinations.Keys)
        {
            Dictionary<GameObject, MovementPath> nextPossibleDestinations = GetPossibleDestinations(reachableZone, new Dictionary<GameObject, MovementPath>(reachableDestinations), new HashSet<GameObject>(reachableDestinations.Keys));
            List<UnitPossibleAction> futurePossibleActions = GetPossibleActions(nextPossibleDestinations);
            if (futurePossibleActions != null && futurePossibleActions.Count > 0)
            {
                foreach (UnitPossibleAction unitAction in futurePossibleActions)
                {
                    unitAction.actionWeight *= actionsWeightTable["ONEHALF_MOVE"];  // Reduce weight for the fact these actions can't be completed until a second activation.
                    if (unitAction.actionWeight > mostValuableActionWeight)
                    {
                        mostValuableActionWeight = unitAction.actionWeight;
                        chosenDestination = reachableZone;
                    }
                }
            }
        }
        if (chosenDestination != null)
        {
            return new Tuple<GameObject, double>(chosenDestination, mostValuableActionWeight);
        }
        //else
        //{  // This Error isn't correct as, in trying out each of the spawn zones, units frequently have nothing reachable within 2 or 3 moves.
        //    Debug.LogError("ERROR! In Unit.GetPartialMoveAndWeight, " + transform.tag + " in " + transform.parent.name + " has nowhere valuable to move even with three moves. Something must be wrong; No one is this useless.");
        //}
        return new Tuple<GameObject, double>(null, 0);
    }


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
        { "GUARD_COMPUTER", 5 },
        { "ONEHALF_MOVE", .25 },  // * weight of action accomplishable on unit's next turn
        { "ONETHIRD_MOVE", .0625 }  // * weight of action accomplishable on unit's next next turn  // TODO not yet implemented for GetPartialMoveAndWeight()
    };

    public class UnitPossibleAction
    {
        public Unit myUnit;
        public string actionType;
        public double actionWeight;
        public GameObject destinationZone;
        public MovementPath pathTaken;
        public GameObject targetedZone;

        public UnitPossibleAction(Unit theUnit, string unitActionType, double unitActionWeight, GameObject unitDestinationZone, GameObject unitTargetedZone = null, MovementPath unitPathTaken = null)
        {
            myUnit = theUnit;
            actionType = unitActionType;
            actionWeight = unitActionWeight;
            destinationZone = unitDestinationZone;
            targetedZone = unitTargetedZone;
            pathTaken = unitPathTaken;
        }
    }

    private List<UnitPossibleAction> GetPossibleActions(Dictionary<GameObject, MovementPath> possibleDestinationsAndPaths)
    {
        //List<ZoneInfo> possibleDestinationsInfo = new List<ZoneInfo>();
        //foreach (GameObject destination in possibleDestinations.Keys)
        //{
        //    possibleDestinationsInfo.Add(destination.GetComponent<ZoneInfo>());
        //}

        List<UnitPossibleAction> allPossibleActions = new List<UnitPossibleAction>();

        foreach (GameObject destinationZone in possibleDestinationsAndPaths.Keys)
        {
            ZoneInfo destination = destinationZone.GetComponent<ZoneInfo>();
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
                            allPossibleActions.Add(new UnitPossibleAction(this, actionType, actionWeight, destination.gameObject, null, possibleDestinationsAndPaths[destinationZone]));
                        }
                        break;
                    case "THOUGHT":
                        if (destination.HasToken("Computer") && GameObject.FindGameObjectsWithTag("Bomb").Length > 0)
                        {
                            actionWeight += actionsWeightTable[actionType];
                            actionWeight += destination.supportRerolls * actionsWeightTable["OBJECTIVE_REROLL"];
                            actionWeight += destinationHindrance * actionsWeightTable["OBJECTIVE_HINDRANCE"];
                            allPossibleActions.Add(new UnitPossibleAction(this, actionType, actionWeight, destination.gameObject, null, possibleDestinationsAndPaths[destinationZone]));
                        }
                        break;
                    case "MELEE":
                        if (destination.HasHeroes())
                        {
                            actionWeight += actionsWeightTable[actionType];
                            actionWeight += destination.supportRerolls * actionsWeightTable["ATTACK_REROLL"];
                            allPossibleActions.Add(new UnitPossibleAction(this, actionType, actionWeight, destination.gameObject, null, possibleDestinationsAndPaths[destinationZone]));
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
                            allPossibleActions.Add(new UnitPossibleAction(this, actionType, actionWeight, destination.gameObject, targetedZone, possibleDestinationsAndPaths[destinationZone]));
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
        GameObject currentZone = GetZone();

        Dictionary<GameObject, MovementPath> possibleDestinations = GetPossibleDestinations(currentZone);
        List<UnitPossibleAction> allPossibleUnitActions = GetPossibleActions(possibleDestinations);

        if (allPossibleUnitActions != null && allPossibleUnitActions.Count > 0)
        {
            foreach (UnitPossibleAction unitAction in allPossibleUnitActions)
            {
                if (unitAction.actionWeight > mostValuableActionWeight)
                {
                    mostValuableActionWeight = unitAction.actionWeight;
                }
            }
        }
        else
        {
            mostValuableActionWeight = GetPartialMoveAndWeight(possibleDestinations).Item2;
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

    IEnumerator MoveToken(MovementPath pathToMove)
    {
        yield return StartCoroutine(AnimateMovementPath(pathToMove));
        string debugString = "Moved " + tag;
        for (int i = 1; i < pathToMove.zones.Count; i++)
        {
            debugString += " from " + pathToMove.zones[i - 1].name + " to " + pathToMove.zones[i].name;
        }
        Debug.Log(debugString);
        yield return 0;
    }

    IEnumerator AnimateMovementPath(MovementPath movementPath)
    {
        GameObject destination = null;  // Needed for transform.SetParent(destination.transform) after loop
        for (int i = 1; i < movementPath.zones.Count; i++)
        {
            GameObject origin = movementPath.zones[i - 1];
            destination = movementPath.zones[i];
            if (wallBreaker > 0)
            {
                ZoneInfo originInfo = origin.GetComponent<ZoneInfo>();
                if (!originInfo.adjacentZones.Contains(destination) && !originInfo.steeplyAdjacentZones.Contains(destination))
                {
                    foreach (GameObject wallRubble in GameObject.FindGameObjectsWithTag("WallRubble"))
                    {
                        WallRubble wallRubbleInfo = wallRubble.GetComponent<WallRubble>();
                        // TODO Below condition could be replaced with dictionary style lookup built by ScenarioMap on Awake() just like unitPrefabsMasterDict
                        if ((origin == wallRubbleInfo.zone1 && destination == wallRubbleInfo.zone2) || (origin == wallRubbleInfo.zone2 && destination == wallRubbleInfo.zone1))
                        {
                            wallRubbleInfo.BreakWall();
                            break;
                        }
                    }
                }
            }
            yield return StartCoroutine(AnimateMovement(origin, destination));
        }
        if (destination != null)
        {
            ZoneInfo destinationInfo = destination.GetComponent<ZoneInfo>();
            transform.SetParent(destinationInfo.GetAvailableUnitSlot().transform);
        }
        yield return 0;
    }

    IEnumerator AnimateMovement(GameObject origin, GameObject destination)
    {
        float uncoverTime = 0.5f;
        float t = 0;

        ZoneInfo destinationInfo = destination.GetComponent<ZoneInfo>();
        GameObject destinationUnitSlot = destinationInfo.GetAvailableUnitSlot();

        Vector3 oldPosition = transform.position;
        Vector3 newPosition = destinationUnitSlot.transform.position;

        transform.SetParent(GameObject.FindGameObjectWithTag("AnimationContainer").transform);  // Needed so unit animating is always drawn last (above everything it might pass over).

        while (t < 1f)
        {
            t += Time.deltaTime * uncoverTime;

            transform.position = new Vector3(Mathf.Lerp(oldPosition.x, newPosition.x, t), Mathf.Lerp(oldPosition.y, newPosition.y, t), 0);

            yield return null;
        }
        yield return 0;
    }

    IEnumerator PerformAction(UnitPossibleAction unitTurn)
    {
        int actionSuccesses = 0;
        int requiredSuccesses;
        GameObject currentZone = GetZone();
        ZoneInfo currentZoneInfo = currentZone.GetComponent<ZoneInfo>();
        int rerolls = currentZoneInfo.supportRerolls - supportRerolls;

        switch (unitTurn.actionType)
        {
            case "MANIPULATION":
                requiredSuccesses = 3;
                actionSuccesses += munitionSpecialist;
                actionSuccesses = RollAndReroll(validActionProficiencies[unitTurn.actionType], actionSuccesses, rerolls, requiredSuccesses);
                actionSuccesses -= currentZoneInfo.GetCurrentHindrance(transform.gameObject);
                if (actionSuccesses >= requiredSuccesses)
                {
                    currentZoneInfo.PrimeBomb();
                }
                break;

            case "THOUGHT":
                requiredSuccesses = 3;
                actionSuccesses = RollAndReroll(validActionProficiencies[unitTurn.actionType], actionSuccesses, rerolls, requiredSuccesses);
                actionSuccesses -= currentZoneInfo.GetCurrentHindrance(transform.gameObject);
                if (actionSuccesses >= requiredSuccesses)
                {
                    List<ZoneInfo> bombZones = new List<ZoneInfo>();
                    foreach (GameObject bomb in GameObject.FindGameObjectsWithTag("Bomb"))
                    {
                        bombZones.Add(bomb.transform.parent.parent.GetComponentInParent<ZoneInfo>());
                    }
                    ZoneInfo chosenBombZone = null;
                    double greatestManipulationChance = -100;
                    foreach (ZoneInfo bombZone in bombZones)
                    {
                        double bombZoneManipulationChance = bombZone.GetOccupantsManipulationLikelihood(transform.gameObject);
                        if (bombZoneManipulationChance > greatestManipulationChance)
                        {
                            greatestManipulationChance = bombZoneManipulationChance;
                            chosenBombZone = bombZone;
                        }
                    }
                    if (chosenBombZone != null)
                    {
                        currentZoneInfo.RemoveComputer();
                        chosenBombZone.PrimeBomb();
                        unitTurn.targetedZone = chosenBombZone.transform.gameObject;  // Only useful for DEBUG statement at end of PerformAction()
                    }
                    else
                    {
                        Debug.LogError("ERROR! Tried to use a computer from " + currentZone.name + " to prime a bomb, but no zones with bombs available. Why was computer able to be used if there are no zones with bombs?");
                    }
                }
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

                    if (currentZone == unitTurn.targetedZone)
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
        string debugString = tag + " in " + unitTurn.destinationZone.name + " performed " + unitTurn.actionType;
        if (unitTurn.targetedZone != null)
        {
            debugString += " targeting " + unitTurn.targetedZone.name;
        }
        debugString += " and got " + actionSuccesses + " successes";
        Debug.Log(tag + " in " + unitTurn.destinationZone.name + " performed " + unitTurn.actionType + " and got " + actionSuccesses + " successes");
        yield return 0;
    }


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

    public void TokenClicked()
    {
        if (lifePoints > 0)
        {
            ModifyLifePoints(-1);
        }
        else
        {
            ModifyLifePoints(1);
        }
    }

    public void ModifyLifePoints(int difference)
    {
        lifePoints += difference;
        if (lifePointsMax > 1)  // Will be a VillainRow with a UnitNumber object instead of just a token button like Unit.
        {
            try
            {
                transform.Find("UnitNumber").GetComponent<TMP_Text>().text = lifePoints.ToString();
            }
            catch (Exception err)
            {
                Debug.LogError("Failed to get/adjust UnitNumber representing lifePoints from unit " + transform.name + ".  Error details: " + err.ToString());
            }
        }

        if (lifePoints > 0)
        {
            this.GetComponent<CanvasGroup>().alpha = (float)1;
        }
        else
        {
            this.GetComponent<CanvasGroup>().alpha = (float).2;
        }
    }

    public UnitSave ToJSON()
    {
        return new UnitSave(this);
    }
}


[Serializable]
public class UnitSave
{
    public string tag;
    public int lifePoints;

    public UnitSave(Unit unit)
    {
        tag = unit.tag;
        lifePoints = unit.lifePoints;
    }
}
