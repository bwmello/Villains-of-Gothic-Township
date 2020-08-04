using System;  // for Math.abs()
using System.Collections;
using System.Collections.Generic;
using System.IO;  // For File.ReadAllText for loading json save files
using UnityEngine;
using UnityEngine.UI;  // For button
using UnityEngine.SceneManagement;  // For SceneManager
//using UnityEditor;  // For AssetDatabase.LoadAssetAtPath() for getting unit prefabs
using TMPro;  // for TMP_Text to edit SuccessVsFailure's successContainer blanks

public class ScenarioMap : MonoBehaviour
{
    readonly System.Random random = new System.Random();

    [SerializeField]
    GameObject roundClock;
    GameObject clockHand;
    GameObject clockTurnBack;
    [SerializeField]
    List<GameObject> unitPrefabsMasterList;
    private List<GameObject> spawnZones = new List<GameObject>();
    public Dictionary<string, GameObject> unitPrefabsMasterDict;
    public List<string> villainRiver;
    public List<string> unitTagsMasterList;

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
    public int totalHeroes;
    
    private GameObject mainCamera;
    private GameObject animationContainer;
    private Animate animate;
    public GameObject bombPrefab;  // These prefabs are here just to pass off to static MissionSpecifics
    public GameObject primedBombPrefab;

    public delegate IEnumerator ActionCallback(GameObject unit, GameObject target, int totalSuccesses, int requiredSuccesses);
    public Dictionary<string, List<(string, int, double, ActionCallback)>> missionSpecificActionsWeightTable = new Dictionary<string, List<(string, int, double, ActionCallback)>>() {
        { "MELEE", new List<(string, int, double, ActionCallback)>() { (null, 0, 10, null) } },  // 10 * averageTotalWounds
        { "RANGED", new List<(string, int, double, ActionCallback)>() { (null, 0, 10, null) } }
    };


    void Awake()
    {
        MissionSpecifics.bombPrefab = bombPrefab;
        MissionSpecifics.primedBombPrefab = primedBombPrefab;
        clockHand = roundClock.transform.Find("ClockHand").gameObject;
        clockTurnBack = roundClock.transform.Find("TurnBackButton").gameObject;
        unitPrefabsMasterDict = new Dictionary<string, GameObject>();
        foreach (GameObject unitPrefab in unitPrefabsMasterList)
        {
            unitPrefabsMasterDict[unitPrefab.name] = unitPrefab;
        }
        ScenarioSave scenarioSave = JsonUtility.FromJson<ScenarioSave>(File.ReadAllText(Application.persistentDataPath + "/" + SceneHandler.saveName));
        LoadScenarioSave(scenarioSave);
    }

    void Start()
    {
        mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
        animationContainer = GameObject.FindGameObjectWithTag("AnimationContainer");
        animate = animationContainer.GetComponent<Animate>();
        //SaveIntoJson();  // Used along with SaveIntoJson commented out file path for setting initial game state save json (loaded in Awake())
        SetMissionSpecificActionsWeightTable();  // May also work if instead put in Awake()

        if (currentRound < 1)  // Start with villain's turn
        {
            DisablePlayerUI();
            CameraToFixedZoom();
            SetMissionSpecificActionsWeightTable();
            StartCoroutine(StartVillainTurn());
        }
    }

