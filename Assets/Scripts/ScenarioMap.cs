using System;  // for Math.abs()
using System.Collections;
using System.Collections.Generic;
using System.IO;  // For File.ReadAllText for loading json save files
using System.Linq;  // For converting array.ToList()
using UnityEngine;
using UnityEngine.UI;  // For button
//using UnityEditor;  // For AssetDatabase.LoadAssetAtPath() for getting unit prefabs

public class ScenarioMap : MonoBehaviour
{
    readonly System.Random random = new System.Random();

    public GameObject UIOverlay;

    [SerializeField]
    List<GameObject> unitPrefabsMasterList;
    private List<GameObject> spawnZones = new List<GameObject>();
    public Dictionary<string, GameObject> unitPrefabsMasterDict;
    public List<string> villainRiver;
    public List<string> unitTagsMasterList;
    public List<string> potentialAlliesList;

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
    public UnitPool[] unitsPool;  // Unity can't expose dictionaries in the inspector, so Dictionary<string, GameObject[]> actionProficiencies not possible without addon

    public string missionName;
    public int currentRound = 1;  // Used by ScenarioMapSave at bottom
    public int reinforcementPoints = 5;
    public List<GameObject> heroes;

    public GameObject animationContainer;
    public Animate animate;
    public GameObject heroPrefab;
    public GameObject bombPrefab;  // These prefabs are here just to be used by MissionSpecifics
    public GameObject primedBombPrefab;
    public GameObject briefcasePrefab;
    public GameObject jammerPrefab;
    public GameObject activeJammerPrefab;
    public GameObject ratPrefab;


    void Awake()
    {
        animationContainer = GameObject.FindGameObjectWithTag("AnimationContainer");
        animate = animationContainer.GetComponent<Animate>();
        MissionSpecifics.scenarioMap = this;
        MissionSpecifics.mainCamera = GameObject.FindGameObjectWithTag("MainCamera");  // Might be able to be replaced with animate.mainCamera, but as both happen in Awake() maybe not;
        unitPrefabsMasterDict = new Dictionary<string, GameObject>();
        foreach (GameObject unitPrefab in unitPrefabsMasterList)
        {
            unitPrefabsMasterDict[unitPrefab.name] = unitPrefab;
        }

        ScenarioSave scenarioSave;
        if (SceneHandler.saveName == null)
        {
            scenarioSave = JsonUtility.FromJson<ScenarioSave>(Resources.Load<TextAsset>("MissionSetupSaves/" + MissionSpecifics.missionName).text);
        }
        else
        {
            scenarioSave = JsonUtility.FromJson<ScenarioSave>(File.ReadAllText(Application.persistentDataPath + "/" + SceneHandler.saveName));
        }
        LoadScenarioSave(scenarioSave);
    }

    void Start()
    {
        animate.CameraToMaxZoom();
        if (SceneHandler.saveName == null)  // If new game
        {
            StartSetup();
        }
        else
        {
            StartFirstTurn();
        }
    }

    void StartSetup()
    {
        MissionSpecifics.currentPhase = "Setup";
        UIOverlay.GetComponent<UIOverlay>().InitializeSetupUIOverlay();
        UIOverlay.GetComponent<UIOverlay>().ShowSetupUIOverlay();
        if (SceneHandler.isFirstTimePlaying)
        {
            UIOverlay.GetComponent<UIOverlay>().OpenMenu();  // OpenMenu first for prep work: Disabling player control of camera and disabling of other buttons and such
            UIOverlay.GetComponent<UIOverlay>().OpenHelp();
        }
    }

    public void StartFirstTurn()  // called from UIOverlay when Start Game button clicked
    {
        //animate.CameraToMaxZoom();  // Does this also center the camera when coming from StartSetup()?
        if (currentRound < 1)  // Start with villain's turn
        {
            StartCoroutine(StartVillainTurn());
        }
        else
        {
            MissionSpecifics.currentPhase = "Hero";  // StartHeroTurn() actually not called here as it also advances the currentRound and the clock
            MissionSpecifics.SetActionsWeightTable();  // Copied from StartHeroTurn()
            UnitIntel.ResetPerRoundResources();
            EnablePlayerUI();  // UIOverlay starts disabled so it doesn't flash on the screen when villains go first
        }
    }

