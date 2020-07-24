using System;  // for [Serializable]
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;  // For button
using TMPro;  // for TMP_Text to update the henchmen quantity from UnitRows

public class Unit : MonoBehaviour
{
    readonly System.Random random = new System.Random();

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
    public int ignoreElevation = 0;  // TODO Also ignores this many wounds caused by a fall
    public int ignoreSize = 0;
    // TODO public int ignoreMenace = 0;  // NervesOfSteel (The Joker and Riddler's Gang w/ Handgun)
    public int wallBreaker = 0;

    public int martialArtsSuccesses = 0;
    // TODO public int reach = 0;  // Reach (Clayface and Penguin's Gang) Melee attacks can target heroes/miniatures this far away with LoS
    // TODO public int berserk = 0;  // Berserk (Killer Croc) If lifePoints <= lifePointsMax/2, add this many white dice to each attack
    public int circularStrike = 0;  // TODO for CHAINS, if hero removed after MELEE with another hero in that zone, popup prompt saying up to this many additional successes carry over
    public int counterAttack = 0;  // TODO for SHOTGUN, if hero moves into space with SHOTGUN, reminder that after melee attack against SHOTGUN is resolved, SHOTGUN gets free melee attack vs hero with number of yellow dice = counterattack

    public int marksmanSuccesses = 0;
    public int pointBlankRerolls = 0;
    // TODO public int counterRangedAttack = 0;  // Retaliation (Riddler's Gang w/ Handgun) if hero not in unit's space, reminder that after ranged attack against unit is resolved, unit gets free ranged attack vs hero with this number of yellow dice

    public int munitionSpecialist = 0;

    public bool gasImmunity = false;
    public bool frosty = false;  // OTTERPOP's ability to ignore frost and cryogenic tokens and spawn frost tokens  // Frost (Mr. Freeze) During attack or explosion from unit, place Frost Token in targeted area, which increases difficult terrain by number of Frost Tokens (except Mr. Freeze)
    public bool fiery = false;  // Firefly's ability to place flame tokens after attack/explosion and be immune to flame tokens

    // Actions
    // TODO TODO public int blast = 0;  // Blast (Mr. Freeze) auto manipulation to trigger this level of explosion in unit's area and adjacent area with LoS
    // TODO TODO public int grenade = 0;  // Grenade (Mr. Freeze) Complex manipulation to trigger this level of explosion in targeted area with LoS w/ difficulty = distance, failure has explosion triggered at distance equal to number of successes
    // TODO public int moveCommand = 0;  // Tactician (Two-Face) auto thought to immediately grant this many free move points to an ally (unit can only receive this once per turn in case multiple units have moveCommand > 0). If ally is character, they also get their Move Point Bonus for the First Movement (but not again if activated later in the turn, I assume)

    // Remainder TODO Harmless, Reduced Mobility, Attraction, Shackle, Hacking, Gas Immunity, Misfortune, Horror, Regeneration, Fly, Impenetrable Defense, Untouchable, Burst, Luck, Flame, Toxic Gas, Poison, Body Guard, Investigation, Sneak Attack, Imaginary Friend


    [Serializable]
    public struct ActionProficiency
    {
        public string actionType;
        public int actionMultiplier;  // The number of times the action can be performed
        public List<GameObject> proficiencyDice;
    }
    public ActionProficiency[] actionProficiencies;  // Unity can't expose dictionaries in the inspector, so Dictionary<string, GameObject[]> actionProficiencies not possible without addon


    void Awake()  // Need to happen on Instantiate for potential spawn/reinforcement evaluation, so Start() not good enough
    {
        transform.name = transform.tag;  // Needed so that when Instantiated is named UZI or CHAINS instead of UZI(Clone) or CHAINS(Clone)
    }

    public bool IsActive()
    {
        if (lifePoints > 0)
        {
            return true;
        }
        return false;
    }

    public GameObject GetZone()
    {
        return transform.parent.parent.parent.gameObject;  // Grabs ZoneInfoPanel instead of UnitsContainer. If changes in future, only need to change this function.
    }