    void SetMissionSpecificActionsWeightTable()
    {
        int totalBombs;
        int totalPrimedBombs;
        int totalComputers;
        switch (missionName)  // Each nonconstant key (ex: "THOUGHT") should be wiped each time and only added back in if conditions are still met
        {
            case "ASinkingFeeling":
                totalBombs = MissionSpecifics.GetTotalActiveTokens(new List<string>() { "Bomb" });
                totalPrimedBombs = MissionSpecifics.GetTotalActiveTokens(new List<string>() { "PrimedBomb" });
                totalComputers = MissionSpecifics.GetTotalActiveTokens(new List<string>() { "Computer" });

                missionSpecificActionsWeightTable["MANIPULATION"] = new List<(string, int, double, ActionCallback)>();
                if (totalBombs > 0)
                {    // 60 * GetChanceOfSuccess(), which returns 1 for a 50/50 chance (where averageSuccesses = requiredSuccesses). CROWBARS have a .55556 chance of priming a bomb unhindered, which gives a weight of 33.33333.
                    missionSpecificActionsWeightTable["MANIPULATION"].Add(("Bomb", 3, 60, PrimeBombManually));
                }

                missionSpecificActionsWeightTable["THOUGHT"] = new List<(string, int, double, ActionCallback)>();
                if (totalComputers > 0 && totalBombs > 0)
                {
                    missionSpecificActionsWeightTable["THOUGHT"].Add(("Computer", 1, 60, PrimeBombRemotely));
                }

                missionSpecificActionsWeightTable["GUARD"] = new List<(string, int, double, ActionCallback)>();
                if (totalPrimedBombs > 0)
                {
                    missionSpecificActionsWeightTable["GUARD"].Add(("PrimedBomb", 0, 15, null));  // Flat weight bonus for hindering heroes attempting to disable objectives
                }
                if (totalBombs > 0)
                {
                    missionSpecificActionsWeightTable["GUARD"].Add(("Bomb", 0, 10, null));
                    if (totalComputers > 0)
                    {
                        missionSpecificActionsWeightTable["GUARD"].Add(("Computer", 0, 5, null));
                    }
                }
                break;
            case "IceToSeeYou":
                totalBombs = MissionSpecifics.GetTotalActiveTokens(new List<string>() { "Bomb" });
                totalComputers = MissionSpecifics.GetTotalActiveTokens(new List<string>() { "Computer" });

                missionSpecificActionsWeightTable["MANIPULATION"] = new List<(string, int, double, ActionCallback)>() { ("Grenade", 0, 20, null) };  // 20 * averageAutoWounds

                missionSpecificActionsWeightTable["THOUGHT"] = new List<(string, int, double, ActionCallback)>();
                if (totalComputers > 0)
                {
                    missionSpecificActionsWeightTable["THOUGHT"].Add(("Computer", 3, 20, ActivateCryogenicDevice));  // Assume you cost the hero at least 1 movepoint and auto deal 2/3 of a wound per cryogenic token, 0 weight if would hit anyone except frosty
                }

                missionSpecificActionsWeightTable["GUARD"] = new List<(string, int, double, ActionCallback)>();
                if (totalBombs > 0)
                {
                    missionSpecificActionsWeightTable["GUARD"].Add(("Bomb", 0, 15, null));
                }
                break;
        }
    }

