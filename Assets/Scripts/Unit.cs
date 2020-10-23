using System;  // for [Serializable]
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;  // For button
using TMPro;  // for TMP_Text to update the henchmen quantity from UnitRows

public class Unit : MonoBehaviour
{
    readonly System.Random random = new System.Random();
    private Animate animate;
    public float fadedAlpha = .2f;  // Public so can be used by ZoneInfo.cs when terrain danger increases

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
    public int grenade = 0;  // Grenade (Mr. Freeze) Complex manipulation to trigger this level of explosion in targeted area with LoS w/ difficulty = distance, failure has explosion triggered at distance equal to number of successes
    // TODO public int blast = 0;  // Blast (Mr. Freeze) auto manipulation to trigger this level of explosion in unit's area and adjacent area with LoS
    // TODO public int moveCommand = 0;  // Tactician (Two-Face) auto thought to immediately grant this many free move points to an ally (unit can only receive this once per turn in case multiple units have moveCommand > 0). If ally is character, they also get their Move Point Bonus for the First Movement (but not again if activated later in the turn, I assume)

    // Remainder TODO Harmless, Reduced Mobility, Attraction, Shackle, Hacking, Gas Immunity, Misfortune, Horror, Regeneration, Fly, Impenetrable Defense, Untouchable, Burst, Luck, Flame, Toxic Gas, Poison, Body Guard, Investigation, Sneak Attack, Imaginary Friend


    [Serializable]
    public struct ActionProficiency
    {
        public string actionType;
        public int actionMultiplier;  // The number of times the action can be performed
        public List<GameObject> proficiencyDice;

        public ActionProficiency(string newActionType, int newActionMultiplier, List<GameObject> newProficiencyDice)
        {
            actionType = newActionType;
            actionMultiplier = newActionMultiplier;
            proficiencyDice = newProficiencyDice;
        }
    }
    public ActionProficiency[] actionProficiencies;  // Unity can't expose dictionaries in the inspector, so Dictionary<string, GameObject[]> actionProficiencies not possible without addon
    private readonly ActionProficiency moveActionProficiency = new ActionProficiency("MOVE", 1, new List<GameObject>());