    public IEnumerator ActivateUnit(Dictionary<string, List<(string, int, double, ScenarioMap.ActionCallback)>> actionsWeightTable)
    {
        GameObject currentZone = GetZone();
        Dictionary<GameObject, MovementPath> possibleDestinations = GetPossibleDestinations(currentZone);
        List<UnitPossibleAction> allPossibleUnitActions = GetPossibleActions(possibleDestinations, actionsWeightTable);

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
            yield return StartCoroutine(PerformAction(chosenAction, actionsWeightTable));
        }
        else
        {
            GameObject destinationZone = GetPartialMoveAndWeight(possibleDestinations, actionsWeightTable).Item1;
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
        public int terrainDanger = 0;
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
            if (!frosty)
            {
                List<GameObject> frostTokens = potentialZoneInfo.GetAllTokensWithTag("Frost");
                foreach (GameObject frostToken in frostTokens)
                {
                    terrainDifficultyCost += frostToken.GetComponent<EnvironToken>().quantity;
                }
                List<GameObject> cryogenicTokens = potentialZoneInfo.GetAllTokensWithTag("Cryogenic");
                foreach (GameObject cryogenicToken in cryogenicTokens)
                {
                    terrainDifficultyCost += cryogenicToken.GetComponent<EnvironToken>().quantity;
                }
            }
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
                int totalTerrainDanger = possibleDestinations[currentZone].terrainDanger + potentialZoneInfo.GetTerrainDangerTotal(this);
                if (possibleDestinations.ContainsKey(potentialZone))
                {
                    if (totalTerrainDanger < possibleDestinations[potentialZone].terrainDanger || (totalTerrainDanger == possibleDestinations[potentialZone].terrainDanger &&  totalMovementCost < possibleDestinations[potentialZone].movementSpent))
                    {
                        possibleDestinations[potentialZone].zones = new List<GameObject>(possibleDestinations[currentZone].zones);
                        possibleDestinations[potentialZone].zones.Add(currentZone);
                        possibleDestinations[potentialZone].terrainDanger = totalTerrainDanger;
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
                    possibleDestinations[potentialZone].terrainDanger = totalTerrainDanger;
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

    private Tuple<GameObject, double> GetPartialMoveAndWeight(Dictionary<GameObject, MovementPath> reachableDestinations, Dictionary<string, List<(string, int, double, ScenarioMap.ActionCallback)>> actionsWeightTable)
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
            List<UnitPossibleAction> futurePossibleActions = GetPossibleActions(nextPossibleDestinations, actionsWeightTable);
            if (futurePossibleActions != null && futurePossibleActions.Count > 0)
            {
                foreach (UnitPossibleAction unitAction in futurePossibleActions)
                {
                    unitAction.actionWeight *= .25;  // Reduce weight for the fact these actions can't be completed until a second activation. TODO add another lookahead (for next next turn) with a .0625 multiplier for a third activation
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

    private double GetAverageSuccesses(List<GameObject> dice, int rerolls = 0)
    {
        double averageSuccesses = 0;
        //if (rerolls > 0)
        //{
        //    // TODO now how do you calculate the improved averageSuccesses from rerolls? Have to make all possible combinations of all dieResults lists and reroll the most below average die number.
        //    // Sum the values multiplied by their odds
        //    // For a YellowDie, dieResults = [0,0,0,1,1,2], average = 0/6 + 0/6 + 0/6 + 1/6 + 1/6 + 2/6 = 2/3.  For a YellowRerollableDie, dieResults = [[0,0,0,1,1,2],[0,0,0,1,1,2],[0,0,0,1,1,2],1,1,2], average = (0/6 + 0/6 + 0/6 + 1/6 + 1/6 + 2/6)/6 + 2/3/6 + 2/3/6 + 1/6 + 1/6 + 2/6 = 1.
        //    List<List<(double, List<int>)>> diceCombinations = new List<List<(double, List<int>)>>();  // probably not, though
        //    // averageSuccesses = sum over all[result * probability of result]
        //    // For two YellowDice, totalCombinations = 6*6 = 36,  average = dieResults * occurences / totalCombinations = (0,0)*9/36 + (0,1)*6*2/36 + (0,2)*3*2/36 + (1,1)*4/36 + (1,2)*2*2/36 + (2,2)*1/36 = 4/3
        //    // For two YellowDice with one reroll, average = ((0,0)*3/6 + (0,1)*2/6 + (0,2)*1/6)*9/36 + ((0,1)*3 + (1,1)*2 + (1,2)*1)*12/216 + ((0,2)*3 + (1,2)*2 + (2,2)*1)*6/216 + (1,1)*4/36 + (1,2)*2*2/36 + (2,2)*1/36 = 1.95238095238?

        //    foreach (Dice die in dice)
        //    {
        //        //List<double> dieResults = new List<double>(Array.ConvertAll<int, double>(die.faces, num => (double)num));
        //        List<int> dieResults = new List<int>();
        //        if (die.rerollable)
        //        {
        //            for (int i = 0; i < dieResults.Count; i++)
        //            {
        //                if (dieResults[i] < die.averageSuccesses)
        //                {
        //                    //    dieResults[i] = die.averageSuccesses;  // I think this only works when rerolls = 0. Otherwise, have to inject die.faces into this result and calculate yet more possible combinations in next step (assuming rerolls > 0).
        //                }
        //                else
        //                {
        //                    break;
        //                }
        //            }
        //        }
        //    }
        //}
        //else  // No generic rerolls, so getting the average is easy
        //{
        //    foreach (Dice die in dice)
        //    {
        //        averageSuccesses += die.averageSuccessesWithReroll;  // Accounts for dice specific rerolls, but not generic rerolls
        //    }
        //}
        foreach (GameObject die in dice)
        {
            averageSuccesses += die.GetComponent<Dice>().averageSuccessesWithReroll;  // Accounts for dice specific rerolls, but not generic rerolls
        }

        averageSuccesses += dice.Count * rerolls / 3;  // For two YellowDice average = 1.33333333333, with reroll average = 1.95238095238, this func's estimation = 1.33333339 + 2 * 1 / 3 =  1.999999999
        return averageSuccesses;
    }

    private double GetChanceOfSuccess(int requiredSuccesses, List<GameObject> dice, int rerolls = 0)
    {
        double chanceOfSuccess = 0;
        // TODO proper method from NiceToHaves vs this incredibly rough estimation
        chanceOfSuccess = GetAverageSuccesses(dice, rerolls) / requiredSuccesses;
        return chanceOfSuccess;
    }

    public class UnitPossibleAction
    {
        public Unit myUnit;
        public (string, int, double, ScenarioMap.ActionCallback) missionSpecificAction;
        public ActionProficiency actionProficiency;
        public double actionWeight;
        public GameObject destinationZone;
        public MovementPath pathTaken;
        public GameObject targetedZone;

        public UnitPossibleAction(Unit theUnit, (string, int, double, ScenarioMap.ActionCallback) unitMissionSpecificAction, ActionProficiency unitActionProficiency, double unitActionWeight, GameObject unitDestinationZone, GameObject unitTargetedZone = null, MovementPath unitPathTaken = null)
        {
            myUnit = theUnit;
            missionSpecificAction = unitMissionSpecificAction;
            actionProficiency = unitActionProficiency;
            actionWeight = unitActionWeight;
            destinationZone = unitDestinationZone;
            targetedZone = unitTargetedZone;
            pathTaken = unitPathTaken;
        }
    }

    private List<UnitPossibleAction> GetPossibleActions(Dictionary<GameObject, MovementPath> possibleDestinationsAndPaths, Dictionary<string, List<(string, int, double, ScenarioMap.ActionCallback)>> actionsWeightTable)
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
            int destinationHindrance = destination.GetCurrentHindrance(gameObject);
            int availableRerolls = destination.supportRerolls;
            if (GetZone() == destinationZone)
            {
                availableRerolls -= supportRerolls;
            }

            foreach (ActionProficiency actionProficiency in actionProficiencies)
            {
                double actionWeight = 0;

                if (actionsWeightTable.ContainsKey("GUARD"))
                {
                    foreach ((string, int, double, ScenarioMap.ActionCallback) guardable in actionsWeightTable["GUARD"])
                    {
                        if (destination.HasObjectiveToken(guardable.Item1))
                        {
                            actionWeight += guardable.Item3;
                        }
                    }
                }

                switch (actionProficiency.actionType)
                {
                    case "MELEE":
                        if (destination.HasHeroes())
                        {
                            double averageWounds = GetAverageSuccesses(actionProficiency.proficiencyDice, availableRerolls) + martialArtsSuccesses;
                            averageWounds *= actionProficiency.actionMultiplier;
                            actionWeight += averageWounds * actionsWeightTable["MELEE"][0].Item3;
                            if (possibleDestinationsAndPaths[destinationZone].terrainDanger > 0)
                            {
                                actionWeight /= (2 * possibleDestinationsAndPaths[destinationZone].terrainDanger);
                            }
                            allPossibleActions.Add(new UnitPossibleAction(this, actionsWeightTable["MELEE"][0], actionProficiency, actionWeight, destination.gameObject, null, possibleDestinationsAndPaths[destinationZone]));
                        }
                        break;
                    case "RANGED":
                        GameObject targetedZone = destination.GetLineOfSightZoneWithHero();
                        if (targetedZone != null)
                        {
                            List<GameObject> dicePool = new List<GameObject>(actionProficiency.proficiencyDice);
                            if (destination.elevation > targetedZone.GetComponent<ZoneInfo>().elevation)
                            {
                                dicePool.Add(destination.environmentalDie);
                            }
                            if (pointBlankRerolls > 0 && targetedZone == destination.gameObject)
                            {
                                availableRerolls += pointBlankRerolls;
                            }
                            double averageWounds = GetAverageSuccesses(dicePool, availableRerolls) + marksmanSuccesses - destinationHindrance;
                            if (averageWounds > 0)
                            {
                                averageWounds *= actionProficiency.actionMultiplier;
                            }
                            else
                            {
                                averageWounds += actionProficiency.actionMultiplier / 10;
                            }
                            averageWounds *= actionProficiency.actionMultiplier;
                            actionWeight += averageWounds * actionsWeightTable["RANGED"][0].Item3;
                            if (possibleDestinationsAndPaths[destinationZone].terrainDanger > 0)
                            {
                                actionWeight /= (2 * possibleDestinationsAndPaths[destinationZone].terrainDanger);
                            }
                            allPossibleActions.Add(new UnitPossibleAction(this, actionsWeightTable["RANGED"][0], actionProficiency, actionWeight, destination.gameObject, targetedZone, possibleDestinationsAndPaths[destinationZone]));
                        }
                        break;
                    case "MANIPULATION":
                        if (actionsWeightTable.ContainsKey("MANIPULATION"))
                        {

                            foreach ((string, int, double, ScenarioMap.ActionCallback) manipulatable in actionsWeightTable["MANIPULATION"])
                            {
                                if (destination.HasObjectiveToken(manipulatable.Item1))
                                {
                                    int requiredSuccesses = manipulatable.Item2 + destinationHindrance - (manipulatable.Item1 == "Bomb" ? munitionSpecialist : 0);
                                    double chanceOfSuccess = GetChanceOfSuccess(requiredSuccesses, actionProficiency.proficiencyDice, availableRerolls);
                                    actionWeight += chanceOfSuccess * manipulatable.Item3;
                                    if (possibleDestinationsAndPaths[destinationZone].terrainDanger > 0)
                                    {
                                        actionWeight /= (2 * possibleDestinationsAndPaths[destinationZone].terrainDanger);
                                    }
                                    allPossibleActions.Add(new UnitPossibleAction(this, manipulatable, actionProficiency, actionWeight, destination.gameObject, null, possibleDestinationsAndPaths[destinationZone]));
                                }
                            }
                        }
                        break;
                    case "THOUGHT":
                        if (actionsWeightTable.ContainsKey("THOUGHT"))
                        {
                            foreach ((string, int, double, ScenarioMap.ActionCallback) thoughtable in actionsWeightTable["THOUGHT"])
                            {
                                if (destination.HasObjectiveToken(thoughtable.Item1))
                                {
                                    int requiredSuccesses = thoughtable.Item2 + destinationHindrance;
                                    double chanceOfSuccess = GetChanceOfSuccess(requiredSuccesses, actionProficiency.proficiencyDice, availableRerolls);
                                    actionWeight += chanceOfSuccess * thoughtable.Item3;
                                    if (possibleDestinationsAndPaths[destinationZone].terrainDanger > 0)
                                    {
                                        actionWeight /= (2 * possibleDestinationsAndPaths[destinationZone].terrainDanger);
                                    }
                                    allPossibleActions.Add(new UnitPossibleAction(this, thoughtable, actionProficiency, actionWeight, destination.gameObject, null, possibleDestinationsAndPaths[destinationZone]));
                                }
                            }
                        }
                        break;
                }
            }
        }

        return allPossibleActions;
    }

    public double GetMostValuableActionWeight(Dictionary<string, List<(string, int, double, ScenarioMap.ActionCallback)>> actionsWeightTable)
    {
        double mostValuableActionWeight = 0;
        GameObject currentZone = GetZone();

        Dictionary<GameObject, MovementPath> possibleDestinations = GetPossibleDestinations(currentZone);
        List<UnitPossibleAction> allPossibleUnitActions = GetPossibleActions(possibleDestinations, actionsWeightTable);

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
            mostValuableActionWeight = GetPartialMoveAndWeight(possibleDestinations, actionsWeightTable).Item2;
        }

        return mostValuableActionWeight;
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

            ZoneInfo destinationInfo = destination.GetComponent<ZoneInfo>();
            int desinationTerrainDanger = destinationInfo.GetTerrainDangerTotal(this);
            int automaticWounds = 0;
            Dice damageDie = destinationInfo.environmentalDie.GetComponent<Dice>();
            for (int j = 0; j < desinationTerrainDanger; j++)
            {
                automaticWounds += damageDie.Roll();
            }
            ModifyLifePoints(-automaticWounds);
            if (lifePoints <= 0)
            {
                break;
            }
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
        GameObject destinationUnitSlot = destination.GetComponent<ZoneInfo>().GetAvailableUnitSlot();
        GameObject mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
        transform.SetParent(GameObject.FindGameObjectWithTag("AnimationContainer").transform);  // Needed so unit animating is always drawn last (above everything it might pass over).

        yield return StartCoroutine(MoveObjectOverTime(new List<GameObject>() { transform.gameObject, mainCamera }, transform.position, destinationUnitSlot.transform.position));
        yield return 0;
    }

    Boolean waitingOnPlayerInput = false;
    public IEnumerator PauseUntilPlayerPushesContinue(GameObject animationContainer, ZoneInfo targetedZoneInfo, GameObject targetedHero)
    {
        Button heroButton = targetedHero.GetComponent<Button>();
        heroButton.enabled = true;
        waitingOnPlayerInput = true;
        GameObject continueButton = Instantiate(targetedZoneInfo.confirmButtonPrefab, targetedZoneInfo.transform.parent.transform);
        continueButton.transform.position = targetedHero.transform.TransformPoint(0, 20f, 0);
        continueButton.GetComponent<Button>().onClick.AddListener(delegate { waitingOnPlayerInput = false; });
        yield return new WaitUntil(() => !waitingOnPlayerInput);
        heroButton.enabled = false;
        Destroy(continueButton);
        yield return 0;
    }

    private readonly Vector2[] woundPlacement = new[] { new Vector2(7f, 6f), new Vector2(7f, 0f), new Vector2(7f, -6f), new Vector2(-7f, 6f), new Vector2(-7f, 0f), new Vector2(-7f, -6f), new Vector2(-2.5f, 7f), new Vector2(2.5f, 7f), new Vector2(-2.5f, -7f), new Vector2(2.5f, -7f) };
    public IEnumerator AnimateWounds(ZoneInfo targetedZoneInfo, int woundsTotal)
    {
        GameObject targetedHero = targetedZoneInfo.GetRandomHero();

        if (targetedHero != null)
        {
            GameObject animationContainer = GameObject.FindGameObjectWithTag("AnimationContainer");
            GameObject mainCamera = GameObject.FindGameObjectWithTag("MainCamera");

            for (int i = 0; i < woundsTotal; i++)
            {
                if (i == (woundsTotal - 1) / 2)  // If halfway or less through woundsTotal
                {
                    StartCoroutine(MoveObjectOverTime(new List<GameObject>() { mainCamera }, mainCamera.transform.position, targetedHero.transform.position));  // If only one wound, AnimateWounds() will return before camera finished panning
                }
                GameObject wound = Instantiate(targetedZoneInfo.woundPrefab, transform);
                Vector3 newDestination = targetedHero.transform.TransformPoint(woundPlacement[i].x, woundPlacement[i].y, 0);
                wound.transform.SetParent(animationContainer.transform);  // Needed so unit animating is always drawn last (above everything it might pass over).
                if (i < woundsTotal - 1)
                {
                    StartCoroutine(MoveObjectOverTime(new List<GameObject>() { wound }, wound.transform.position, newDestination, .7f));
                    yield return new WaitForSecondsRealtime(.5f);
                }
                else  // If last wound
                {
                    yield return StartCoroutine(MoveObjectOverTime(new List<GameObject>() { wound }, wound.transform.position, newDestination, .7f));
                }
            }
            yield return StartCoroutine(PauseUntilPlayerPushesContinue(animationContainer, targetedZoneInfo, targetedHero));

            // Fading the wounds out
            float fadeoutTime = 0.7f;
            float t = 0;
            CanvasGroup animationContainerTransparency = animationContainer.GetComponent<CanvasGroup>();
            while (t < 1f)
            {
                t += Time.deltaTime * fadeoutTime;

                float transparency = Mathf.Lerp(1, .2f, t);
                animationContainerTransparency.alpha = transparency;

                yield return null;
            }

            for (int i = woundsTotal - 1; i >= 0; i--)
            {
                Destroy(animationContainer.transform.GetChild(i).gameObject);
            }
            animationContainerTransparency.alpha = 1f;  // Reset animationContainer transparency
        }
        yield return 0;
    }

    IEnumerator MoveObjectOverTime(List<GameObject> objectsToMove, Vector3 origin, Vector3 destination, float timeCoefficient = .5f)
    {
        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * timeCoefficient;
            foreach( GameObject currentObject in objectsToMove)
            {
                currentObject.transform.position = new Vector3(Mathf.Lerp(origin.x, destination.x, t), Mathf.Lerp(origin.y, destination.y, t), currentObject.transform.position.z);
            }
            yield return null;
        }
        yield return 0;
    }

    IEnumerator PerformAction(UnitPossibleAction unitTurn, Dictionary<string, List<(string, int, double, ScenarioMap.ActionCallback)>> actionsWeightTable)
    {
        int actionSuccesses = 0;
        int requiredSuccesses;
        GameObject currentZone = GetZone();
        ZoneInfo currentZoneInfo = currentZone.GetComponent<ZoneInfo>();
        int currentZoneHindrance = currentZoneInfo.GetCurrentHindrance(gameObject);
        int availableRerolls = currentZoneInfo.supportRerolls - supportRerolls;

        GameObject mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
        mainCamera.transform.position = new Vector3(transform.position.x, transform.position.y, mainCamera.transform.position.z);
        GameObject animationContainer = GameObject.FindGameObjectWithTag("AnimationContainer");

        switch (unitTurn.actionProficiency.actionType)
        {
            case "MELEE":
                for (int i = 0; i < unitTurn.actionProficiency.actionMultiplier; i++)
                {
                    actionSuccesses = RollAndReroll(unitTurn.actionProficiency.proficiencyDice, availableRerolls);
                    if (actionSuccesses > 0)
                    {
                        actionSuccesses += martialArtsSuccesses;
                        yield return StartCoroutine(AnimateWounds(currentZoneInfo, actionSuccesses));
                    }
                    if (fiery)
                    {
                        currentZoneInfo.AddEnvironTokens(new EnvironTokenSave("Flame", 1, false, true));
                    }
                    if (frosty)
                    {
                        currentZoneInfo.AddEnvironTokens(new EnvironTokenSave("Frost", 1, false, true));
                    }
                }
                break;
            case "RANGED":
                if (unitTurn.targetedZone != null)
                {
                    ZoneInfo targetedLineOfSightZoneInfo = unitTurn.targetedZone.GetComponent<ZoneInfo>();

                    List<GameObject> dicePool = new List<GameObject>(unitTurn.actionProficiency.proficiencyDice);
                    if (currentZoneInfo.elevation > targetedLineOfSightZoneInfo.elevation)
                    {
                        dicePool.Add(currentZoneInfo.environmentalDie);
                    }

                    if (currentZone == unitTurn.targetedZone)
                    {
                        availableRerolls += pointBlankRerolls;
                    }

                    for (int i = 0; i < unitTurn.actionProficiency.actionMultiplier; i++)
                    {
                        actionSuccesses = RollAndReroll(dicePool, availableRerolls);
                        if (actionSuccesses > 0)
                        {
                            actionSuccesses += marksmanSuccesses;
                        }
                        actionSuccesses -= currentZoneInfo.GetCurrentHindrance(gameObject);
                        if (actionSuccesses > 0)
                        {
                            yield return StartCoroutine(AnimateWounds(targetedLineOfSightZoneInfo, actionSuccesses));
                        }
                    }
                    if (fiery)
                    {
                        targetedLineOfSightZoneInfo.AddEnvironTokens(new EnvironTokenSave("Flame", 1, false, true));
                    }
                    if (frosty)
                    {
                        targetedLineOfSightZoneInfo.AddEnvironTokens(new EnvironTokenSave("Frost", 1, false, true));
                    }
                }
                else
                {
                    Debug.LogError("ERROR! RANGED action was performed while targetedLineOfSightZone was null, so henchman just wasted its action wildly firing its gun into the air.");
                }
                break;
            case "MANIPULATION":
                requiredSuccesses = unitTurn.missionSpecificAction.Item2 + currentZoneHindrance - (unitTurn.missionSpecificAction.Item1 == "Bomb" ? munitionSpecialist : 0);
                actionSuccesses = RollAndReroll(unitTurn.actionProficiency.proficiencyDice, availableRerolls, requiredSuccesses);
                yield return StartCoroutine(unitTurn.missionSpecificAction.Item4(gameObject, null, actionSuccesses, requiredSuccesses));
                break;
            case "THOUGHT":
                requiredSuccesses = unitTurn.missionSpecificAction.Item2;
                actionSuccesses = RollAndReroll(unitTurn.actionProficiency.proficiencyDice, availableRerolls, requiredSuccesses);
                yield return StartCoroutine(unitTurn.missionSpecificAction.Item4(gameObject, null, actionSuccesses, requiredSuccesses));
                break;
        }
        string debugString = tag + " in " + unitTurn.destinationZone.name + " performed " + unitTurn.actionProficiency.actionType;
        if (unitTurn.targetedZone != null)
        {
            debugString += " targeting " + unitTurn.targetedZone.name;
        }
        debugString += " and got " + actionSuccesses + " successes";
        Debug.Log(debugString);
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

    private int RollAndReroll(List<GameObject> dicePool, int rerolls, int requiredSuccesses) // TODO still need to fix this like improvements to other RollAndReroll (rerollable dice can be rerolled across their color, multiple dice can be rerolled)
    {
        int rolledSuccesses = 0;
        List<ActionResult> currentActionResults = new List<ActionResult>();

        foreach (GameObject die in dicePool)
        {
            Dice dieInfo = die.GetComponent<Dice>();
            ActionResult currentActionResult = new ActionResult(dieInfo, dieInfo.Roll());
            rolledSuccesses += currentActionResult.successes;
            currentActionResults.Add(currentActionResult);
        }

        while (rolledSuccesses < requiredSuccesses)
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
                rolledSuccesses = rolledSuccesses - mostDisappointingResult.successes + rerolledDieSuccesses;
                currentActionResults[mostDisappointingResultIndex] = new ActionResult(mostDisappointingResult.die, rerolledDieSuccesses);
            }
        }

        return rolledSuccesses;
    }