    public void DisablePlayerUI()
    {
        UIOverlay.GetComponent<UIOverlay>().HideUIOverlay();
        ConfigureUnitHeroAndTokenInteractivity();
    }

    public void EnablePlayerUI()
    {
        // Re-Enable all the UI buttons which were disabled at EndHeroTurn()
        UIOverlay.GetComponent<UIOverlay>().ShowUIOverlay();
        ConfigureUnitHeroAndTokenInteractivity();
    }

    public void ConfigureUnitHeroAndTokenInteractivity()
    {
        foreach (Unit unit in transform.GetComponentsInChildren<Unit>())
        {
            unit.ConfigureClickAndDragability();
        }
        foreach (GameObject hero in heroes)
        {
            hero.GetComponent<Hero>().ConfigureClickAndDragability();
        }
        foreach (Token token in transform.GetComponentsInChildren<Token>())
        {
            token.ConfigureClickability();
        }
    }

    // The flow: Player uses UI to end turn -> EndHeroTurn() -> StartVillainTurn() -> StartHeroTurn() -> Player takes their next turn
    // Villain first flow: StartVillainTurn() -> StartHeroTurn() -> Player takes their next turn -> Player uses UI to end turn -> EndHeroTurn()
    public void EndHeroTurn()
    {
        if (MissionSpecifics.IsGameOver(currentRound))
        {
            MissionSpecifics.currentPhase = "GameOver";
            MissionSpecifics.EndGameAnimation();
            UIOverlay.GetComponent<UIOverlay>().ShowGameOverPanel();
        }
        else
        {
            // Dredge the river, removing any tiles with 0 units on the map
            foreach (string unitTag in new List<string>(villainRiver))
            {
                if (unitTag != "REINFORCEMENT" && GameObject.FindGameObjectsWithTag(unitTag).Length == 0)
                {
                    Debug.Log("Dredging " + unitTag);
                    villainRiver.Remove(unitTag);
                }
            }

            SaveIntoJson();  // Do this before CleanupZones() in case player wants to go back to just before they ended their turn.
            CleanupZones();
            StartCoroutine(StartVillainTurn());
        }
    }

    IEnumerator StartVillainTurn()
    {
        MissionSpecifics.currentPhase = "Villain";
        EnablePlayerUI();  // Needed to show UtilityBelt if villain taking first turn. Can't put in StartFirstTurn() either because don't want clock and menu buttons to flash on screen
        DisablePlayerUI();  // Disable all UI so Villain turn isn't interrupted
        Camera.main.GetComponent<PanAndZoom>().controlCamera = false;  // Disable camera controls. Animate functions undo this, but are responsible for redisabling player's control of camera
        yield return StartCoroutine(MissionSpecifics.VillainTurnStarted());

        if (MissionSpecifics.IsGameOver(currentRound))
        {
            MissionSpecifics.EndGameAnimation();
            UIOverlay.GetComponent<UIOverlay>().ShowGameOverPanel();
        }
        else
        {
            DissipateEnvironTokens(false);
            MissionSpecifics.SetActionsWeightTable();
            UnitIntel.ResetPerRoundResources();

            yield return StartCoroutine(ActivateRiverTiles());
            yield return StartCoroutine(StartHeroTurn());
        }
        yield return 0;
    }

