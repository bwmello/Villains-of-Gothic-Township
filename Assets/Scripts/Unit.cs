using System;  // for [Serializable]
using System.Collections;
using System.Collections.Generic;
using System.Linq;  // For converting array.ToList()
using UnityEngine;
using UnityEngine.UI;  // For button
using TMPro;  // for TMP_Text to update the henchmen quantity from UnitRows
using Shapes2D;  // for changing color to blue if isHeroAlly

public class Unit : MonoBehaviour
{
    readonly System.Random random = new System.Random();
    private Animate animate;
    public float fadedAlpha = .3f;  // Public so can be used by ZoneInfo.cs when terrain danger increases
    public bool isHeroAlly = false;

    public int lifePoints = 1;
    public int lifePointsMax = 1;
    public int defense;
    public int woundShields = -1;  // Adds to defense. 0 to 2 randomly added on Instantiation. -1 means not yet initialized
    public int reinforcementCost = 1;
    public int ignoreRangedWounds = 0;  // TODO implement for GCPDNIGHTSTICK
    public int protectedByAllies = 0;  // TODO for PISTOLS, popup prompt if removing protected Unit while they have this many allies in their zone OR auto redirect attack and alert player
    public int sacrifice = 0;  // TODO implement for GCPDPISTOL

    public int size = 1;
    public int menace = 1;
    public int supportRerolls = 0;

    public int movePoints;
    public int ignoreTerrainDifficulty = 0;
    public int ignoreElevation = 0;  // TODO Also ignores this many wounds caused by a fall
    public int ignoreSize = 0;
    public int ignoreMenace = 0;  // TODO implment for DRONEs
    public int wallBreaker = 0;

    public int martialArtsSuccesses = 0;
    public int reach = 0;  // TODO Reach (MUDMAN and BIRDMAN's Gang) Melee attacks can target heroes/miniatures this far away with LoS
    // public int berserk = 0;  // Berserk (OLLYGATOR) If lifePoints <= lifePointsMax/2, add this many white dice to each attack
    public int electricity = 0;  // TODO implement for GUARD
    public int shackle = 0;  // TODO (MUDMAN) Instead of melee damage (still show wounds (or proxy) for overcoming defense) inflict this many shackle, reducing all target's successes by [shackle] until they perform complex MANIPULATION vs [shackle] to remove it
    public int circularStrike = 0;  // TODO for CHAINS, if hero removed after MELEE with another hero in that zone, popup prompt saying up to this many additional successes carry over
    public int counterAttack = 0;  // (Ignore, player resolved) if hero moves into space with SHOTGUN, reminder that after melee attack against SHOTGUN is resolved, SHOTGUN gets free melee attack vs hero with number of yellow dice = counterattack

    public int marksmanSuccesses = 0;
    public int pointBlankRerolls = 0;
    public int burstCarryOver = 0;  // After target neutralized, remaining successes can be used against [burstCarryOver] other visible targets
    // public int counterRangedAttack = 0;  // Retaliation (QUIZMAN's Gang w/ Handgun) if hero not in unit's space, reminder that after ranged attack against unit is resolved, unit gets free ranged attack vs hero with this number of yellow dice

    public int munitionSpecialist = 0;

    public bool flying = false;  // TODO implement for DRONEs
    public bool gasImmunity = false;
    public bool frosty = false;  // OTTERPOP's ability to ignore frost and cryogenic tokens and spawn frost tokens  // Frost (OTTERPOP) During attack or explosion from unit, place Frost Token in targeted area, which increases difficult terrain by number of Frost Tokens (except Mr. Freeze)
    public bool fiery = false;  // FIREGUY's ability to place flame tokens after attack/explosion and be immune to flame tokens

    // Actions
    public int grenade = 0;  // Grenade (OTTERPOP) Complex manipulation to trigger this level of explosion in targeted area with LoS w/ difficulty = distance, failure has explosion triggered at distance equal to number of successes
    // public int blast = 0;  // Blast (OTTERPOP) auto manipulation to trigger this level of explosion in unit's area and adjacent area with LoS
    //public int moveCommand = 0;  // Tactician (SKULLFACE, POLIKILLIAN) auto thought to grant ally free move + [moveCommand]. Takes up action, so seems dumb.

    // Remainder Harmless, Reduced Mobility, Attraction, Shackle, Hacking, Gas Immunity, Misfortune, Horror, Regeneration, Fly, Impenetrable Defense, Untouchable, Burst, Luck, Flame, Toxic Gas, Poison, Body Guard, Investigation, Sneak Attack, Imaginary Friend


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

    void Start()
    {
        ConfigureColor();
    }

    public bool IsActive()
    {
        if (lifePoints > 0)
        {
            return true;
        }
        return false;
    }