    private int RollAndReroll(List<GameObject> dicePool, int rerolls)
    {
        rerolls = 1;
        int rolledSuccesses = 0;
        List<ActionResult> currentActionResults = new List<ActionResult>();
        string debugString = "RollAndReroll for unit " + gameObject.name + ". ";

        // Separate dice results by color for freeRerolls
        Dictionary<string, List<ActionResult>> actionResultsByColor = new Dictionary<string, List<ActionResult>>();
        foreach (GameObject die in dicePool)
        {
            Dice dieInfo = die.GetComponent<Dice>();
            ActionResult currentActionResult = new ActionResult(dieInfo, dieInfo.Roll());
            if (actionResultsByColor.ContainsKey(dieInfo.color))
            {
                actionResultsByColor[dieInfo.color].Add(currentActionResult);
            }
            else
            {
                actionResultsByColor[dieInfo.color] = new List<ActionResult>() { currentActionResult };
            }
        }

        // Apply freeRerolls
        foreach (List<ActionResult> dieResults in actionResultsByColor.Values)
        {
            int freeRerolls = 0;
            foreach (ActionResult result in dieResults)
            {
                if (result.die.rerollable)
                {
                    freeRerolls += 1;
                }
            }
            debugString += dieResults[0].die.color + " has " + freeRerolls.ToString() + " freeRerolls. ";

            if (freeRerolls > 0)
            {
                dieResults.Sort((x, y) => (y.die.averageSuccesses - y.successes).CompareTo(x.die.averageSuccesses - x.successes));  // Sorts from greatest below average to most above average

                for (int i = 0; i < freeRerolls; i++)
                {
                    if (dieResults[i].successes >= dieResults[i].die.averageSuccesses)
                    {
                        break;  // Exit freeRerolls loop if none of the dice rolled below average
                    }
                    else
                    {
                        debugString += "Using freeReroll to change from " + dieResults[i].successes.ToString() + " to ";
                        dieResults[i] = new ActionResult(dieResults[i].die, dieResults[i].die.Roll());
                        debugString += dieResults[i].successes.ToString();
                    }
                }
            }

            currentActionResults.AddRange(dieResults);
        }

        // Apply rerolls
        for (int i = 0; i < rerolls; i++)
        {
            debugString += "  On my " + (i + 1).ToString() + " reroll: ";
            for (int j = 0; j < currentActionResults.Count-1; j++)  // Reroll each die still below its average
            {
                if (currentActionResults[i].successes < currentActionResults[i].die.averageSuccesses)
                {
                    debugString += " Changing " + currentActionResults[i].successes.ToString() + " to ";
                    currentActionResults[i] = new ActionResult(currentActionResults[i].die, currentActionResults[i].die.Roll());
                    debugString += currentActionResults[i].successes.ToString();
                }
            }
        }

        // Add up final roll
        debugString += "\nFinal roll: ";
        foreach (ActionResult myActionResult in currentActionResults)
        {
            rolledSuccesses += myActionResult.successes;
            debugString += myActionResult.successes.ToString() + ", ";
        }
        Debug.Log(debugString);
        return rolledSuccesses;
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
            this.GetComponent<CanvasGroup>().alpha = 1;
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