    /* ActionCallbacks specific to "ASinkingFeeling" mission */
    public IEnumerator PrimeBombManually(GameObject unit, GameObject target, int totalSuccesses, int requiredSuccesses)
    {
        ZoneInfo unitZoneInfo = unit.GetComponent<Unit>().GetZone().GetComponent<ZoneInfo>();
        // Animate success vs failure UI
        GameObject successContainer = Instantiate(unitZoneInfo.successVsFailurePrefab, animationContainer.transform);
        successContainer.transform.position = unit.transform.TransformPoint(new Vector3(0, 12, 0));

        successContainer.GetComponent<RectTransform>().sizeDelta = new Vector2(requiredSuccesses * 10, successContainer.GetComponent<RectTransform>().rect.height);
        Transform successContainerText = successContainer.transform.GetChild(0);
        successContainerText.GetComponent<RectTransform>().sizeDelta = new Vector2(requiredSuccesses * 10, successContainerText.GetComponent<RectTransform>().rect.height);
        string successContainerBlanks = "_";
        for (int i = 1; i < requiredSuccesses; i++)
        {
            successContainerBlanks += " _";
        }
        successContainerText.GetComponent<TMP_Text>().text = successContainerBlanks;

        for (int i = -1; i < (totalSuccesses >= 0 ? totalSuccesses : 0); i++)
        {
            yield return new WaitForSecondsRealtime(1);
            if (i == requiredSuccesses - 1)  // Otherwise there can be more results (checks/x's) displayed than requiredSuccesses
            {
                break;
            }
            GameObject successOrFailurePrefab = i + 1 < totalSuccesses ? unitZoneInfo.successPrefab : unitZoneInfo.failurePrefab;
            GameObject successOrFailureMarker = Instantiate(successOrFailurePrefab, successContainer.transform);
        }
        yield return new WaitForSecondsRealtime(1);

        if (totalSuccesses >= requiredSuccesses)
        {
            //yield return StartCoroutine(MoveObjectOverTime(new List<GameObject>() { mainCamera }, mainCamera.transform.position, currentZoneInfo.GetBomb().transform.position));  // Moving camera over slightly to bomb being armed is more jarring than anything else
            unitZoneInfo.PrimeBomb();
            yield return new WaitForSecondsRealtime(2);
            SetMissionSpecificActionsWeightTable();
        }
        Destroy(successContainer);
        yield return 0;
    }
    public IEnumerator PrimeBombRemotely(GameObject unit, GameObject target, int totalSuccesses, int requiredSuccesses)
    {
        ZoneInfo unitZoneInfo = unit.GetComponent<Unit>().GetZone().GetComponent<ZoneInfo>();
        // Animate success vs failure UI
        GameObject successContainer = Instantiate(unitZoneInfo.successVsFailurePrefab, animationContainer.transform);
        successContainer.transform.position = unit.transform.TransformPoint(new Vector3(0, 12, 0));

        successContainer.GetComponent<RectTransform>().sizeDelta = new Vector2(requiredSuccesses * 10, successContainer.GetComponent<RectTransform>().rect.height);
        Transform successContainerText = successContainer.transform.GetChild(0);
        successContainerText.GetComponent<RectTransform>().sizeDelta = new Vector2(requiredSuccesses * 10, successContainerText.GetComponent<RectTransform>().rect.height);
        string successContainerBlanks = "_";
        for (int i = 1; i < requiredSuccesses; i++)
        {
            successContainerBlanks += " _";
        }
        successContainerText.GetComponent<TMP_Text>().text = successContainerBlanks;

        for (int i = -1; i < (totalSuccesses >= 0 ? totalSuccesses : 0); i++)
        {
            yield return new WaitForSecondsRealtime(1);
            if (i == requiredSuccesses - 1)  // Otherwise there can be more results (checks/x's) displayed than requiredSuccesses
            {
                break;
            }
            GameObject successOrFailurePrefab = i + 1 < totalSuccesses ? unitZoneInfo.successPrefab : unitZoneInfo.failurePrefab;
            GameObject successOrFailureMarker = Instantiate(successOrFailurePrefab, successContainer.transform);
        }
        yield return new WaitForSecondsRealtime(1);

        if (totalSuccesses >= requiredSuccesses)
        {
            List<ZoneInfo> bombZones = new List<ZoneInfo>();
            foreach (GameObject bomb in GameObject.FindGameObjectsWithTag("Bomb"))
            {
                bombZones.Add(bomb.transform.parent.parent.GetComponentInParent<ZoneInfo>());
            }
            ZoneInfo chosenBombZone = null;
            double leastManipulationChance = 100;
            foreach (ZoneInfo bombZone in bombZones)
            {
                double bombZoneManipulationChance = bombZone.GetOccupantsManipulationLikelihood(unit);
                //Debug.Log("!!!" + bombZone.transform.name + " with bombZoneManipulationChance: " + bombZoneManipulationChance.ToString());
                if (bombZoneManipulationChance < leastManipulationChance)
                {
                    leastManipulationChance = bombZoneManipulationChance;
                    chosenBombZone = bombZone;
                }
            }
            if (chosenBombZone != null)
            {
                unitZoneInfo.RemoveComputer();
                yield return StartCoroutine(animate.MoveObjectOverTime(new List<GameObject>() { mainCamera }, mainCamera.transform.position, chosenBombZone.GetBomb().transform.position));  // Move camera to bomb being armed
                chosenBombZone.PrimeBomb();
                yield return new WaitForSecondsRealtime(2);
                //unitTurn.targetedZone = chosenBombZone.transform.gameObject;  // Only useful for DEBUG statement at end of PerformAction()
            }
            else
            {
                Debug.LogError("ERROR! Tried to use a computer from " + unitZoneInfo.name + " to prime a bomb, but no zones with bombs available. Why was computer able to be used if there are no zones with bombs?");
            }
            SetMissionSpecificActionsWeightTable();  // TODO test that the last bomb to be primed doesn't leave "Bomb" as a THOUGHT action for next unit due to mistiming
        }
        Destroy(successContainer);
        yield return 0;
    }