    public bool IsVillain()
    {
        if (lifePointsMax > 1)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public bool isClickable = false;
    public void SetIsClickable(bool shouldMakeClickable, bool shouldConfigureColor = true)  // If called directly (not through ConfigureClickAndDragability(), you shouldConfigureColor as well
    {
        //if  (shouldMakeClickable != isClickable) {
        if (IsVillain())
        {
            foreach (Button button in transform.GetComponentsInChildren<Button>())
            {
                //button.enabled = shouldMakeClickable;
                button.interactable = shouldMakeClickable;
            }
        }
        else
        {
            //gameObject.GetComponent<Button>().enabled = shouldMakeClickable;
            gameObject.GetComponent<Button>().interactable = shouldMakeClickable;
        }
        isClickable = shouldMakeClickable;

        if (shouldConfigureColor)
        {
            ConfigureColor();
        }
    }

    public bool isDraggable = false;  // Should this exist both here and in Draggable script?
    public void SetIsDraggable(bool shouldMakeDraggable, bool shouldConfigureColor = true)  // If called directly (not through ConfigureClickAndDragability(), you shouldConfigureColor as well
    {
        if (!IsVillain())  // && shouldMakeDraggable != isDraggable  // Villains are never draggable
        {
            GetComponent<BoxCollider2D>().enabled = shouldMakeDraggable;  // Doesn't prevent from being dragged but will stop/start registering OnCollisionEnter/Exit events
            GetComponent<Draggable>().isDraggable = shouldMakeDraggable;
            isDraggable = shouldMakeDraggable;
        }

        if (shouldConfigureColor)
        {
            ConfigureColor();
        }
    }

    public void ConfigureClickAndDragability()
    {
        switch (MissionSpecifics.currentPhase)
        {
            //case "Setup":
            case "Hero":
                SetIsClickable(true, false);
                if (isHeroAlly)
                {
                    SetIsDraggable(true, false);
                }
                else
                {
                    SetIsDraggable(false, false);
                }
                break;
            case "Villain":
                SetIsClickable(false, false);
                SetIsDraggable(false, false);
                break;
        }
        ConfigureColor();
    }

    public void ConfigureColor()
    {
        if (!IsVillain())
        {
            //Button button = GetComponent<Button>();
            //var colorBlock = button.colors;
            //colorBlock.disabledColor = new Color(1f, 1f, 1f);
            //button.colors = colorBlock;
            Shape shape = GetComponent<Shape>();
            if (!isHeroAlly)
            {
                shape.settings.fillColor = (isClickable || isDraggable) ? new Color(1f, .85f, .85f) : new Color(.8f, .45f, .45f);
            }
            else
            {
                if (isClickable || isDraggable)
                {
                    shape.settings.fillColor = new Color(.85f, .85f, 1f);
                }
                else
                {
                    shape.settings.fillColor = new Color(.45f, .45f, .8f);

                }
                //shape.settings.fillColor = (isClickable || isDraggable) ? new Color(.85f, .85f, 1f) : new Color(.45f, .45f, .8f);
            }
            //shape.ComputeAndApply();  // According to Shapes2D doc, there may be flicker without this call but I haven't noticed any difference
        }
        else
        {
            foreach (Shape villainShape in GetComponentsInChildren<Shape>())
            {
                villainShape.settings.fillColor = (isClickable || isDraggable) ? new Color(1f, .85f, .85f) : new Color(.85f, .5f, .5f);
            }
        }
    }

    public void TokenClicked()
    {
        if (!gameObject.GetComponent<Draggable>().isDragging)
        {
            switch (MissionSpecifics.currentPhase)
            {
                case "Setup":
                    if (isHeroAlly)
                    {
                        Destroy(gameObject);
                    }
                    break;
                default:
                    if (lifePoints > 0)
                    {
                        ModifyLifePoints(-1);
                    }
                    else
                    {
                        ModifyLifePoints(1);
                    }
                    break;
            }
        }
    }

    public void ModifyLifePoints(int difference)
    {
        float initialAlpha = this.GetComponent<CanvasGroup>().alpha;
        lifePoints += difference;
        if (lifePoints > lifePointsMax)
        {
            lifePoints = lifePointsMax;
        }
        else if (lifePoints < 0)
        {
            lifePoints = 0;
        }

        if (IsVillain())  // Will be a VillainRow with a UnitNumber object instead of just a token button like Unit.
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

        if (this.GetComponent<CanvasGroup>().alpha != initialAlpha)  // If unit was alive and is now dead or vice versa
        {
            if (IsActive())
            {
                MissionSpecifics.UnitResuscitated(gameObject);
            }
            else
            {
                MissionSpecifics.UnitKilled(gameObject);
            }
        }
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

            if (potentialZoneInfo.GetCurrentOccupancy() + size > potentialZoneInfo.maxOccupancy)
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
        public List<GameObject> priorityTargets = new List<GameObject>();

        public UnitPossibleAction(Unit theUnit, MissionSpecifics.ActionWeight unitMissionSpecificAction, ActionProficiency unitActionProficiency, double unitActionWeight, GameObject unitActionZone, GameObject unitFinalDestinationZone, MovementPath unitPathTaken = null, GameObject unitTargetedZone = null, List<GameObject> unitPriorityTargets = null)
        {
            myUnit = theUnit;
            missionSpecificAction = unitMissionSpecificAction;
            actionProficiency = unitActionProficiency;
            actionWeight = unitActionWeight;
            actionZone = unitActionZone;
            finalDestinationZone = unitFinalDestinationZone;
            pathTaken = unitPathTaken;
            targetedZone = unitTargetedZone;
            priorityTargets = unitPriorityTargets;
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
                        List<GameObject> meleeTargetableZones = possibleZoneInfo.GetZonesWithTargetsWithinLinesOfSight(reach);
                        if (meleeTargetableZones.Count > 0)
                        {
                            double averageWounds = GetAverageSuccesses(actionProficiency.proficiencyDice, availableRerolls) + martialArtsSuccesses;
                            averageWounds *= actionProficiency.actionMultiplier;
                            if (frosty)  // Account for increasing difficult terrain of targets's zone
                            {
                                actionWeight += UnitIntel.increaseTerrainDifficultyWeight;  // Only counted if there is a target
                            }

                            List<MissionSpecifics.ActionWeight> highPriorityTargets = new List<MissionSpecifics.ActionWeight>();
                            for (int i = 2; i < MissionSpecifics.actionsWeightTable["MELEE"].Count; i++)
                            {
                                highPriorityTargets.Add(MissionSpecifics.actionsWeightTable["MELEE"][i]);
                            }

                            List<(GameObject, double)> priorityTargetsAndWeights = new List<(GameObject, double)>();
                            foreach (GameObject targetableZone in meleeTargetableZones)
                            {
                                ZoneInfo targetableZoneInfo = targetableZone.GetComponent<ZoneInfo>();
                                foreach (GameObject hero in targetableZoneInfo.GetTargetableHeroes())
                                {
                                    double targetHeroWeight = averageWounds * MissionSpecifics.actionsWeightTable["MELEE"][0].weightFactor + actionWeight;
                                    if (hero.GetComponent<Hero>().canCounterMeleeAttacks)
                                    {
                                        targetHeroWeight -= UnitIntel.provokingCounterAttackWeight / defense;
                                    }
                                    priorityTargetsAndWeights.Add((hero, targetHeroWeight));
                                }
                                foreach (GameObject heroAlly in targetableZoneInfo.GetTargetableHeroAllies())
                                {
                                    bool isHighPriorityTarget = false;
                                    foreach (MissionSpecifics.ActionWeight highPriorityTarget in highPriorityTargets)
                                    {
                                        if (heroAlly.CompareTag(highPriorityTarget.targetType))
                                        {
                                            isHighPriorityTarget = true;
                                            priorityTargetsAndWeights.Add((heroAlly, averageWounds * highPriorityTarget.weightFactor + actionWeight));
                                            break;
                                        }
                                    }
                                    if (!isHighPriorityTarget)
                                    {
                                        priorityTargetsAndWeights.Add((heroAlly, averageWounds * MissionSpecifics.actionsWeightTable["MELEE"][1].weightFactor + actionWeight));
                                    }
                                }
                            }
                            priorityTargetsAndWeights.Sort((x, y) => y.Item2.CompareTo(x.Item2));  // Sorts by most valuable weight

                            actionWeight = priorityTargetsAndWeights[0].Item2;
                            if (priorityTargetsAndWeights.Count > 1 && circularStrike > 0)
                            {
                                actionWeight += UnitIntel.additionalTargetsForAdditionalAttacksWeight;
                            }
                            else if (priorityTargetsAndWeights.Count == 1 && actionProficiency.actionMultiplier > 1)
                            {
                                actionWeight -= actionProficiency.actionMultiplier * UnitIntel.additionalTargetsForAdditionalAttacksWeight;  // Wounds from additional attacks not guaranteed
                            }

                            //string priorityTargetsDebugString = "PriorityTargetsDebugString for unit " + gameObject.name;
                            //foreach ((GameObject, double) priorityTargetAndWeight in priorityTargetsAndWeights)  // Doesn't include change to actionWeight above
                            //{
                            //    priorityTargetsDebugString += "    Targeting " + priorityTargetAndWeight.Item1.name + " with weight: " + priorityTargetAndWeight.Item2.ToString();
                            //}
                            //Debug.Log(priorityTargetsDebugString);

                            if (actionWeight > inactiveWeight)
                            {
                                List<GameObject> priorityTargets = priorityTargetsAndWeights.Select(x => x.Item1).ToList();
                                if (priorityTargetsAndWeights[0].Item1.TryGetComponent<Hero>(out var tempHeroComponent))  // tempHeroComponent isn't used, but no other way to use TryGetComponent
                                {
                                    allPossibleActions.Add(new UnitPossibleAction(this, MissionSpecifics.actionsWeightTable["MELEE"][0], actionProficiency, actionWeight, possibleZone, finalDestinationZone, possibleDestinationsAndPaths[finalDestinationZone], null, priorityTargets));  // TargetZone not really needed with priorityTargets list
                                }
                                else
                                {
                                    allPossibleActions.Add(new UnitPossibleAction(this, MissionSpecifics.actionsWeightTable["MELEE"][1], actionProficiency, actionWeight, possibleZone, finalDestinationZone, possibleDestinationsAndPaths[finalDestinationZone], null, priorityTargets));
                                }
                                zoneAddedToAllPossibleActions = true;
                            }
                        }
                        break;
                    case "RANGED":
                        List<GameObject> rangedTargetableZones = possibleZoneInfo.GetZonesWithTargetsWithinLinesOfSight();
                        if (rangedTargetableZones.Count > 0)
                        {
                            if (frosty)  // Account for increasing difficult terrain of targets's zone
                            {
                                actionWeight += UnitIntel.increaseTerrainDifficultyWeight;  // Only counted if there is a target
                            }

                            List<MissionSpecifics.ActionWeight> highPriorityTargets = new List<MissionSpecifics.ActionWeight>();
                            for (int i = 2; i < MissionSpecifics.actionsWeightTable["RANGED"].Count; i++)
                            {
                                highPriorityTargets.Add(MissionSpecifics.actionsWeightTable["RANGED"][i]);
                            }

                            List<(GameObject, double)> priorityTargetsAndWeights = new List<(GameObject, double)>();
                            foreach (GameObject targetableZone in rangedTargetableZones)
                            {
                                ZoneInfo targetableZoneInfo = targetableZone.GetComponent<ZoneInfo>();
                                List<GameObject> dicePool = new List<GameObject>(actionProficiency.proficiencyDice);
                                if (possibleZoneInfo.elevation > targetableZoneInfo.elevation)
                                {
                                    dicePool.Add(possibleZoneInfo.environmentalDie);
                                }
                                if (pointBlankRerolls > 0 && targetableZone == possibleZoneInfo.gameObject)
                                {
                                    availableRerolls += pointBlankRerolls;
                                }
                                //Debug.Log("!!!Trying to get smokeHindrance for " + tag + " from zone " + possibleZoneInfo.name + " to zone " + targetableZone.name);
                                int smokeHindrance = possibleZoneInfo.GetSmokeBetweenZones(targetableZone);

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

                                foreach (GameObject hero in targetableZoneInfo.GetTargetableHeroes())
                                {
                                    double targetHeroWeight = averageWounds * MissionSpecifics.actionsWeightTable["RANGED"][0].weightFactor + actionWeight;
                                    if (hero.GetComponent<Hero>().canCounterRangedAttacks)
                                    {
                                        targetHeroWeight -= UnitIntel.provokingCounterAttackWeight / defense;
                                    }
                                    priorityTargetsAndWeights.Add((hero, targetHeroWeight));
                                }
                                foreach (GameObject heroAlly in targetableZoneInfo.GetTargetableHeroAllies())
                                {
                                    bool isHighPriorityTarget = false;
                                    foreach (MissionSpecifics.ActionWeight highPriorityTarget in highPriorityTargets)
                                    {
                                        if (heroAlly.CompareTag(highPriorityTarget.targetType))
                                        {
                                            isHighPriorityTarget = true;
                                            priorityTargetsAndWeights.Add((heroAlly, averageWounds * highPriorityTarget.weightFactor + actionWeight));
                                            break;
                                        }
                                    }
                                    if (!isHighPriorityTarget)
                                    {
                                        priorityTargetsAndWeights.Add((heroAlly, averageWounds * MissionSpecifics.actionsWeightTable["RANGED"][1].weightFactor + actionWeight));
                                    }
                                }
                            }
                            priorityTargetsAndWeights.Sort((x, y) => y.Item2.CompareTo(x.Item2));  // Sorts by most valuable weight

                            actionWeight = priorityTargetsAndWeights[0].Item2;
                            if (priorityTargetsAndWeights.Count > 1 && burstCarryOver > 0)
                            {
                                actionWeight += UnitIntel.additionalTargetsForAdditionalAttacksWeight;
                            }
                            else if (priorityTargetsAndWeights.Count == 1 && actionProficiency.actionMultiplier > 1)
                            {
                                actionWeight -= actionProficiency.actionMultiplier * UnitIntel.additionalTargetsForAdditionalAttacksWeight;  // Wounds from additional attacks not guaranteed
                            }
                            if (actionWeight > inactiveWeight)
                            {
                                List<GameObject> priorityTargets = priorityTargetsAndWeights.Select(x => x.Item1).ToList();
                                if (priorityTargetsAndWeights[0].Item1.TryGetComponent<Hero>(out var tempHeroComponent))  // tempHeroComponent isn't used, but no other way to use TryGetComponent
                                {
                                    allPossibleActions.Add(new UnitPossibleAction(this, MissionSpecifics.actionsWeightTable["RANGED"][0], actionProficiency, actionWeight, possibleZone, finalDestinationZone, possibleDestinationsAndPaths[finalDestinationZone], null, priorityTargets));  // TargetZone not really needed with priorityTargets list
                                }
                                else
                                {
                                    allPossibleActions.Add(new UnitPossibleAction(this, MissionSpecifics.actionsWeightTable["RANGED"][1], actionProficiency, actionWeight, possibleZone, finalDestinationZone, possibleDestinationsAndPaths[finalDestinationZone], null, priorityTargets));
                                }
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
                                                allPossibleActions.Add(new UnitPossibleAction(this, manipulatable, actionProficiency, actionWeight, possibleZone, finalDestinationZone, possibleDestinationsAndPaths[finalDestinationZone], grenadeTargetZone, null));
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
                                        allPossibleActions.Add(new UnitPossibleAction(this, manipulatable, actionProficiency, actionWeight, possibleZone, finalDestinationZone, possibleDestinationsAndPaths[finalDestinationZone], null, null));
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
                                        allPossibleActions.Add(new UnitPossibleAction(this, thoughtable, actionProficiency, actionWeight, possibleZone, finalDestinationZone, possibleDestinationsAndPaths[finalDestinationZone], null, null));
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
                    allPossibleActions.Add(new UnitPossibleAction(this, defaultGuardAction, moveActionProficiency, onlyMovingWeight, possibleZone, finalDestinationZone, possibleDestinationsAndPaths[finalDestinationZone], null, null));
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
            Vector3 unitSlotCoords = movementPath.zones[movementPath.zones.Count - 1].GetComponent<ZoneInfo>().GetAvailableUnitSlot().transform.position;
            Vector3 endZoneCoords = movementPath.zones[movementPath.zones.Count - 1].transform.position;
            Vector3 finalCoordinates = animate.GetPointFurthestFromOrigin(transform.position, unitSlotCoords, endZoneCoords);
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
            if (movementPath.movementSpent > movePoints && IsActive())  // TODO Reduce bonusMovePointsRemaining based on how far unit gets before dying to environToken, but only have movementSpent for destination
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

    public bool WasAttackTargetDropped(GameObject attackTarget)  // Could be either Hero or Unit
    {
        if (attackTarget.TryGetComponent<Hero>(out Hero targetHero)) {
            return targetHero.IsWoundedOut();
        }
        else if (attackTarget.TryGetComponent<Unit>(out Unit targetUnit))
        {
            return !targetUnit.IsActive();
        }
        else
        {
            Debug.LogError("ERROR! attackTarget of WasAttackTargetDropped(attackTarget) is neither a hero nor a unit");
        }
        return false;
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
                if (unitTurn.priorityTargets.Count > 0)
                {
                    MissionSpecifics.currentPhase = "VillainAttack";
                    GameObject currentMeleeTarget = unitTurn.priorityTargets[0];
                    int currentMeleeTargetIndex = 0;
                    for (int i = 0; i < unitTurn.actionProficiency.actionMultiplier; i++)
                    {
                        GameObject targetZone = null;
                        if (currentMeleeTarget.TryGetComponent<Hero>(out Hero targetHero))
                        {
                            if (targetHero.IsWoundedOut())
                            {
                                currentMeleeTargetIndex++;
                                if (currentMeleeTargetIndex < unitTurn.priorityTargets.Count)  // Repeat this iteration of the loop with a new target
                                {
                                    currentMeleeTarget = unitTurn.priorityTargets[currentMeleeTargetIndex];
                                    i--;
                                    continue;
                                }
                                else  // No more targets
                                {
                                    goto meleeAttacksFinishedJump;
                                }
                            }
                            else
                            {
                                targetZone = targetHero.GetZone();
                            }
                        }
                        else if (currentMeleeTarget.TryGetComponent<Unit>(out Unit targetUnit))
                        {
                            if (!targetUnit.IsActive())
                            {
                                currentMeleeTargetIndex++;
                                if (currentMeleeTargetIndex < unitTurn.priorityTargets.Count)  // Repeat this iteration of the loop with a new target
                                {
                                    currentMeleeTarget = unitTurn.priorityTargets[currentMeleeTargetIndex];
                                    i--;
                                    continue;
                                }
                                else  // No more targets
                                {
                                    goto meleeAttacksFinishedJump;
                                }
                            }
                            else
                            {
                                targetZone = targetUnit.GetZone();
                            }
                        }
                        ZoneInfo targetZoneInfo = targetZone.GetComponent<ZoneInfo>();
                        int totalZoneEnemiesBeforeAttack = targetZoneInfo.GetTargetableHeroesCount() + targetZoneInfo.GetTargetableHeroesAlliesCount();

                        actionSuccesses = RollAndReroll(unitTurn.actionProficiency.proficiencyDice, availableRerolls);
                        if (actionSuccesses > 0)
                        {
                            actionSuccesses += martialArtsSuccesses;
                        }
                        yield return StartCoroutine(animate.MeleeAttack(gameObject, currentMeleeTarget, actionSuccesses));

                        int totalZoneEnemiesAfterAttack = targetZoneInfo.GetTargetableHeroesCount() + targetZoneInfo.GetTargetableHeroesAlliesCount();

                        if (fiery)
                        {
                            yield return StartCoroutine(targetZoneInfo.AddEnvironTokens(new EnvironTokenSave("Flame", 1, false, true)));
                        }
                        if (frosty)
                        {
                            yield return StartCoroutine(targetZoneInfo.AddEnvironTokens(new EnvironTokenSave("Frost", 1, false, true)));
                        }

                        int circularStrikeOccurrences = 0;  // Does this reset on each attack?
                        while (circularStrike > circularStrikeOccurrences)
                        {
                            if (IsActive())
                            {
                                if (totalZoneEnemiesAfterAttack < totalZoneEnemiesBeforeAttack)
                                {
                                    if (WasAttackTargetDropped(currentMeleeTarget))
                                    {
                                        currentMeleeTargetIndex++;
                                        if (currentMeleeTargetIndex < unitTurn.priorityTargets.Count)
                                        {
                                            currentMeleeTarget = unitTurn.priorityTargets[currentMeleeTargetIndex];
                                            if (currentMeleeTarget.TryGetComponent<Hero>(out Hero carryOverTargetHero))
                                            {
                                                if (carryOverTargetHero.IsWoundedOut())
                                                {
                                                    continue;  // Start circularStrike loop over to select new currentMeleeTarget
                                                }
                                                else
                                                {
                                                    targetZone = carryOverTargetHero.GetZone();
                                                }
                                            }
                                            else if (currentMeleeTarget.TryGetComponent<Unit>(out Unit carryOverTargetUnit))
                                            {
                                                if (!carryOverTargetUnit.IsActive())
                                                {
                                                    continue;  // Start circularStrike loop over to select new currentMeleeTarget
                                                }
                                                else
                                                {
                                                    targetZone = carryOverTargetUnit.GetZone();
                                                }
                                            }
                                        }
                                        else
                                        {
                                            goto meleeAttacksFinishedJump;  // No more targets
                                        }
                                        targetZoneInfo = targetZone.GetComponent<ZoneInfo>();
                                        totalZoneEnemiesBeforeAttack = targetZoneInfo.GetTargetableHeroesCount() + targetZoneInfo.GetTargetableHeroesAlliesCount();
                                    }
                                    else
                                    {
                                        totalZoneEnemiesBeforeAttack = totalZoneEnemiesAfterAttack;
                                    }
                                    yield return StartCoroutine(animate.MeleeAttack(gameObject, currentMeleeTarget, -1));
                                    circularStrikeOccurrences++;

                                    totalZoneEnemiesAfterAttack = targetZoneInfo.GetTargetableHeroesCount() + targetZoneInfo.GetTargetableHeroesAlliesCount();

                                    if (fiery)  // Triggered during the "End the Melee Attack" step, which happens for circularStrike?
                                    {
                                        yield return StartCoroutine(targetZoneInfo.AddEnvironTokens(new EnvironTokenSave("Flame", 1, false, true)));
                                    }
                                    if (frosty)
                                    {
                                        yield return StartCoroutine(targetZoneInfo.AddEnvironTokens(new EnvironTokenSave("Frost", 1, false, true)));
                                    }
                                }
                                else
                                {
                                    break;
                                }
                            }
                            else
                            {
                                break;  // Exit the while loop (and same IsActive() check below exits the for loop) so you can still set MissionSpecifics.currentPhase = "Villain";
                            }
                        }

                        if (!IsActive())  // If Counterattacked and killed, stop attacking
                        {
                            if (currentMeleeTarget.TryGetComponent<Hero>(out Hero vengefulHero))
                            {
                                vengefulHero.canCounterMeleeAttacks = true;
                            }
                            goto meleeAttacksFinishedJump;  // break doesn't cut it because you're back at the top of the loop. Better to jump
                        }
                    }
                    meleeAttacksFinishedJump:;  // Needed when running out of priorityTargets
                    MissionSpecifics.currentPhase = "Villain";
                }
                break;
            case "RANGED":
                if (unitTurn.priorityTargets.Count > 0)
                {
                    MissionSpecifics.currentPhase = "VillainAttack";
                    GameObject currentRangedTarget = unitTurn.priorityTargets[0];
                    int currentRangedTargetIndex = 0;
                    for (int i = 0; i < unitTurn.actionProficiency.actionMultiplier; i++)
                    {
                        GameObject targetZone = null;
                        if (currentRangedTarget.TryGetComponent<Hero>(out Hero targetHero))
                        {
                            if (targetHero.IsWoundedOut())
                            {
                                currentRangedTargetIndex++;
                                if (currentRangedTargetIndex < unitTurn.priorityTargets.Count)  // Repeat this iteration of the loop with a new target
                                {
                                    currentRangedTarget = unitTurn.priorityTargets[currentRangedTargetIndex];
                                    i--;
                                    continue;
                                }
                                else  // No more targets
                                {
                                    goto rangeAttacksFinishedJump;
                                }
                            }
                            else
                            {
                                targetZone = targetHero.GetZone();
                            }
                        }
                        else if (currentRangedTarget.TryGetComponent<Unit>(out Unit targetUnit))
                        {
                            if (!targetUnit.IsActive())
                            {
                                currentRangedTargetIndex++;
                                if (currentRangedTargetIndex < unitTurn.priorityTargets.Count)  // Repeat this iteration of the loop with a new target
                                {
                                    currentRangedTarget = unitTurn.priorityTargets[currentRangedTargetIndex];
                                    i--;
                                    continue;
                                }
                                else  // No more targets
                                {
                                    goto rangeAttacksFinishedJump;
                                }
                            }
                            else
                            {
                                targetZone = targetUnit.GetZone();
                            }
                        }

                        if (targetZone)  // Set from hero vs heroAlly above, not from unitTurn.targetedZone
                        {
                            ZoneInfo targetZoneInfo = targetZone.GetComponent<ZoneInfo>();
                            int totalZoneEnemiesBeforeAttack = targetZoneInfo.GetTargetableHeroesCount() + targetZoneInfo.GetTargetableHeroesAlliesCount();

                            List<GameObject> dicePool = new List<GameObject>(unitTurn.actionProficiency.proficiencyDice);
                            if (currentZoneInfo.elevation > targetZone.GetComponent<ZoneInfo>().elevation)
                            {
                                dicePool.Add(currentZoneInfo.environmentalDie);
                            }

                            if (currentZone == unitTurn.targetedZone)
                            {
                                availableRerolls += pointBlankRerolls;
                            }
                            int smokeHindrance = currentZoneInfo.GetSmokeBetweenZones(targetZone);

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
                            yield return StartCoroutine(animate.RangedAttack(gameObject, currentRangedTarget, actionSuccesses));

                            int totalZoneEnemiesAfterAttack = targetZoneInfo.GetTargetableHeroesCount() + targetZoneInfo.GetTargetableHeroesAlliesCount();

                            if (fiery)
                            {
                                yield return StartCoroutine(targetZoneInfo.AddEnvironTokens(new EnvironTokenSave("Flame", 1, false, true)));
                            }
                            if (frosty)
                            {
                                yield return StartCoroutine(targetZoneInfo.AddEnvironTokens(new EnvironTokenSave("Frost", 1, false, true)));
                            }

                            int burstCarryOverOccurrences = 0;
                            while (burstCarryOver > burstCarryOverOccurrences)
                            {
                                if (IsActive())
                                {
                                    if (totalZoneEnemiesAfterAttack < totalZoneEnemiesBeforeAttack)
                                    {
                                        if (WasAttackTargetDropped(currentRangedTarget))
                                        {
                                            currentRangedTargetIndex++;
                                            if (currentRangedTargetIndex < unitTurn.priorityTargets.Count)
                                            {
                                                currentRangedTarget = unitTurn.priorityTargets[currentRangedTargetIndex];
                                                if (currentRangedTarget.TryGetComponent<Hero>(out Hero carryOverTargetHero))
                                                {
                                                    if (carryOverTargetHero.IsWoundedOut())
                                                    {
                                                        continue;  // Start burstCarryOver loop over to select new currentRangedTarget
                                                    }
                                                    else
                                                    {
                                                        targetZone = carryOverTargetHero.GetZone();
                                                    }
                                                }
                                                else if (currentRangedTarget.TryGetComponent<Unit>(out Unit carryOverTargetUnit))
                                                {
                                                    if (!carryOverTargetUnit.IsActive())
                                                    {
                                                        continue;  // Start carryOver loop over to select new currentRangedTarget
                                                    }
                                                    else
                                                    {
                                                        targetZone = carryOverTargetUnit.GetZone();
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                goto rangeAttacksFinishedJump;  // No more targets
                                            }
                                            targetZoneInfo = targetZone.GetComponent<ZoneInfo>();
                                            totalZoneEnemiesBeforeAttack = targetZoneInfo.GetTargetableHeroesCount() + targetZoneInfo.GetTargetableHeroesAlliesCount();
                                        }
                                        else
                                        {
                                            totalZoneEnemiesBeforeAttack = totalZoneEnemiesAfterAttack;
                                        }
                                        yield return StartCoroutine(animate.RangedAttack(gameObject, currentRangedTarget, -1));
                                        burstCarryOverOccurrences++;

                                        totalZoneEnemiesAfterAttack = targetZoneInfo.GetTargetableHeroesCount() + targetZoneInfo.GetTargetableHeroesAlliesCount();

                                        if (fiery)  // Triggered during the "End the Melee Attack" step, which happens for circularStrike?
                                        {
                                            yield return StartCoroutine(targetZoneInfo.AddEnvironTokens(new EnvironTokenSave("Flame", 1, false, true)));
                                        }
                                        if (frosty)
                                        {
                                            yield return StartCoroutine(targetZoneInfo.AddEnvironTokens(new EnvironTokenSave("Frost", 1, false, true)));
                                        }
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                                else
                                {
                                    break;  // Exit the while loop (and same IsActive() check below exits the for loop) so you can still set MissionSpecifics.currentPhase = "Villain";
                                }
                            }
                        }
                        else
                        {
                            Debug.LogError("ERROR! RANGED action was performed while targetZone was null, so henchman just wasted its action wildly firing its gun into the air.");
                        }

                        if (!IsActive())  // If Counterattacked and killed, stop attacking
                        {
                            if (currentRangedTarget.TryGetComponent<Hero>(out Hero vengefulHero))
                            {
                                vengefulHero.canCounterRangedAttacks = true;
                            }
                            goto rangeAttacksFinishedJump;  // break doesn't cut it because you're back at the top of the loop. Better to jump
                        }
                    }
                    rangeAttacksFinishedJump:;  // Needed when running out of priorityTargets
                    MissionSpecifics.currentPhase = "Villain";
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

    public void ShowWoundShields()
    {
        int currentlyDisplayedWoundShields = 0;
        foreach (Transform woundShield in transform.Find("WoundShieldsContainer"))
        {
            if (currentlyDisplayedWoundShields >= woundShields)
            {
                break;
            }
            woundShield.gameObject.SetActive(true);
            currentlyDisplayedWoundShields++;
        }
    }

    public void HideWoundShields()
    {
        foreach (Transform woundShield in transform.Find("WoundShieldsContainer"))
        {
            woundShield.gameObject.SetActive(false);
        }
    }

    public void GenerateWoundShields()  // Called on ScenarioMap.CallReinforcements(), could be called on in LoadUnitSave() below
    {
        //string loadWoundShieldsDebugString = tag + "LoadUnitSave woundShields: " + woundShields.ToString();
        if (IsVillain())
        {
            woundShields = 1;
        }
        else
        {
            List<int> possibleWoundShieldValues = MissionSpecifics.GetWoundShieldValues().ToList();
            int[] woundShieldFrequencyMapping = new int[possibleWoundShieldValues.Count];  // By default each index should be initialized to 0
            foreach (GameObject unitObject in GameObject.FindGameObjectsWithTag(tag))
            {
                Unit unit = unitObject.GetComponent<Unit>();
                if (unit != this && unit.woundShields >= 0)
                {
                    //loadWoundShieldsDebugString += "   " + unit.tag + " from " + unit.GetZone().name + " with woundShields: " + unit.woundShields;
                    woundShieldFrequencyMapping[unit.woundShields]++;
                }
            }
            int leastOccurencesOfSameWoundShieldNumber = woundShieldFrequencyMapping.Min();
            //loadWoundShieldsDebugString += "\nleastOccurencesOfSameWoundShieldNumber: " + leastOccurencesOfSameWoundShieldNumber.ToString();
            for (int i = 0; i < woundShieldFrequencyMapping.Length; i++)
            {
                //loadWoundShieldsDebugString += "    woundShieldFrequencyMapping[" + i.ToString() + "]: " + woundShieldFrequencyMapping[i].ToString();
                if (woundShieldFrequencyMapping[i] > leastOccurencesOfSameWoundShieldNumber)
                {
                    possibleWoundShieldValues.Remove(i);
                    //loadWoundShieldsDebugString += " is greater than leastOccurences, so removing it from list.";
                }
            }
            woundShields = possibleWoundShieldValues[random.Next(possibleWoundShieldValues.Count)];
            //woundShields = possibleWoundShieldValues[random.Next(possibleWoundShieldValues.Count)];

            //loadWoundShieldsDebugString += "    possibleWoundShieldValues list: { ";
            //foreach (int woundShieldValue in possibleWoundShieldValues)
            //{
            //    loadWoundShieldsDebugString += woundShieldValue + ", ";
            //}
            //loadWoundShieldsDebugString += "}";
        }
        //loadWoundShieldsDebugString += "    final woundShields: " + woundShields.ToString();
        //Debug.Log(loadWoundShieldsDebugString);
        ShowWoundShields();
    }

    public void LoadUnitSave(UnitSave unitSave)  // Called from ZoneInfo.LoadZoneSave()
    {
        ModifyLifePoints(unitSave.lifePoints - lifePoints);
        isHeroAlly = unitSave.isHeroAlly;
        lifePoints = unitSave.lifePoints;
        //HideWoundShields();
        if (!isHeroAlly && unitSave.woundShields < 0)  // if still -1, set this unit's woundShields
        {
            GenerateWoundShields();
        }
        //ShowWoundShields();  // Only needed with HideWoundShields() above, otherwise called by GenerateWoundShields() if needed
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
    public bool isHeroAlly;
    public int lifePoints;
    public int woundShields;

    public UnitSave(Unit unit)
    {
        tag = unit.tag;
        isHeroAlly = unit.isHeroAlly;
        lifePoints = unit.lifePoints;
        woundShields = unit.woundShields;
    }
}