    IEnumerator ActivateRiverTiles()
    {
        int totalTileActivations = 2;  // Number of tiles left to activate
        List<Unit.UnitPossibleAction> predeterminedActivations = MissionSpecifics.GetPredeterminedActivations();
        if (predeterminedActivations != null)
        {
            List<string> unitTypesActivated = new List<string>();
            foreach (Unit.UnitPossibleAction unitAction in predeterminedActivations)
            {
                yield return unitAction.myUnit.ActivateUnitWithPredeterminedAction(unitAction);

                villainRiver.Remove(unitAction.myUnit.tag);
                villainRiver.Add(unitAction.myUnit.tag);
                if (!unitTypesActivated.Contains(unitAction.myUnit.tag))  // If a new type of unit is being activated, a new tile is being activated
                {
                    unitTypesActivated.Add(unitAction.myUnit.tag);
                    totalTileActivations -= 1;
                    yield return StartCoroutine(MissionSpecifics.CharacterTileActivated());  // JamAndSeek uses CallReinforcements, so wait for whatever it's going to do
                }

                if (MissionSpecifics.IsGameOver(currentRound))
                {
                    totalTileActivations = 0;
                    yield break;  // Skip to IsGameOver evaluation at end of Villain turn
                }
            }
        }
        for (int i = 0; i < totalTileActivations; i++)  // Will error if predeterminedActivations > totalActivations
        {
            (string unitTypeToActivate, GameObject[] unitsToActivate) = GetVillainTileToActivate(i);
            if (unitTypeToActivate == "REINFORCEMENT")
            {
                yield return StartCoroutine(CallReinforcements(MissionSpecifics.GetReinforcementPoints()));
                yield return StartCoroutine(MissionSpecifics.ActivateReinforcement());
            }
            else
            {
                yield return StartCoroutine(ActivateUnits(unitsToActivate, unitTypeToActivate));
            }

            villainRiver.Remove(unitTypeToActivate);
            villainRiver.Add(unitTypeToActivate);
            if (unitTypeToActivate != "REINFORCEMENT")
            {
                yield return StartCoroutine(MissionSpecifics.CharacterTileActivated());  // JamAndSeek uses CallReinforcements, so wait for whatever it's going to do
            }

            if (MissionSpecifics.IsGameOver(currentRound))
            {
                yield break;  // Skip to IsGameOver evaluation at end of Villain turn
            }
        }
        yield return 0;
    }

    IEnumerator ActivateUnits(GameObject[] unitsToActivate, string unitTypeToActivate)
    {
        foreach (GameObject unit in unitsToActivate)
        {
            Unit unitInfo = unit.GetComponent<Unit>();
            if (unitInfo.IsActive())  // Check if killed (but not yet removed) by previous unit's turn
            {
                yield return StartCoroutine(unit.GetComponent<Unit>().ActivateUnit());
                if (MissionSpecifics.IsGameOver(currentRound))
                {
                    yield break;  // Skip to IsGameOver evaluation at end of Villain turn
                }
            }
        }
        while (UnitIntel.unitsToActivateLast.Count > 0)  // Now activate any units that were going to use an activateLast ActionWeight
        {
            Unit unitInfo = UnitIntel.unitsToActivateLast.Dequeue().GetComponent<Unit>();
            if (unitInfo.CompareTag(unitTypeToActivate))
            {
                if (unitInfo.IsActive())
                {
                    yield return StartCoroutine(unitInfo.ActivateUnit(true));  // param activatingLast = true
                    if (MissionSpecifics.IsGameOver(currentRound))
                    {
                        yield break;  // Skip to IsGameOver evaluation at end of Villain turn
                    }
                }
            }
            else
            {
                Debug.LogError("ERROR! ScenarioMap.ActivateUnitsWithTag(" + unitTypeToActivate + ") tried to activate " + unitInfo.tag + " from UnitIntel.unitsToActivateLast.");
            }
        }
        yield return 0;
    }