    /* ActionCallbacks specific to "IceToSeeYou" mission */
    public IEnumerator ActivateCryogenicDevice(GameObject unit, GameObject target, int totalSuccesses, int requiredSuccesses)
    {
        ZoneInfo unitZoneInfo = unit.GetComponent<Unit>().GetZone().GetComponent<ZoneInfo>();
        // Animate success vs failure UI
        GameObject successContainer = Instantiate(unitZoneInfo.successVsFailurePrefab, animationContainer.transform);
        successContainer.transform.position = unit.transform.TransformPoint(new Vector3(0, 12, 0));

        successContainer.GetComponent<RectTransform>().sizeDelta = new Vector2(requiredSuccesses * 10, successContainer.GetComponent<RectTransform>().rect.height);
        Transform successContainerText = successContainer.transform.GetChild(0);
        successContainerText.GetComponent<RectTransform>().sizeDelta = new Vector2(requiredSuccesses * 10, successContainerText.GetComponent<RectTransform>().rect.height);
        string successContainerBlanks = "_";
        for (int i = 1; i < requiredSuccesses; i++)
        {
            successContainerBlanks += " _";
        }
        successContainerText.GetComponent<TMP_Text>().text = successContainerBlanks;

        for (int i = -1; i < (totalSuccesses >= 0 ? totalSuccesses : 0); i++)
        {
            yield return new WaitForSecondsRealtime(1);
            if (i == requiredSuccesses - 1)  // Otherwise there can be more results (checks/x's) displayed than requiredSuccesses
            {
                break;
            }
            GameObject successOrFailurePrefab = i + 1 < totalSuccesses ? unitZoneInfo.successPrefab : unitZoneInfo.failurePrefab;
            GameObject successOrFailureMarker = Instantiate(successOrFailurePrefab, successContainer.transform);
        }
        yield return new WaitForSecondsRealtime(1);

        if (totalSuccesses >= requiredSuccesses)
        {
            List<(double, GameObject)> cryoZoneTargets = new List<(double, GameObject)>();

            GameObject hero = GameObject.FindGameObjectWithTag("1stHero");
            GameObject heroZone = hero.GetComponent<Hero>().GetZone();
            cryoZoneTargets.Add((30, heroZone));
            foreach (GameObject bomb in GameObject.FindGameObjectsWithTag("Bomb"))
            {
                GameObject bombZone = bomb.transform.parent.parent.gameObject;
                cryoZoneTargets.Add((15, bombZone));
                ZoneInfo bombZoneInfo = bombZone.GetComponent<ZoneInfo>();
                if ((bombZoneInfo.adjacentZones.Count + bombZoneInfo.steeplyAdjacentZones.Count) == 1)  // If only one way in or out (ignoring walls)
                {
                    if (bombZoneInfo.adjacentZones.Count == 1)
                    {
                        cryoZoneTargets.Add((20, bombZoneInfo.adjacentZones[0]));  // TODO Once hero has broken through any wall, adjust this
                    }
                    else
                    {
                        cryoZoneTargets.Add((20, bombZoneInfo.steeplyAdjacentZones[0]));
                    }
                }
            }
            for (int i = 0; i < cryoZoneTargets.Count - 1; i++)  // Subtract friendly fire from each target zone's weight
            {
                foreach (Unit inAreaUnit in cryoZoneTargets[i].Item2.GetComponent<ZoneInfo>().GetUnitsInfo())
                {
                    if (!inAreaUnit.frosty)
                    {
                        //cryoZoneTargets[i].Item1 -= 5;  // Doesn't work because I think .Item1 is a clone of the value ("return value is not a variable")
                        cryoZoneTargets[i] = (cryoZoneTargets[i].Item1 - 9, cryoZoneTargets[i].Item2);
                    }
                }
            }
            cryoZoneTargets.Sort((x, y) => y.Item1.CompareTo(x.Item1));  // Sorts by doubles in descending order

            //string cryoDebugString = "";
            //foreach ((double, GameObject) cryoZoneTarget in cryoZoneTargets)
            //{
            //    cryoDebugString += "cryoZoneTarget: " + cryoZoneTarget.Item2.name + " worth " + cryoZoneTarget.Item1 + ",   ";
            //}
            //Debug.Log("!!!ActivateCryogenicDevice of ScenarioMap, cryoDebugString: " + cryoDebugString);

            for (int i = 0; i < cryoZoneTargets.Count-1 && i < 2; i++)
            {
                GameObject zoneToCryo = cryoZoneTargets[i].Item2;
                yield return StartCoroutine(animate.MoveObjectOverTime(new List<GameObject>() { mainCamera }, mainCamera.transform.position, zoneToCryo.transform.position));
                yield return new WaitForSecondsRealtime(1);
                yield return StartCoroutine(zoneToCryo.GetComponent<ZoneInfo>().AddEnvironTokens(new EnvironTokenSave("Cryogenic", 1, false, true)));
                yield return new WaitForSecondsRealtime(2);
            }
            unitZoneInfo.RemoveComputer();
            SetMissionSpecificActionsWeightTable();
        }
        Destroy(successContainer);
        yield return 0;
    }