    void Awake()  // Need to happen on Instantiate for potential spawn/reinforcement evaluation, so Start() not good enough
    {
        transform.name = transform.tag;  // Needed so that when Instantiated is named UZI or CHAINS instead of UZI(Clone) or CHAINS(Clone)
        animate = GameObject.FindGameObjectWithTag("AnimationContainer").GetComponent<Animate>();  // Not initiated quickly enough in Start()
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

    public IEnumerator ActivateUnitWithPredeterminedAction(UnitPossibleAction chosenAction)
    {
        GameObject currentZone = GetZone();

        animate.CameraToFixedZoom();   // Placed here because otherwise the camera "jumps" from zoomed in to first unit's move
        if (currentZone != chosenAction.actionZone)
        {
            chosenAction.pathTaken.zones.Add(chosenAction.actionZone);  // Otherwise token is never animated moving the last zone to the destination
            yield return StartCoroutine(MoveToken(chosenAction.pathTaken));
        }

        if (!IsActive())  // If killed by first move
        {
            yield break;
        }
        if (chosenAction.actionProficiency.actionType != moveActionProficiency.actionType)  // If performing action other than moving
        {
            yield return StartCoroutine(PerformAction(chosenAction));
        }

        if (!IsActive())  // If killed by own action
        {
            yield break;
        }
        if (chosenAction.finalDestinationZone != chosenAction.actionZone)
        {
            Dictionary<GameObject, MovementPath> possibleFinalDestinations = GetPossibleDestinations(currentZone);
            if (!possibleFinalDestinations.ContainsKey(chosenAction.finalDestinationZone))  // If moving to chosenAction.finalDestinationZone is no longer possible (due to performed action)
            {
                if (MissionSpecifics.actionsWeightTable.ContainsKey("GUARD"))
                {
                    double mostValuableFinalDestinationWeight = 0;
                    foreach (GameObject possibleFinalDestinationZone in possibleFinalDestinations.Keys)
                    {
                        double currentFinalDestinationWeight = 0;
                        ZoneInfo possibleFinalDestinationZoneInfo = possibleFinalDestinationZone.GetComponent<ZoneInfo>();
                        foreach (MissionSpecifics.ActionWeight guardable in MissionSpecifics.actionsWeightTable["GUARD"])
                        {
                            if (possibleFinalDestinationZoneInfo.HasObjectiveToken(guardable.targetType))
                            {
                                currentFinalDestinationWeight += guardable.weightFactor;
                            }
                        }
                        if (currentFinalDestinationWeight > mostValuableFinalDestinationWeight)
                        {
                            chosenAction.finalDestinationZone = possibleFinalDestinationZone;
                            chosenAction.pathTaken = possibleFinalDestinations[possibleFinalDestinationZone];
                            mostValuableFinalDestinationWeight = currentFinalDestinationWeight;
                        }
                    }
                }
            }
            chosenAction.pathTaken.zones.Add(chosenAction.finalDestinationZone);
            yield return StartCoroutine(MoveToken(chosenAction.pathTaken));
        }
        //else
        //{
        //    GameObject destinationZone = GetPartialMoveAndWeight(possibleDestinations).Item1;
        //    if (destinationZone != null && currentZone != destinationZone)
        //    {
        //        possibleDestinations[destinationZone].zones.Add(destinationZone);  // Otherwise token is never animated moving the last zone to the destination
        //        yield return StartCoroutine(MoveToken(possibleDestinations[destinationZone]));
        //    }
        //}
        yield return 0;
    }

    public IEnumerator ActivateUnit(bool activatingLast = false)
    {
        GameObject currentZone = GetZone();
        Dictionary<GameObject, MovementPath> possibleDestinations = GetPossibleDestinations(currentZone);
        List<UnitPossibleAction> allPossibleUnitActions = GetPossibleActions(possibleDestinations);
        //Debug.Log("ActivateUnit for " + transform.tag + " in " + GetZone().name + "   allPossibleUnitActions.Count: " + allPossibleUnitActions.Count.ToString());

        if (allPossibleUnitActions != null && allPossibleUnitActions.Count > 0)  // if there's an action with weight > GetInactiveWeight()
        {
            UnitPossibleAction chosenAction = null;

            foreach (UnitPossibleAction unitAction in allPossibleUnitActions)
            {
                if (chosenAction == null || unitAction.actionWeight > chosenAction.actionWeight)
                {
                    chosenAction = unitAction;
                }
            }

            if (!activatingLast && chosenAction.missionSpecificAction.activateLast)  // If will negatively impact other unit turns, come back to this unit
            {
                UnitIntel.unitsToActivateLast.Enqueue(gameObject);
                yield break;
            }

            animate.CameraToFixedZoom();   // Placed here because otherwise the camera "jumps" from zoomed in to first unit's move
            if (currentZone != chosenAction.actionZone)
            {
                chosenAction.pathTaken.zones.Add(chosenAction.actionZone);  // Otherwise token is never animated moving the last zone to the destination
                yield return StartCoroutine(MoveToken(chosenAction.pathTaken));
            }

            if (!IsActive())  // If killed by first move
            {
                yield break;
            }
            if (chosenAction.actionProficiency.actionType != moveActionProficiency.actionType)  // If performing action other than moving
            {
                yield return StartCoroutine(PerformAction(chosenAction));
            }

            if (!IsActive())  // If killed by own action
            {
                yield break;
            }
            if (chosenAction.finalDestinationZone != chosenAction.actionZone)
            {
                Dictionary<GameObject, MovementPath> possibleFinalDestinations = GetPossibleDestinations(currentZone);
                if (!possibleFinalDestinations.ContainsKey(chosenAction.finalDestinationZone))  // If moving to chosenAction.finalDestinationZone is no longer possible (due to performed action)
                {
                    if (MissionSpecifics.actionsWeightTable.ContainsKey("GUARD"))
                    {
                        double mostValuableFinalDestinationWeight = 0;
                        foreach (GameObject possibleFinalDestinationZone in possibleFinalDestinations.Keys)
                        {
                            double currentFinalDestinationWeight = 0;
                            ZoneInfo possibleFinalDestinationZoneInfo = possibleFinalDestinationZone.GetComponent<ZoneInfo>();
                            foreach (MissionSpecifics.ActionWeight guardable in MissionSpecifics.actionsWeightTable["GUARD"])
                            {
                                if (possibleFinalDestinationZoneInfo.HasObjectiveToken(guardable.targetType))
                                {
                                    currentFinalDestinationWeight += guardable.weightFactor;
                                }
                            }
                            if (currentFinalDestinationWeight > mostValuableFinalDestinationWeight)
                            {
                                chosenAction.finalDestinationZone = possibleFinalDestinationZone;
                                chosenAction.pathTaken = possibleFinalDestinations[possibleFinalDestinationZone];
                                mostValuableFinalDestinationWeight = currentFinalDestinationWeight;
                            }
                        }
                    }
                }
                chosenAction.pathTaken.zones.Add(chosenAction.finalDestinationZone);
                yield return StartCoroutine(MoveToken(chosenAction.pathTaken));
            }
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
        public List<GameObject> zones;
        public int movementSpent = 0;
        public int terrainDanger = 0;

        public MovementPath()
        {
            zones = new List<GameObject>();
        }

        public MovementPath(int newMovementSpent, int newTerrainDanger, List<GameObject> newZones)
        {
            movementSpent = newMovementSpent;
            terrainDanger = newTerrainDanger;
            zones = new List<GameObject>(newZones);
        }
    }

    private Dictionary<GameObject, MovementPath> GetPossibleDestinations(GameObject currentZone, Dictionary<GameObject, MovementPath> possibleDestinations = null, HashSet<GameObject> alreadyPossibleZones = null)
    {
        int totalPossibleMovePoints = movePoints + UnitIntel.bonusMovePointsRemaining;
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
                terrainDifficultyCost += potentialZoneInfo.GetQuantityOfEnvironTokensWithTag("Frost") + potentialZoneInfo.GetQuantityOfEnvironTokensWithTag("Cryogenic");
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

            if (totalPossibleMovePoints >= totalMovementCost)  // if unit can move here
            {
                int totalTerrainDanger = possibleDestinations[currentZone].terrainDanger + potentialZoneInfo.GetTerrainDangerTotal(this);
                if (possibleDestinations.ContainsKey(potentialZone))
                {
                    // If two movementPaths to the same zone, prioritize movementPath with less terrain danger. If equal, prioritize movementPath with less totalMovementCost. If equal, prioritize movementPath that breaks a wall, because it's cool!
                    if (totalTerrainDanger < possibleDestinations[potentialZone].terrainDanger || (totalTerrainDanger == possibleDestinations[potentialZone].terrainDanger &&  (totalMovementCost < possibleDestinations[potentialZone].movementSpent || (totalMovementCost == possibleDestinations[potentialZone].movementSpent && wallBreakCost > 0))))
                    {
                        possibleDestinations[potentialZone].zones = new List<GameObject>(possibleDestinations[currentZone].zones);
                        possibleDestinations[potentialZone].zones.Add(currentZone);
                        possibleDestinations[potentialZone].terrainDanger = totalTerrainDanger;
                        possibleDestinations[potentialZone].movementSpent = totalMovementCost;
                        if (totalPossibleMovePoints > totalMovementCost)
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
                    if (totalPossibleMovePoints > totalMovementCost)
                    {
                        possibleDestinations = GetPossibleDestinations(potentialZone, possibleDestinations, alreadyPossibleZones);
                    }
                }
            }
        }
        return possibleDestinations;
    }

    private double GetAverageSuccesses(List<GameObject> dice, int rerolls = 0)
    {
        double averageSuccesses = 0;
        foreach (GameObject die in dice)
        {
            averageSuccesses += die.GetComponent<Dice>().GetExpectedValue(rerolls);
        }
        return averageSuccesses;
    }

    public double GetChanceOfSuccess(int requiredSuccesses, List<GameObject> dice, int rerolls = 0)
    {
        int maxSuccesses = 0;  // TODO Remove this maxSuccess check once the dice math regarding rerolls is fixed (so GetChanceOfSuccess returns 0 without this check)
        foreach (GameObject die in dice)
        {
            maxSuccesses += die.GetComponent<Dice>().GetLargestPossibleResult();
        }
        if (requiredSuccesses > maxSuccesses)
        {
            return 0;
        }

        List<(int, GameObject)> firstFailureResult = new List<(int, GameObject)>();
        foreach (GameObject die in dice)
        {
            firstFailureResult.Add((0, die));
        }
        List<List<(int, GameObject)>> failureResults = new List<List<(int, GameObject)>>() { firstFailureResult };

        while (true)
        {
            List<(int, GameObject)> nextFailCombo = GetNextDiceFailCombo(requiredSuccesses, failureResults[failureResults.Count - 1]);
            if (nextFailCombo != null)
            {
                failureResults.Add(nextFailCombo);
            }
            else
            {
                break;
            }
        }

        double probabilityOfFailure = 0;
        foreach (List<(int, GameObject)> failCombo in failureResults)
        {
            double comboProbability = 1;
            foreach ((int successes, GameObject die) in failCombo)
            {
                //Debug.Log(successes.ToString() + " " + die.name + "  probabilityOfResult: " + die.GetComponent<Dice>().GetProbabilityOfResult(successes, rerolls).ToString());
                comboProbability *= die.GetComponent<Dice>().GetProbabilityOfResult(successes, rerolls);
            }
            //Debug.Log("comboProbability: " + comboProbability.ToString());
            probabilityOfFailure += comboProbability;
        }
        //return GetAverageSuccesses(dice, rerolls) / requiredSuccesses;  // Old, rough estimation method
        return 1 - probabilityOfFailure;
    }

    // For requiredSuccesses = 4 and 3 dice:
    // 0,0,0->1,0,0->2,0,0->3,0,0->0,1,0->1,1,0->2,1,0->0,2,0->1,2,0->0,3,0->0,0,1->1,0,1->2,0,1->0,1,1->1,1,1->0,2,1->0,0,2->1,0,2->0,1,2->0,0,3
    public List<(int, GameObject)> GetNextDiceFailCombo(int requiredSuccesses, List<(int, GameObject)> previousFailCombo)
    {
        List<(int, GameObject)> nextFailCombo = new List<(int, GameObject)>(previousFailCombo);
        while (true)
        {
            if (nextFailCombo[nextFailCombo.Count - 1].Item1 >= requiredSuccesses - 1)  // If max successes already for last die, no more failCombos
            {
                break;
            }
            for (int i = 0; i < nextFailCombo.Count; i++)
            {
                if (nextFailCombo[i].Item1 < requiredSuccesses - 1)
                {
                    nextFailCombo[i] = (nextFailCombo[i].Item1 + 1, nextFailCombo[i].Item2);
                    break;
                }
                else
                {
                    nextFailCombo[i] = (0, nextFailCombo[i].Item2);
                }
            }
            int totalSuccesses = 0;
            foreach ((int successes, GameObject die) in nextFailCombo)
            {
                totalSuccesses += successes;
            }
            if (totalSuccesses < requiredSuccesses)
            {
                return nextFailCombo;
            }
        }
        return null;
    }

    public class UnitPossibleAction
    {
        public Unit myUnit;
        public MissionSpecifics.ActionWeight missionSpecificAction;
        public ActionProficiency actionProficiency;
        public double actionWeight;
        public GameObject actionZone;  // Zone where action is to be performed
        public GameObject finalDestinationZone;  // Where unit ends up, usually the same as the actionZone (unless unit moves after action)
        public MovementPath pathTaken;
        public GameObject targetedZone;

        public UnitPossibleAction(Unit theUnit, MissionSpecifics.ActionWeight unitMissionSpecificAction, ActionProficiency unitActionProficiency, double unitActionWeight, GameObject unitActionZone, GameObject unitFinalDestinationZone, GameObject unitTargetedZone = null, MovementPath unitPathTaken = null)
        {
            myUnit = theUnit;
            missionSpecificAction = unitMissionSpecificAction;
            actionProficiency = unitActionProficiency;
            actionWeight = unitActionWeight;
            actionZone = unitActionZone;
            finalDestinationZone = unitFinalDestinationZone;
            targetedZone = unitTargetedZone;
            pathTaken = unitPathTaken;
        }
    }

    public List<UnitPossibleAction> GetPossibleActions(Dictionary<GameObject, MovementPath> possibleDestinationsAndPaths, bool isPartialMove = false)  // TODO isPartialMove doesn't seem to change anything
    {
        List<UnitPossibleAction> allPossibleActions = new List<UnitPossibleAction>();

        foreach (GameObject possibleZone in possibleDestinationsAndPaths.Keys)
        {
            ZoneInfo possibleZoneInfo = possibleZone.GetComponent<ZoneInfo>();
            int actionZoneHindrance = possibleZoneInfo.GetCurrentHindrance(gameObject);
            int availableRerolls = possibleZoneInfo.GetSupportRerolls(gameObject);
            double terrainDangerWeight = 0;
            double bonusMovePointWeight = 0;
            double guardZoneWeight = 0;
            GameObject finalDestinationZone = possibleZone;
            MissionSpecifics.ActionWeight defaultGuardAction = new MissionSpecifics.ActionWeight();
            double inactiveWeight = GetInactiveWeight();
            if (GetZone() == possibleZone)
            {
                foreach (GameObject possibleFinalDestinationZone in possibleDestinationsAndPaths.Keys)
                {
                    double possibleBonusMovePointWeight = 0;
                    double possibleGuardZoneWeight = 0;
                    MissionSpecifics.ActionWeight possibleGuardAction = new MissionSpecifics.ActionWeight();
                    ZoneInfo possibleFinalDestinationZoneInfo = possibleFinalDestinationZone.GetComponent<ZoneInfo>();
                        
                    if (possibleDestinationsAndPaths[possibleFinalDestinationZone].terrainDanger > 0)  // Then don't even include possibleFinalDestinationZone as an option
                    {
                        continue;
                    }

                    if (possibleDestinationsAndPaths[possibleFinalDestinationZone].movementSpent > movePoints)
                    {
                        possibleBonusMovePointWeight = UnitIntel.bonusMovePointWeight[possibleDestinationsAndPaths[possibleFinalDestinationZone].movementSpent - movePoints];
                    }

                    if (MissionSpecifics.actionsWeightTable.ContainsKey("GUARD"))
                    {
                        foreach (MissionSpecifics.ActionWeight guardable in MissionSpecifics.actionsWeightTable["GUARD"])
                        {
                            if (possibleFinalDestinationZoneInfo.HasObjectiveToken(guardable.targetType))
                            {
                                GameObject objectiveToken = possibleFinalDestinationZoneInfo.GetObjectiveToken(guardable.targetType);
                                possibleGuardZoneWeight += guardable.weightFactor * MissionSpecifics.GetHeroProximityToObjectiveWeightMultiplier(possibleFinalDestinationZone, isPartialMove);
                                possibleGuardAction = guardable;
                            }
                        }
                    }

                    if (possibleGuardZoneWeight + possibleBonusMovePointWeight > guardZoneWeight + bonusMovePointWeight)
                    {
                        finalDestinationZone = possibleFinalDestinationZone;
                        guardZoneWeight = possibleGuardZoneWeight;
                        bonusMovePointWeight = possibleBonusMovePointWeight;
                        defaultGuardAction = possibleGuardAction;
                    }
                }
            }
            else
            {
                if (possibleDestinationsAndPaths[possibleZone].terrainDanger > 0)
                {
                    terrainDangerWeight = possibleDestinationsAndPaths[possibleZone].terrainDanger * UnitIntel.terrainDangerWeight; // TODO Reduce actionWeight by chance of unit dying for actions performed after move
                }
                if (possibleDestinationsAndPaths[possibleZone].movementSpent > movePoints)
                {
                    bonusMovePointWeight = UnitIntel.bonusMovePointWeight[possibleDestinationsAndPaths[possibleZone].movementSpent - movePoints];
                }
                if (MissionSpecifics.actionsWeightTable.ContainsKey("GUARD"))
                {
                    foreach (MissionSpecifics.ActionWeight guardable in MissionSpecifics.actionsWeightTable["GUARD"])
                    {
                        if (possibleZoneInfo.HasObjectiveToken(guardable.targetType))
                        {
                            GameObject objectiveToken = possibleZoneInfo.GetObjectiveToken(guardable.targetType);
                            guardZoneWeight += guardable.weightFactor * MissionSpecifics.GetHeroProximityToObjectiveWeightMultiplier(possibleZone, isPartialMove);
                        }
                    }
                }
            }

            double onlyMovingWeight = terrainDangerWeight + bonusMovePointWeight + guardZoneWeight;
            bool zoneAddedToAllPossibleActions = false;
            foreach (ActionProficiency actionProficiency in actionProficiencies)
            {
                double actionWeight = onlyMovingWeight;

                switch (actionProficiency.actionType)
                {
                    case "MELEE":
                        if (possibleZoneInfo.HasHeroes())
                        {
                            double averageWounds = GetAverageSuccesses(actionProficiency.proficiencyDice, availableRerolls) + martialArtsSuccesses;
                            averageWounds *= actionProficiency.actionMultiplier;
                            actionWeight += averageWounds * MissionSpecifics.actionsWeightTable["MELEE"][0].weightFactor;
                            if (frosty)  // Account for increasing difficult terrain of hero's zone
                            {
                                actionWeight += UnitIntel.increaseTerrainDifficultyWeight;
                            }
                            if (actionWeight > inactiveWeight)
                            {
                                allPossibleActions.Add(new UnitPossibleAction(this, MissionSpecifics.actionsWeightTable["MELEE"][0], actionProficiency, actionWeight, possibleZone, finalDestinationZone, null, possibleDestinationsAndPaths[finalDestinationZone]));
                                zoneAddedToAllPossibleActions = true;
                            }
                        }
                        break;
                    case "RANGED":
                        GameObject targetedZone = possibleZoneInfo.GetLineOfSightZoneWithHero();
                        if (targetedZone != null)
                        {
                            List<GameObject> dicePool = new List<GameObject>(actionProficiency.proficiencyDice);
                            if (possibleZoneInfo.elevation > targetedZone.GetComponent<ZoneInfo>().elevation)
                            {
                                dicePool.Add(possibleZoneInfo.environmentalDie);
                            }
                            if (pointBlankRerolls > 0 && targetedZone == possibleZoneInfo.gameObject)
                            {
                                availableRerolls += pointBlankRerolls;
                            }
                            int smokeHindrance = possibleZoneInfo.GetSmokeBetweenZones(targetedZone);

                            double averageWounds = GetAverageSuccesses(dicePool, availableRerolls) + marksmanSuccesses - actionZoneHindrance - smokeHindrance;
                            //Debug.Log("!!!GetPossibleActions for zone " + possibleZone.name + ",  averageWounds " + averageWounds.ToString() + "  GetAverageSuccesses(dicePool, rerolls=" + availableRerolls.ToString() + ") " + GetAverageSuccesses(dicePool, availableRerolls).ToString() + "  + marksmanSuccesses - actionZoneHindrance " + actionZoneHindrance.ToString());
                            if (averageWounds > 0)
                            {
                                averageWounds *= actionProficiency.actionMultiplier;
                            }
                            else
                            {
                                averageWounds += actionProficiency.actionMultiplier / 2;  // Can't inflict negative wounds, so just try to get this to a low positive number
                            }
                            actionWeight += averageWounds * MissionSpecifics.actionsWeightTable["RANGED"][0].weightFactor;
                            if (actionWeight > inactiveWeight)
                            {
                                allPossibleActions.Add(new UnitPossibleAction(this, MissionSpecifics.actionsWeightTable["RANGED"][0], actionProficiency, actionWeight, possibleZone, finalDestinationZone, targetedZone, possibleDestinationsAndPaths[finalDestinationZone]));
                                zoneAddedToAllPossibleActions = true;
                            }
                        }
                        break;
                    case "MANIPULATION":
                        if (MissionSpecifics.actionsWeightTable.ContainsKey("MANIPULATION"))
                        {
                            foreach (MissionSpecifics.ActionWeight manipulatable in MissionSpecifics.actionsWeightTable["MANIPULATION"])
                            {
                                if (manipulatable.targetType == "Grenade")
                                {
                                    if (grenade > 0)
                                    {
                                        GameObject grenadeTargetZone = possibleZoneInfo.GetLineOfSightZoneWithHero();  // TODO expand to return a list so can target closest hero
                                        if (grenadeTargetZone != null && grenadeTargetZone != possibleZone)  // Don't grenade your own zone
                                        {
                                            List<GameObject> dicePool = new List<GameObject>(actionProficiency.proficiencyDice);
                                            for (int i = 0; i < grenade; i++)
                                            {
                                                dicePool.Add(possibleZoneInfo.environmentalDie);
                                            }
                                            double averageAutoWounds = GetAverageSuccesses(dicePool, availableRerolls);
                                            List<GameObject> lineOfSight = new List<GameObject>() { GetZone() };
                                            lineOfSight.AddRange(possibleZoneInfo.GetSightLineWithZone(grenadeTargetZone));
                                            if (lineOfSight == null)
                                            {
                                                Debug.LogError("ERROR! Unit " + gameObject.name + " in " + possibleZoneInfo.gameObject.name + " isn't able to GetSightLineWithZone with " + grenadeTargetZone.name);
                                            }
                                            int requiredSuccesses = lineOfSight.IndexOf(grenadeTargetZone) + actionZoneHindrance;
                                            double chanceOfSuccess = GetChanceOfSuccess(requiredSuccesses, actionProficiency.proficiencyDice, availableRerolls);
                                            actionWeight += averageAutoWounds * manipulatable.weightFactor * chanceOfSuccess;
                                            //Debug.Log("!!!Weighing grenade throw, actionWeight " + actionWeight.ToString() + " += " + averageAutoWounds.ToString() + " * " + manipulatable.Item3.ToString() + " * " + chanceOfSuccess.ToString());
                                            if (frosty)  // Account for increasing difficult terrain of hero's zone
                                            {
                                                actionWeight += UnitIntel.increaseTerrainDifficultyWeight;
                                            }
                                            actionWeight += grenadeTargetZone.GetComponent<ZoneInfo>().GetUnitsInfo().Count * grenade * UnitIntel.terrainDangeringFriendlies;
                                            if (actionWeight > inactiveWeight)
                                            {
                                                allPossibleActions.Add(new UnitPossibleAction(this, manipulatable, actionProficiency, actionWeight, possibleZone, finalDestinationZone, grenadeTargetZone, possibleDestinationsAndPaths[finalDestinationZone]));
                                                zoneAddedToAllPossibleActions = true;
                                            }
                                        }
                                    }
                                }
                                else if (possibleZoneInfo.HasObjectiveToken(manipulatable.targetType))
                                {
                                    int requiredSuccesses = manipulatable.requiredSuccesses + actionZoneHindrance - (manipulatable.targetType == "Bomb" ? munitionSpecialist : 0);
                                    double chanceOfSuccess = GetChanceOfSuccess(requiredSuccesses, actionProficiency.proficiencyDice, availableRerolls);
                                    actionWeight += chanceOfSuccess * manipulatable.weightFactor;
                                    if (actionWeight > inactiveWeight)
                                    {
                                        allPossibleActions.Add(new UnitPossibleAction(this, manipulatable, actionProficiency, actionWeight, possibleZone, finalDestinationZone, null, possibleDestinationsAndPaths[finalDestinationZone]));
                                        zoneAddedToAllPossibleActions = true;
                                    }
                                }
                            }
                        }
                        break;
                    case "THOUGHT":
                        if (MissionSpecifics.actionsWeightTable.ContainsKey("THOUGHT"))
                        {
                            foreach (MissionSpecifics.ActionWeight thoughtable in MissionSpecifics.actionsWeightTable["THOUGHT"])
                            {
                                if (possibleZoneInfo.HasObjectiveToken(thoughtable.targetType))
                                {
                                    int requiredSuccesses = thoughtable.requiredSuccesses + actionZoneHindrance;
                                    double chanceOfSuccess = GetChanceOfSuccess(requiredSuccesses, actionProficiency.proficiencyDice, availableRerolls);
                                    actionWeight += chanceOfSuccess * thoughtable.weightFactor;
                                    //Debug.Log("Possible THOUGHT action for " + gameObject.name + " in " + possibleZone.name + "  with chanceOfSuccess: " + chanceOfSuccess.ToString() + "  onlyMovingWeight: " + onlyMovingWeight.ToString() + "  actionWeight: " + actionWeight.ToString() + " which should be greater than inactiveWeight: " + inactiveWeight.ToString());
                                    if (actionWeight > inactiveWeight)
                                    {
                                        allPossibleActions.Add(new UnitPossibleAction(this, thoughtable, actionProficiency, actionWeight, possibleZone, finalDestinationZone, null, possibleDestinationsAndPaths[finalDestinationZone]));
                                        zoneAddedToAllPossibleActions = true;
                                    }
                                }
                            }
                        }
                        break;
                }
            }
            if (!zoneAddedToAllPossibleActions)
            {
                if (onlyMovingWeight > inactiveWeight)
                {
                    allPossibleActions.Add(new UnitPossibleAction(this, defaultGuardAction, moveActionProficiency, onlyMovingWeight, possibleZone, finalDestinationZone, null, possibleDestinationsAndPaths[finalDestinationZone]));
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

        //string debugString = "GetMostValuableActionWeight for " + transform.tag;
        //foreach (KeyValuePair<GameObject, MovementPath> zoneMove in possibleDestinations)
        //{
        //    debugString += "   " + zoneMove.Key.name + " with " + zoneMove.Value.movementSpent.ToString() + " movePoints";
        //}
        //debugString += "\n";
        //foreach (UnitPossibleAction unitPossibleAction in allPossibleUnitActions)
        //{
        //    debugString += "   " + unitPossibleAction.actionProficiency.actionType + " in " + unitPossibleAction.actionZone.name + " with weight " + unitPossibleAction.actionWeight.ToString();
        //}
        //Debug.Log(debugString);

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

    public double GetInactiveWeight()  // Weight from standing still and just guarding
    {
        double standingStillWeight = 0;
        if (MissionSpecifics.actionsWeightTable.ContainsKey("GUARD"))
        {
            ZoneInfo unitZoneInfo = GetZone().GetComponent<ZoneInfo>();
            foreach (MissionSpecifics.ActionWeight guardable in MissionSpecifics.actionsWeightTable["GUARD"])
            {
                if (unitZoneInfo.HasObjectiveToken(guardable.targetType))
                {
                    standingStillWeight += guardable.weightFactor;
                }
            }
        }
        return standingStillWeight;
    }

    private Tuple<GameObject, double> GetPartialMoveAndWeight(Dictionary<GameObject, MovementPath> reachableDestinations, int iterations = 1)
    {
        double mostValuableActionWeight = GetInactiveWeight();
        GameObject chosenDestination = null;
        Dictionary<GameObject, MovementPath> reachableDestinationsWithoutBonusMovePoints = new Dictionary<GameObject, MovementPath>();

        foreach (GameObject zone in reachableDestinations.Keys)
        {
            if (reachableDestinations[zone].movementSpent <= movePoints)
            {
                reachableDestinationsWithoutBonusMovePoints[zone] = reachableDestinations[zone];
                reachableDestinationsWithoutBonusMovePoints[zone].movementSpent = 0;  // Don't want to account for previously spent movement so set it to 0.
            }
        }

        Dictionary<GameObject, MovementPath> reachableAndFutureDestinations = new Dictionary<GameObject, MovementPath>(reachableDestinationsWithoutBonusMovePoints);
        foreach (GameObject reachableZone in reachableDestinationsWithoutBonusMovePoints.Keys)
        {
            Dictionary<GameObject, MovementPath> nextPossibleDestinations = GetPossibleDestinations(reachableZone, new Dictionary<GameObject, MovementPath>(reachableDestinationsWithoutBonusMovePoints), new HashSet<GameObject>(reachableDestinationsWithoutBonusMovePoints.Keys));
            List<UnitPossibleAction> futurePossibleActions = GetPossibleActions(nextPossibleDestinations, true);  // isPartialMove = true

            if (futurePossibleActions != null && futurePossibleActions.Count > 0)
            {
                foreach (UnitPossibleAction unitAction in futurePossibleActions)
                {
                    // If an action with a moving target (the target being a hero)
                    if (unitAction.actionProficiency.actionType == "MELEE" || unitAction.actionProficiency.actionType == "RANGED" || (unitAction.actionProficiency.actionType == "MANIPULATE" && unitAction.missionSpecificAction.targetType == "Grenade"))
                    {
                        unitAction.actionWeight *= UnitIntel.partialMoveWeight[0];
                    }

                    unitAction.actionWeight *= UnitIntel.partialMoveWeight[0];  // Reduce weight for the fact these actions can't be completed until a second activation.
                    if (unitAction.actionWeight > mostValuableActionWeight)
                    {
                        mostValuableActionWeight = unitAction.actionWeight;
                        chosenDestination = reachableZone;
                        //Debug.Log("chosenDestination: " + chosenDestination.name + "  unitAction.actionProficiency.actionType: " + unitAction.actionProficiency.actionType + "   unitAction.actionWeight (after being reduced to 25%): " + unitAction.actionWeight.ToString());
                    }
                }
            }

            if (chosenDestination == null)
            {
                foreach (KeyValuePair<GameObject, MovementPath> aPossibleDestination in nextPossibleDestinations)
                {
                    if (!reachableAndFutureDestinations.ContainsKey(aPossibleDestination.Key))
                    {
                        reachableAndFutureDestinations.Add(aPossibleDestination.Key, aPossibleDestination.Value);
                    }
                }
            }
        }
        if (chosenDestination != null)
        {
            return new Tuple<GameObject, double>(chosenDestination, mostValuableActionWeight);
        }
        //else if (iterations < 5)
        //{
        //    Tuple<GameObject, double> nextPartialMoveAndWeight = GetPartialMoveAndWeight(reachableAndFutureDestinations, iterations + 1);  // TODO Fix so it's not thinking a futureDestination is a reachable one, else you get "The given key was not present in the dictionary"
        //    return new Tuple<GameObject, double>(nextPartialMoveAndWeight.Item1, nextPartialMoveAndWeight.Item2 * .25);
        //}
        return new Tuple<GameObject, double>(null, 0);
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
        if (movementPath.zones.Count > 1)
        {
            Vector3 finalCoordinates = movementPath.zones[movementPath.zones.Count - 1].GetComponent<ZoneInfo>().GetAvailableUnitSlot().transform.position;
            float secondsToDelayBeforeCameraMove = animate.PostionCameraBeforeCameraMove(transform.position, finalCoordinates);
            yield return new WaitForSecondsRealtime(1);  // Pause with camera on unit before move
            StartCoroutine(animate.MoveCameraUntilOnscreen(transform.position, finalCoordinates, secondsToDelay: secondsToDelayBeforeCameraMove));
        }
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
            if (!IsActive())
            {
                yield return StartCoroutine(animate.FadeObjects(new List<GameObject>() { gameObject }, 1, fadedAlpha));
                break;
            }
        }
        if (destination != null)
        {
            ZoneInfo destinationInfo = destination.GetComponent<ZoneInfo>();
            transform.SetParent(destinationInfo.GetAvailableUnitSlot().transform);
            if (movementPath.movementSpent > movePoints && IsActive())  // TODO Reduce bonusMovePointsRemaining based on how far unit gets before dying, but only have movementSpent for destination
            {
                UnitIntel.bonusMovePointsRemaining -= movementPath.movementSpent - movePoints;
            }
            yield return new WaitForSecondsRealtime(1);  // Pause with camera on unit after move
        }
        yield return 0;
    }

    IEnumerator AnimateMovement(GameObject origin, GameObject destination)
    {
        GameObject destinationUnitSlot = destination.GetComponent<ZoneInfo>().GetAvailableUnitSlot();
        transform.SetParent(GameObject.FindGameObjectWithTag("AnimationContainer").transform);  // Needed so unit animating is always drawn last (above everything it might pass over).
        yield return StartCoroutine(animate.MoveObjectOverTime(new List<GameObject>() { transform.gameObject }, transform.position, destinationUnitSlot.transform.position));
        yield return 0;
    }

    IEnumerator PerformAction(UnitPossibleAction unitTurn)
    {
        int actionSuccesses = 0;
        int requiredSuccesses;
        GameObject currentZone = GetZone();
        ZoneInfo currentZoneInfo = currentZone.GetComponent<ZoneInfo>();
        int currentZoneHindrance = currentZoneInfo.GetCurrentHindrance(gameObject);
        int availableRerolls = currentZoneInfo.GetSupportRerolls(gameObject);

        if (!animate.IsPointOnScreen(transform.position, 0))
        {
            animate.mainCamera.transform.position = new Vector3(transform.position.x, transform.position.y, animate.mainCamera.transform.position.z);
            yield return new WaitForSecondsRealtime(1);  // Pause after camera jump to unit but before action
        }

        switch (unitTurn.actionProficiency.actionType)
        {
            case "MELEE":
                for (int i = 0; i < unitTurn.actionProficiency.actionMultiplier; i++)
                {
                    if (!IsActive())  // If Counterattacked and killed, stop attacking
                    {
                        yield break;
                    }

                    GameObject targetedHero = currentZoneInfo.GetRandomHero();  // TODO mirror unitTurn.targetedZone like RANGED for when Reach trait comes into play
                    actionSuccesses = RollAndReroll(unitTurn.actionProficiency.proficiencyDice, availableRerolls);
                    if (actionSuccesses > 0)
                    {
                        actionSuccesses += martialArtsSuccesses;
                    }
                    yield return StartCoroutine(animate.MeleeAttack(gameObject, targetedHero, actionSuccesses));
                    if (fiery)
                    {
                        yield return StartCoroutine(currentZoneInfo.AddEnvironTokens(new EnvironTokenSave("Flame", 1, false, true)));
                    }
                    if (frosty)
                    {
                        yield return StartCoroutine(currentZoneInfo.AddEnvironTokens(new EnvironTokenSave("Frost", 1, false, true)));
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

                    int smokeHindrance = currentZoneInfo.GetSmokeBetweenZones(unitTurn.targetedZone);

                    for (int i = 0; i < unitTurn.actionProficiency.actionMultiplier; i++)
                    {
                        if (!IsActive())  // If Retaliated and killed, stop attacking
                        {
                            yield break;
                        }

                        GameObject targetedHero = targetedLineOfSightZoneInfo.GetRandomHero();
                        actionSuccesses = RollAndReroll(dicePool, availableRerolls);
                        if (actionSuccesses > 0)
                        {
                            actionSuccesses += marksmanSuccesses;
                        }
                        actionSuccesses -= currentZoneHindrance + smokeHindrance;
                        if (actionSuccesses < 0)
                        {
                            actionSuccesses = 0;
                        }

                        yield return StartCoroutine(animate.RangedAttack(gameObject, targetedHero, actionSuccesses));
                    }
                    if (fiery)  // Not sure these ever get triggered as the villains with these traits don't have a ranged attack, but the Frost/Fire text: During an attack or after an explosion...
                    {
                        yield return StartCoroutine(targetedLineOfSightZoneInfo.AddEnvironTokens(new EnvironTokenSave("Flame", 1, false, true)));
                    }
                    if (frosty)
                    {
                        yield return StartCoroutine(targetedLineOfSightZoneInfo.AddEnvironTokens(new EnvironTokenSave("Frost", 1, false, true)));
                    }
                }
                else
                {
                    Debug.LogError("ERROR! RANGED action was performed while targetedLineOfSightZone was null, so henchman just wasted its action wildly firing its gun into the air.");
                }
                break;
            case "MANIPULATION":
                if (unitTurn.missionSpecificAction.targetType == "Grenade")
                {
                    if (grenade > 0)
                    {
                        List<GameObject> lineOfSight = new List<GameObject>() { GetZone() };
                        lineOfSight.AddRange(currentZoneInfo.GetSightLineWithZone(unitTurn.targetedZone));
                        requiredSuccesses = lineOfSight.IndexOf(unitTurn.targetedZone) + currentZoneHindrance;
                        actionSuccesses = RollAndReroll(unitTurn.actionProficiency.proficiencyDice, availableRerolls + MissionSpecifics.GetAttackRollBonus(), requiredSuccesses);
                        GameObject targetedZone;
                        if (actionSuccesses >= requiredSuccesses)
                        {
                            targetedZone = unitTurn.targetedZone;
                        }
                        else
                        {
                            targetedZone = lineOfSight[actionSuccesses];
                        }
                        ZoneInfo targetedZoneInfo = targetedZone.GetComponent<ZoneInfo>();
                        yield return StartCoroutine(animate.ThrowGrenade(transform.position, targetedZone.transform.position));
                        yield return StartCoroutine(targetedZoneInfo.IncreaseTerrainDangerTemporarily(grenade));
                        yield return StartCoroutine(targetedZoneInfo.AddEnvironTokens(new EnvironTokenSave("Frost", 1, false, true)));
                    }
                }
                else
                {
                    requiredSuccesses = unitTurn.missionSpecificAction.requiredSuccesses + currentZoneHindrance - (unitTurn.missionSpecificAction.targetType == "Bomb" ? munitionSpecialist : 0);
                    actionSuccesses = RollAndReroll(unitTurn.actionProficiency.proficiencyDice, availableRerolls, requiredSuccesses);
                    yield return StartCoroutine(unitTurn.missionSpecificAction.actionCallback(gameObject, null, actionSuccesses, requiredSuccesses));
                }
                break;
            case "THOUGHT":
                requiredSuccesses = unitTurn.missionSpecificAction.requiredSuccesses + currentZoneHindrance;
                actionSuccesses = RollAndReroll(unitTurn.actionProficiency.proficiencyDice, availableRerolls, requiredSuccesses);
                yield return StartCoroutine(unitTurn.missionSpecificAction.actionCallback(gameObject, null, actionSuccesses, requiredSuccesses));
                break;
        }

        string debugString = tag + " in " + unitTurn.actionZone.name + " performed " + unitTurn.actionProficiency.actionType;
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

    private int RollAndReroll(List<GameObject> dicePool, int rerolls, int requiredSuccesses)
    {
        int rolledSuccesses = 0;
        List<ActionResult> currentActionResults = new List<ActionResult>();
        string debugString = "RollAndReroll for unit " + gameObject.name + " with " + requiredSuccesses.ToString() + " requiredSuccesses. ";

        // Separate dice results by color for freeRerolls
        Dictionary<string, List<ActionResult>> actionResultsByColor = new Dictionary<string, List<ActionResult>>();
        foreach (GameObject die in dicePool)
        {
            Dice dieInfo = die.GetComponent<Dice>();
            ActionResult currentActionResult = new ActionResult(dieInfo, dieInfo.Roll());
            rolledSuccesses += currentActionResult.successes;
            if (actionResultsByColor.ContainsKey(dieInfo.color))
            {
                actionResultsByColor[dieInfo.color].Add(currentActionResult);
            }
            else
            {
                actionResultsByColor[dieInfo.color] = new List<ActionResult>() { currentActionResult };
            }
        }

        // Apply freeRerolls (if needed)
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

            if (freeRerolls > 0 && rolledSuccesses < requiredSuccesses)
            {
                dieResults.Sort((x, y) => (y.die.averageSuccesses - y.successes).CompareTo(x.die.averageSuccesses - x.successes));  // Sorts from greatest below average to most above average

                for (int i = 0; i < freeRerolls; i++)
                {
                    if (dieResults[i].successes >= dieResults[i].die.averageSuccesses)
                    {
                        // TODO If rerolls == 0 add dieResults[i] to another list of worst results by color with leftover freeRerolls, sort that list like above, and reroll the worst of the above average results until rolledSuccesses >= requiredSuccesses
                        break;  // Exit freeRerolls loop if none of the dice rolled below average
                    }
                    else
                    {
                        debugString += "Using freeReroll to change from " + dieResults[i].successes.ToString() + " to ";
                        rolledSuccesses -= dieResults[i].successes;
                        dieResults[i] = new ActionResult(dieResults[i].die, dieResults[i].die.Roll());
                        rolledSuccesses += dieResults[i].successes;
                        debugString += dieResults[i].successes.ToString();

                        if (rolledSuccesses >= requiredSuccesses)
                        {
                            break;
                        }
                    }
                }
            }
            currentActionResults.AddRange(dieResults);
        }

        // Apply rerolls
        for (int i = 0; i < rerolls; i++)
        {
            if (rolledSuccesses >= requiredSuccesses)
            {
                break;
            }
            debugString += "  On my " + (i + 1).ToString() + " reroll: ";
            int totalDiceRerolled = 0;
            for (int j = 0; j < currentActionResults.Count; j++)  // Reroll each die still below its average
            {
                if (currentActionResults[i].successes < currentActionResults[i].die.averageSuccesses)
                {
                    debugString += " Changing " + currentActionResults[i].successes.ToString() + " to ";
                    rolledSuccesses -= currentActionResults[i].successes;
                    currentActionResults[i] = new ActionResult(currentActionResults[i].die, currentActionResults[i].die.Roll());
                    rolledSuccesses += currentActionResults[i].successes;
                    debugString += currentActionResults[i].successes.ToString();
                    totalDiceRerolled++;
                }
            }
            if (totalDiceRerolled == 0)
            {
                currentActionResults.Sort((x, y) => (y.die.averageSuccesses - y.successes).CompareTo(x.die.averageSuccesses - x.successes));  // Sorts from greatest below average to most above average
                debugString += " Changing " + currentActionResults[i].successes.ToString() + " to ";
                rolledSuccesses -= currentActionResults[i].successes;
                currentActionResults[i] = new ActionResult(currentActionResults[i].die, currentActionResults[i].die.Roll());
                rolledSuccesses += currentActionResults[i].successes;
                debugString += currentActionResults[i].successes.ToString();
            }
        }

        // Debug final roll
        debugString += "\nFinal roll: ";
        foreach (ActionResult myActionResult in currentActionResults)
        {
            debugString += myActionResult.successes.ToString() + ", ";
        }
        Debug.Log(debugString);
        return rolledSuccesses;
    }

    private int RollAndReroll(List<GameObject> dicePool, int rerolls)  // Only Melee and Ranged attacks use this method version without requiredSuccesses
    {
        int rolledSuccesses = 0;
        rerolls += MissionSpecifics.GetAttackRollBonus();
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
            for (int j = 0; j < currentActionResults.Count; j++)  // Reroll each die still below its average
            {
                if (currentActionResults[j].successes < currentActionResults[j].die.averageSuccesses)
                {
                    debugString += " Changing " + currentActionResults[j].successes.ToString() + " to ";
                    currentActionResults[j] = new ActionResult(currentActionResults[j].die, currentActionResults[j].die.Roll());
                    debugString += currentActionResults[j].successes.ToString();
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
        if (lifePoints > lifePointsMax)
        {
            lifePoints = lifePointsMax;
        }
        else if (lifePoints < 0)
        {
            lifePoints = 0;
        }

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
            this.GetComponent<CanvasGroup>().alpha = fadedAlpha;
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