    IEnumerator StartHeroTurn()
    {
        if (MissionSpecifics.IsGameOver(currentRound))
        {
            MissionSpecifics.EndGameAnimation();
            UIOverlay.GetComponent<UIOverlay>().ShowGameOverPanel();
        }
        else
        {
            MissionSpecifics.currentPhase = "Hero";
            DissipateEnvironTokens(true);
            CleanupZones();

            currentRound += 1;
            MissionSpecifics.currentRound = currentRound;
            if (currentRound > 1)
            {
                SaveIntoJson();
            }

            foreach (GameObject heroObject in heroes)
            {
                heroObject.GetComponent<Hero>().RestReset();
            }

            MissionSpecifics.SetActionsWeightTable();  // In case game loaded and then enemy unit forced to flee on hero's turn like in JamAndSeek mission
            UnitIntel.ResetPerRoundResources();

            // Re-Enable all the UI buttons which were disabled at EndHeroTurn()
            EnablePlayerUI();
            // Reactivate camera controls
            Camera.main.GetComponent<PanAndZoom>().controlCamera = true;
            yield return UIOverlay.GetComponent<UIOverlay>().AdvanceClock(currentRound);  // Once clock is visible, advance it
        }
        yield return 0;
    }

    (string, GameObject[]) GetVillainTileToActivate(int unitsAlreadyActivated)
    {
        //string debugVillainRiverString = "GetVillainTileToActivate  villainRiver: ";
        //foreach (string unitTag in villainRiver)
        //{
        //    debugVillainRiverString += " " + unitTag + " ";
        //}
        //Debug.Log(debugVillainRiverString);
        double totalWeightOfMostValuableUnitTurn = -1;  // Any negative number as work, still want to activate a unit even if their highest weight is 0
        List<(GameObject, double)> unitsOfTagByWeight = null;
        string mostValuableUnitType = null;
        string debugVillainTilesString = "GetVillainTileToActivate() each unit weight {  ";
        for (int j = 0; j < 3; j++)  // Only first 3 tiles compared
        {
            string unitTag = villainRiver[j];
            double currentWeightOfUnitTurn = 0;
            List<(GameObject, double)> unitsOfTag = new List<(GameObject, double)>();

            if (unitTag == "REINFORCEMENT")
            {
                if (unitsAlreadyActivated == 0)  // Only reinforce if first unit activation
                {
                    List<Tuple<UnitPool, double, GameObject>> reinforcementsAvailable = GetAvailableReinforcements();
                    int reinforcementPointsRemaining = reinforcementPoints;
                    Dictionary<string, int> unitsEarliestPossibleActivationRound = new Dictionary<string, int>();
                    foreach (Tuple<UnitPool, double, GameObject> unitPool in reinforcementsAvailable)
                    {
                        if (!unitsEarliestPossibleActivationRound.ContainsKey(unitPool.Item1.unit.tag))
                        {
                            unitsEarliestPossibleActivationRound.Add(unitPool.Item1.unit.tag, GetEarliestPossibleActivationRound(unitPool.Item1.unit.tag));
                        }

                        if (unitsEarliestPossibleActivationRound[unitPool.Item1.unit.tag] >= 0)  // If the unit has the opportunity to activate before the game ends
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
                    }
                    currentWeightOfUnitTurn += MissionSpecifics.GetReinforcementWeight();
                }
            }
            else
            {
                foreach (GameObject unitObject in GameObject.FindGameObjectsWithTag(unitTag))
                {
                    Unit unit = unitObject.GetComponent<Unit>();
                    if (unit.IsActive())  // Doublecheck unit is active as CleanupZones() may be lagging behind
                    {
                        double currentUnitMostValuableActionWeight = unit.GetMostValuableActionWeight();
                        unitsOfTag.Add((unitObject, currentUnitMostValuableActionWeight));
                        currentWeightOfUnitTurn += currentUnitMostValuableActionWeight;
                    }
                }
            }

            currentWeightOfUnitTurn *= MissionSpecifics.GetRiverActivationWeight()[j];

            if (currentWeightOfUnitTurn > totalWeightOfMostValuableUnitTurn)
            {
                unitsOfTagByWeight = new List<(GameObject, double)>(unitsOfTag);
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
            Debug.LogError("ERROR! ScenarioMap.GetVillainTileToActivate() mostValuableUnitType is null. Are there no tiles in the river to activate? villainRiver: " + villainRiverString);
        }

        if (unitsOfTagByWeight != null)
        {
            unitsOfTagByWeight.Sort((x, y) => y.Item2.CompareTo(x.Item2));  // Sorts from highest weight tuple to lowest
            return (mostValuableUnitType, unitsOfTagByWeight.Select(item => item.Item1).ToArray());
        }
        else
        {
            return (mostValuableUnitType, null);  // Should always be "REINFORCEMENT"
        }
    }

    public int GetEarliestPossibleActivationRound(string unitTag, int activationsThisRound = 0)
    {
        int roundsRemaining = MissionSpecifics.GetFinalRound() - currentRound;
        int activationsRemaining = roundsRemaining * 2 - activationsThisRound;
        int unitRiverIndex = villainRiver.IndexOf(unitTag);
        if (activationsRemaining > 0)
        {
            for (int i = 0; i < roundsRemaining; i++)
            {
                if (unitRiverIndex < 4 + (i * 2 - activationsThisRound))
                {
                    return i;
                }
            }
        }
        return -1;
    }

    public IEnumerator CallReinforcements(int reinforcementPointsRemaining)
    {
        List<Tuple<UnitPool, double, GameObject>> reinforcementsAvailable = GetAvailableReinforcements();
        //int reinforcementPointsRemaining = reinforcementPoints;
        int i = 0;
        animate.CameraToFixedZoom();

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
                GameObject availableUnitSlot = reinforcementsAvailable[i].Item3.GetComponent<ZoneInfo>().GetAvailableUnitSlot();
                if (!animate.IsPointOnScreen(availableUnitSlot.transform.position, .01f))  // Reinforcements typically spawned on edges/corners of map, so greatly reduce buffer to prevent slight camera jumps
                {
                    animate.mainCamera.transform.position = new Vector3(availableUnitSlot.transform.position.x, availableUnitSlot.transform.position.y, animate.mainCamera.transform.position.z);
                }
                yield return new WaitForSecondsRealtime(1);
                GameObject spawnedUnit = Instantiate(reinforcementsAvailable[i].Item1.unit, availableUnitSlot.transform);  // spawn Unit
                spawnedUnit.GetComponent<Unit>().GenerateWoundShields();
                yield return new WaitForSecondsRealtime(2);
                Debug.Log("CallReinforcements() spawning " + reinforcementsAvailable[i].Item1.unit.tag + " at " + reinforcementsAvailable[i].Item3.name);
            }
            reinforcementPointsRemaining -= numToReinforce * unitInfo.reinforcementCost;
            i++;
        }