    public void DisablePlayerUI()
    {
        foreach (Button button in transform.GetComponentsInChildren<Button>())
        {
            button.enabled = false;
        }
        foreach (GameObject wallRubble in GameObject.FindGameObjectsWithTag("WallRubble"))
        {
            wallRubble.GetComponent<WallRubble>().isClickable = false;
        }
    }

    public void CameraToFixedZoom()
    {
        Camera mainCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
        mainCamera.orthographicSize = 2.2f;
    }

    // The flow: Player uses UI to end turn -> EndHeroTurn() -> StartVillainTurn() -> StartHeroTurn() -> Player takes their next turn
    public void EndHeroTurn()
    {
        if (MissionSpecifics.IsGameOver(currentRound))
        {
            animate.ShowGameOver();
        }
        else
        {
            DisablePlayerUI();  // Disable all UI so Villain turn isn't interrupted
            CameraToFixedZoom();

            // Dredge the river, removing any tiles with 0 units on the map
            foreach (string unitTag in new List<string>(villainRiver))
            {
                if (unitTag != "REINFORCEMENT" && GameObject.FindGameObjectsWithTag(unitTag).Length == 0)
                {
                    Debug.Log("EndHeroTurn() DREDGING " + unitTag);
                    villainRiver.Remove(unitTag);
                }
            }

            SaveIntoJson();  // Do this before CleanupZones() in case player wants to go back to just before they ended their turn.
            CleanupZones();
            SetMissionSpecificActionsWeightTable();
            StartCoroutine(StartVillainTurn());
        }
    }