        yield return 0;
    }

    List<Tuple<UnitPool, double, GameObject>> GetAvailableReinforcements()
    {
        List<Tuple<UnitPool, double, GameObject>> reinforcementsAvailable = new List<Tuple<UnitPool, double, GameObject>>();
        //string unitsPoolDebugString = "GetAvailableReinforcements() reinforcementPoints: " + reinforcementPoints.ToString() + "unitsPool: {";
        foreach (UnitPool unitPool in unitsPool)
        {
            if (unitPool.unit.GetComponent<Unit>().reinforcementCost > 0)  // If Unit can be reinforced
            {
                if (villainRiver.Contains(unitPool.unit.tag))  // If unit tile has not been dredged from (or not added to if originally missing like SUPERBARN) villainRiver
                {
                    //unitsPoolDebugString += " " + unitPool.unit.tag + " onMap:" + GameObject.FindGameObjectsWithTag(unitPool.unit.tag).Length.ToString() + " of total:" + unitPool.total.ToString();
                    int inReserve = unitPool.total - GameObject.FindGameObjectsWithTag(unitPool.unit.tag).Length;
                    if (inReserve > 0)
                    {
                        Boolean activateableThisTurn = false;
                        for (int i = 0; i < 4; i++)
                        {
                            if (villainRiver[i] != "REINFORCEMENT" && unitPool.unit.CompareTag(villainRiver[i]))
                            {
                                activateableThisTurn = true;
                                break;
                            }
                        }

                        (GameObject, double) bestSpawnZoneAndWeight = ChooseBestUnitPlacement(unitPool.unit, spawnZones);
                        if (!activateableThisTurn)
                        {
                            bestSpawnZoneAndWeight.Item2 *= .9;  // Only 90% as valuable if Unit can't be activated this turn
                        }
                        reinforcementsAvailable.Add(new Tuple<UnitPool, double, GameObject>(new UnitPool(unitPool.unit, inReserve), bestSpawnZoneAndWeight.Item2, bestSpawnZoneAndWeight.Item1));
                    }
                }
            }
        }
        //Debug.Log(unitsPoolDebugString);
        reinforcementsAvailable.Sort((x, y) => x.Item2.CompareTo(y.Item2));  // Sorts list by most valuable weight. https://stackoverflow.com/questions/4668525/sort-listtupleint-int-in-place

        string reinforcementsAvailableDebugString = "GetAvailableReinforcements() reinforcementsAvailable: [";
        foreach (Tuple<UnitPool, double, GameObject> reinforcement in reinforcementsAvailable)
        {
            reinforcementsAvailableDebugString += " { " + reinforcement.Item1.total + " of " + reinforcement.Item1.unit.tag + " with weight " + reinforcement.Item2 + " at spawn " + reinforcement.Item3 + "} ";
        }
        Debug.Log(reinforcementsAvailableDebugString + "]");

        return reinforcementsAvailable;
    }

    public (GameObject, double) ChooseBestUnitPlacement(GameObject unitPrefab, List<GameObject> possibleZones)
    {
        GameObject bestZone = null;
        double bestZoneActionWeight = -1000;
        foreach (GameObject possibleZone in possibleZones)
        {
            GameObject availableUnitSlot = possibleZone.GetComponent<ZoneInfo>().GetAvailableUnitSlot();
            if (availableUnitSlot != null)
            {
                GameObject tempUnit = Instantiate(unitPrefab, availableUnitSlot.transform);
                double currentZoneActionWeight = tempUnit.GetComponent<Unit>().GetMostValuableActionWeight();
                DestroyImmediate(tempUnit);
                if (currentZoneActionWeight > bestZoneActionWeight)
                {
                    bestZone = possibleZone;
                    bestZoneActionWeight = currentZoneActionWeight;
                }
            }
        }

        return (bestZone, bestZoneActionWeight);
    }

    void DissipateEnvironTokens(bool isHeroTurn)
    {
        string[] dissipatingEnvironTokensTags = new string[] { "Gas", "Flame", "Smoke", "Frost" };
        foreach (string tokenTag in dissipatingEnvironTokensTags)
        {
            foreach (GameObject environToken in GameObject.FindGameObjectsWithTag(tokenTag))
            {
                environToken.GetComponent<EnvironToken>().Dissipate(isHeroTurn);
            }
        }
    }

    void CleanupZones()
    {
        foreach (GameObject zone in GameObject.FindGameObjectsWithTag("ZoneInfoPanel"))
        {
            ZoneInfo zoneInfo = zone.GetComponent<ZoneInfo>();

            if (zoneInfo.HasObjectiveToken("ImaginedCompanion"))
            {
                bool existsActiveImaginer = false;
                foreach (Unit unit in zoneInfo.GetUnitsInfo())
                {
                    if (unit.imaginedCompanion > 0)
                    {
                        existsActiveImaginer = true;
                        break;
                    }
                }
                if (!existsActiveImaginer)
                {
                    GameObject imaginedCompanionToken = zoneInfo.GetObjectiveToken("ImaginedCompanion");
                    imaginedCompanionToken.GetComponent<Token>().TokenButtonClicked(imaginedCompanionToken.GetComponent<Button>());
                }
            }

            zoneInfo.DestroyFadedTokensAndUnits();
        }
    }

    public void GoBackATurn()
    {
        ScenarioSave scenarioSave = JsonUtility.FromJson<ScenarioSave>(File.ReadAllText(Application.persistentDataPath + "/" + (currentRound - 1).ToString() + missionName + ".json"));
        LoadScenarioSave(scenarioSave);
        UIOverlay.GetComponent<UIOverlay>().CloseMenu();
        ConfigureUnitHeroAndTokenInteractivity();
    }

    public void SaveIntoJson()
    {
        ScenarioSave scenarioToSave = new ScenarioSave(this);
        string scenarioAsJson = JsonUtility.ToJson(scenarioToSave);

        //Debug.Log("ScenarioAsJson: " + JsonUtility.ToJson(scenarioToSave, true));  // scenarioAsJson is minimum size, this one is pretty printed.
        PlayerPrefs.SetInt(missionName, currentRound);  // Update for the mission select screen to know which round to Continue
        PlayerPrefs.Save();
        string filename = "/" + currentRound.ToString() + missionName + ".json";
        //string filename = "/" + missionName + ".json";  // This file path (without being prepended by round number) is the initial game state for this scenario.
        System.IO.File.WriteAllText(Application.persistentDataPath + filename, scenarioAsJson);
    }

    public void LoadScenarioSave(ScenarioSave scenarioSave)
    {
        missionName = scenarioSave.missionName;
        currentRound = scenarioSave.currentRound;
        MissionSpecifics.currentRound = currentRound;
        reinforcementPoints = scenarioSave.reinforcementPoints;
        UIOverlay.GetComponent<UIOverlay>().utilityBelt.GetComponent<UtilityBelt>().LoadUtilityBeltSave(scenarioSave.utilityBelt);
        //UnitIntel.LoadUnitIntelSave(scenarioSave.unitIntel);
        UIOverlay.GetComponent<UIOverlay>().SetClock(currentRound);

        villainRiver = new List<string>(scenarioSave.villainRiver);
        foreach (string enemyName in villainRiver)  // You can't choose an ally that's working for the villain
        {
            if (potentialAlliesList.Contains(enemyName))
            {
                potentialAlliesList.Remove(enemyName);
            }
        }
        unitTagsMasterList = new List<string>(scenarioSave.unitTagsMasterList);  // Is this needed anymore?

        unitsPool = new UnitPool[scenarioSave.unitsPool.Count];
        for (int i = 0; i < scenarioSave.unitsPool.Count; i++)
        {
            GameObject unitPrefab = unitPrefabsMasterDict[scenarioSave.unitsPool[i].tag];
            if (unitPrefab != null)
            {
                unitsPool[i] = new UnitPool(unitPrefab, scenarioSave.unitsPool[i].total);
            }
            else
            {
                Debug.LogError("ERROR! In ZoneInfo.LoadZoneSave(), unable to find prefab asset for " + scenarioSave.unitsPool[i].tag + " for " + transform.name);
            }
        }

        GameObject[] allWallRubbleList = GameObject.FindGameObjectsWithTag("WallRubble");
        foreach (GameObject brokenWall in allWallRubbleList)
        {
            WallRubble wallRubble = brokenWall.GetComponent<WallRubble>();
            wallRubble.RebuildWall();  // Needed for walls that are no longer broken, or to prevent double breaking of a wall when going back a turn
            if (scenarioSave.brokenWalls.Contains(brokenWall.name))
            {
                wallRubble.BreakWall();
            }
        }

        GameObject[] randomizedZones = GameObject.FindGameObjectsWithTag("ZoneInfoPanel");
        List<GameObject> zones = randomizedZones.ToList();
        zones.Sort((x, y) => x.GetComponent<ZoneInfo>().id.CompareTo(y.GetComponent<ZoneInfo>().id));  // Sorts from lowest zone id to highest
        for (int i = 0; i < scenarioSave.zones.Count; i++)
        {
            GameObject currentZone = zones[i];
            currentZone.GetComponent<ZoneInfo>().LoadZoneSave(scenarioSave.zones[i]);
            if (currentZone.GetComponent<ZoneInfo>().isSpawnZone)
            {
                spawnZones.Add(currentZone);
            }
        }

        heroes = new List<GameObject>();  // Reset heroes list
        foreach (HeroSave heroSave in scenarioSave.heroes)
        {
            GameObject hero = Instantiate(heroPrefab, transform);  // If not made child of ScenarioMap, scale goes crazy
            hero.GetComponent<Hero>().LoadHeroSave(heroSave);
            GameObject heroZone = zones[heroSave.zoneID];
            heroZone.GetComponent<ZoneInfo>().AddHeroToZone(hero);
            heroes.Add(hero);
        }
    }
}