    IEnumerator StartVillainTurn()
    {
        DissipateEnvironTokens(false);
        yield return StartCoroutine(ActivateRiverTiles());

        if (MissionSpecifics.IsGameOver(currentRound))
        {
            animate.ShowGameOver();
        }
        else
        {
            // Disable camera controls
            PanAndZoom panAndZoom = this.GetComponent<PanAndZoom>();
            panAndZoom.controlCamera = false;
            yield return StartCoroutine(StartHeroTurn());

            // Reactivate camera controls
            panAndZoom.controlCamera = true;
        }
        yield return 0;
    }

    IEnumerator ActivateRiverTiles()
    {
        for (int i = 0; i < 2; i++)
        {
            string unitTypeToActivate = GetVillainTileToActivate(i);
            if (unitTypeToActivate == "REINFORCEMENT")
            {
                yield return StartCoroutine(CallReinforcements());
            }
            else
            {
                yield return StartCoroutine(ActivateUnitsWithTag(unitTypeToActivate));
            }

            villainRiver.Remove(unitTypeToActivate);
            villainRiver.Add(unitTypeToActivate);
        }
        yield return 0;
    }

    IEnumerator ActivateUnitsWithTag(string unitTypeToActivate)
    {
        foreach (GameObject unit in GameObject.FindGameObjectsWithTag(unitTypeToActivate))
        {
            Unit unitInfo = unit.GetComponent<Unit>();
            if (unitInfo.IsActive())  // Check if killed (but not yet removed) by previous unit's turn
            {
                yield return StartCoroutine(unit.GetComponent<Unit>().ActivateUnit(missionSpecificActionsWeightTable));
            }
        }
        yield return 0;
    }

    IEnumerator StartHeroTurn()
    {
        DissipateEnvironTokens(true);
        CleanupZones();
        GameObject mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
        mainCamera.transform.position = new Vector3(clockHand.transform.position.x, clockHand.transform.position.y, mainCamera.transform.position.z);
        float currentClockHandAngle = -(currentRound * 30) + 2;
        currentRound += 1;
        float newClockHandAngle = -(currentRound * 30) + 2;
        yield return StartCoroutine(TurnClockHand(currentClockHandAngle, newClockHandAngle));
        if (currentRound > 1)
        {
            SaveIntoJson();
            if (!clockTurnBack.activeSelf)
            {
                clockTurnBack.SetActive(true);
            }
        }

        // Re-Enable all the UI buttons which were disabled at EndHeroTurn()
        foreach (Button button in transform.GetComponentsInChildren<Button>())
        {
            button.enabled = true;
        }
        foreach (GameObject wallRubble in GameObject.FindGameObjectsWithTag("WallRubble"))
        {
            wallRubble.GetComponent<WallRubble>().isClickable = true;
        }
        yield return 0;
    }