[Serializable]
public class ScenarioSave
{
    public string version;
    public string missionName;
    public int currentRound;
    public int reinforcementPoints;
    public List<HeroSave> heroes = new List<HeroSave>();
    public UtilityBeltSave utilityBelt;
    //public UnitIntelSave unitIntel;
    public List<string> villainRiver = new List<string>();
    public List<string> unitTagsMasterList = new List<string>();
    public List<string> brokenWalls = new List<string>();
    public List<ZoneSave> zones = new List<ZoneSave>();

    // public Dictionary<string, int> unitsPool = new Dictionary<string, int>();  // Ex: {"UZI": 2, "CROWBAR": 0,...}
    [Serializable]  // But dictionaries aren't serializable so here we are again. Still calling this __Dict as it does have a string and a value, just not key-value relationship.
    public struct UnitPoolDict
    {
        public string tag;
        public int total;

        public UnitPoolDict(string newTag, int newTotal)
        {
            tag = newTag;
            total = newTotal;
        }
    }
    public List<UnitPoolDict> unitsPool = new List<UnitPoolDict>();

    public ScenarioSave(ScenarioMap scenarioMap)
    {
        version = SceneHandler.version;  // Checked in MissionSelection.cs on start screen. If versions don't match, those save files can't be continued (user may only select "New")
        missionName = scenarioMap.missionName;
        currentRound = scenarioMap.currentRound;
        reinforcementPoints = scenarioMap.reinforcementPoints;
        foreach (GameObject hero in scenarioMap.heroes)
        {
            heroes.Add(hero.GetComponent<Hero>().ToJSON());
        }
        utilityBelt = scenarioMap.UIOverlay.GetComponent<UIOverlay>().utilityBelt.GetComponent<UtilityBelt>().ToJSON();
        //unitIntel = UnitIntel.ToJSON();
        villainRiver.AddRange(scenarioMap.villainRiver);
        unitTagsMasterList.AddRange(scenarioMap.unitTagsMasterList);
        foreach (ScenarioMap.UnitPool unitPool in scenarioMap.unitsPool)
        {  // Must be added one at a time because List<UnitPool> can't convert its GameObject unit to JSON, so need List<UnitPoolDict> with strings instead.
            unitsPool.Add(new UnitPoolDict(unitPool.unit.tag, unitPool.total));
        }

        foreach (GameObject brokenWall in GameObject.FindGameObjectsWithTag("WallRubble"))
        {
            WallRubble wallRubble = brokenWall.GetComponent<WallRubble>();
            if (wallRubble.WallIsBroken())
            {
                brokenWalls.Add(brokenWall.name);
            }
        }

        GameObject[] randomizedZones = GameObject.FindGameObjectsWithTag("ZoneInfoPanel");
        List<GameObject> orderedZones = randomizedZones.ToList();
        orderedZones.Sort((x, y) => x.GetComponent<ZoneInfo>().id.CompareTo(y.GetComponent<ZoneInfo>().id));  // Sorts from lowest zone id to highest
        foreach (GameObject zone in orderedZones)
        {
            zones.Add(new ZoneSave(zone.GetComponent<ZoneInfo>()));
        }
    }
}