    string GetVillainTileToActivate(int unitsAlreadyActivated)
    {
        //string debugVillainRiverString = "GetVillainTileToActivate  villainRiver: ";
        //foreach (string unitTag in villainRiver)
        //{
        //    debugVillainRiverString += " " + unitTag + " ";
        //}
        //Debug.Log(debugVillainRiverString);
        double totalWeightOfMostValuableUnitTurn = -1;  // Any negative number as work, still want to activate a unit even if their highest weight is 0
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
                    currentWeightOfUnitTurn += GetMissionSpecificReinforcementWeight();
                }
            }
            else
            {
                foreach (GameObject unit in GameObject.FindGameObjectsWithTag(unitTag))
                {
                    currentWeightOfUnitTurn += unit.GetComponent<Unit>().GetMostValuableActionWeight(missionSpecificActionsWeightTable);
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

    IEnumerator CallReinforcements()
    {
        GameObject mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
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
                GameObject availableUnitSlot = reinforcementsAvailable[i].Item3.GetComponent<ZoneInfo>().GetAvailableUnitSlot();
                mainCamera.transform.position = new Vector3(availableUnitSlot.transform.position.x, availableUnitSlot.transform.position.y, mainCamera.transform.position.z);
                yield return new WaitForSecondsRealtime(1);
                Instantiate(reinforcementsAvailable[i].Item1.unit, availableUnitSlot.transform);  // spawn Unit
                yield return new WaitForSecondsRealtime(1);
                Debug.Log("CallReinforcements() spawning " + reinforcementsAvailable[i].Item1.unit.tag + " at " + reinforcementsAvailable[i].Item3.name);
            }
            reinforcementPointsRemaining -= numToReinforce * unitInfo.reinforcementCost;
            i++;
        }

        yield return StartCoroutine(ActivateMissionSpecificReinforcement());
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
                            GameObject availableUnitSlot = spawnZone.GetComponent<ZoneInfo>().GetAvailableUnitSlot();
                            GameObject tempUnit = Instantiate(unitPool.unit, availableUnitSlot.transform);
                            double currentSpawnActionWeight = tempUnit.GetComponent<Unit>().GetMostValuableActionWeight(missionSpecificActionsWeightTable);
                            DestroyImmediate(tempUnit);
                            if (currentSpawnActionWeight > highestSpawnActionWeight)
                            {
                                chosenSpawnZone = spawnZone;
                                highestSpawnActionWeight = currentSpawnActionWeight;
                            }
                        }
                        if (chosenSpawnZone == null)  // If can't decide between spawnZones, randomly pick one
                        {
                            chosenSpawnZone = spawnZones[random.Next(spawnZones.Count)];
                        }
                        if (!activateableThisTurn)
                        {
                            highestSpawnActionWeight *= .9;  // Only 90% as valuable if Unit can't be activated this turn
                        }
                        reinforcementsAvailable.Add(new Tuple<UnitPool, double, GameObject>(new UnitPool(unitPool.unit, inReserve), highestSpawnActionWeight, chosenSpawnZone));
                    }
                }
            }
        }
        //Debug.Log(unitsPoolDebugString);
        reinforcementsAvailable.Sort((x, y) => x.Item2.CompareTo(y.Item2));  // Hopefully this sorts the list by the most valuable weight. https://stackoverflow.com/questions/4668525/sort-listtupleint-int-in-place

        string reinforcementsAvailableDebugString = "GetAvailableReinforcements() reinforcementsAvailable: [";
        foreach (Tuple<UnitPool, double, GameObject> reinforcement in reinforcementsAvailable)
        {
            reinforcementsAvailableDebugString += " { " + reinforcement.Item1.total + " of " + reinforcement.Item1.unit.tag + " with weight " + reinforcement.Item2 + " at spawn " + reinforcement.Item3 + "} ";
        }
        Debug.Log(reinforcementsAvailableDebugString + "]");

        return reinforcementsAvailable;
    }

    double GetMissionSpecificReinforcementWeight()
    {
        double weight = 0;
        switch (missionName)
        {
            case "ASinkingFeeling":
                GameObject superBarn = GameObject.FindGameObjectWithTag("SUPERBARN");
                if (superBarn != null)
                {
                    weight = superBarn.GetComponent<Unit>().GetMostValuableActionWeight(missionSpecificActionsWeightTable);
                }
                break;
        }
        return weight;
    }

    IEnumerator ActivateMissionSpecificReinforcement()
    {
        switch (missionName)
        {
            case "ASinkingFeeling":
                GameObject superBarn = GameObject.FindGameObjectWithTag("SUPERBARN");
                if (superBarn == null)
                {
                    GameObject barn = GameObject.FindGameObjectWithTag("BARN");
                    if (barn != null)
                    {
                        GameObject superbarnPrefab = unitPrefabsMasterDict["SUPERBARN"];
                        if (superbarnPrefab != null)
                        {
                            Unit barnInfo = barn.GetComponent<Unit>();
                            Unit superBarnInfo = Instantiate(superbarnPrefab, barn.transform.parent).GetComponent<Unit>();
                            superBarnInfo.GetComponent<Unit>().ModifyLifePoints(barnInfo.lifePoints - barnInfo.lifePointsMax);  // Do not reset SuperBarn's life points to 6 if Barn was damaged.
                            DestroyImmediate(barn);
                            int barnRiverIndex = villainRiver.IndexOf("BARN");
                            villainRiver[barnRiverIndex] = "SUPERBARN";
                        }
                        else
                        {
                            Debug.LogError("ERROR! In ScenarioMap.CallReinforcements(), unable to find prefab asset for SUPERBARN at UnitPrefabs/SUPERBARN.prefab");
                        }
                    }
                }
                else
                {
                    Unit superBarnInfo = superBarn.GetComponent<Unit>();
                    yield return StartCoroutine(superBarnInfo.ActivateUnit(missionSpecificActionsWeightTable));
                    superBarnInfo.ModifyLifePoints(-2);
                    if (superBarnInfo.lifePoints < 1)
                    {
                        Destroy(superBarn);
                    }
                }
                break;
        }
        yield return 0;
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
            zoneInfo.DestroyFadedTokens();
        }

        foreach (string unitTag in villainRiver)
        {
            if (unitTag != "REINFORCEMENT")
            {
                foreach (GameObject unit in GameObject.FindGameObjectsWithTag(unitTag))
                {
                    if (unit.GetComponent<CanvasGroup>().alpha < 1)
                    {
                        for (int i = 0; i < unitsPool.Length; i++)
                        {
                            if (unit == unitsPool[i].unit)
                            {
                                unitsPool[i].total -= 1;
                            }
                        }
                        DestroyImmediate(unit);
                    }
                }
            }
        }
    }

    public void GoBackATurn()
    {
        ScenarioSave scenarioSave = JsonUtility.FromJson<ScenarioSave>(File.ReadAllText(Application.persistentDataPath + "/" +  (currentRound-1).ToString() + missionName +   ".json"));
        LoadScenarioSave(scenarioSave);
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
        reinforcementPoints = scenarioSave.reinforcementPoints;
        totalHeroes = scenarioSave.totalHeroes;
        float startingClockHandAngle = -(currentRound * 30) + 2;
        clockHand.transform.eulerAngles = new Vector3(0, 0, startingClockHandAngle);
        if (!clockTurnBack.activeSelf && currentRound > 1)
        {
            clockTurnBack.SetActive(true);
        }
        else if (clockTurnBack.activeSelf && currentRound <= 1)
        {
            clockTurnBack.SetActive(false);
        }

        villainRiver = new List<string>(scenarioSave.villainRiver);
        unitTagsMasterList = new List<string>(scenarioSave.unitTagsMasterList);

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
            if (scenarioSave.brokenWalls.Contains(brokenWall.name))
            {
                wallRubble.BreakWall();
            }
            else
            {
                wallRubble.RebuildWall();  // Restores zone adjacency mapping and Destroy(brokenWall)
            }
        }

        GameObject[] zones = GameObject.FindGameObjectsWithTag("ZoneInfoPanel");
        int zoneIndex = 0;
        foreach (ZoneSave zoneSave in scenarioSave.zones)
        {
            GameObject currentZone = zones[zoneIndex];
            currentZone.GetComponent<ZoneInfo>().LoadZoneSave(zoneSave, totalHeroes);
            if (currentZone.GetComponent<ZoneInfo>().isSpawnZone)
            {
                spawnZones.Add(currentZone);
            }
            zoneIndex++;
        }
    }
}


[Serializable]
public class ScenarioSave
{
    public string missionName;
    public int currentRound;
    public int reinforcementPoints;
    public int totalHeroes;
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
        missionName = scenarioMap.missionName;
        currentRound = scenarioMap.currentRound;
        reinforcementPoints = scenarioMap.reinforcementPoints;
        totalHeroes = scenarioMap.totalHeroes;
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

        foreach (GameObject zone in GameObject.FindGameObjectsWithTag("ZoneInfoPanel"))
        {
            zones.Add(new ZoneSave(zone.GetComponent<ZoneInfo>()));
        }
    }
}
