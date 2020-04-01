﻿using System;  // for Math.abs()
using System.Collections;
using System.Collections.Generic;
using System.IO;  // For File.ReadAllText for loading json save files
using UnityEngine;
using UnityEditor;  // For AssetDatabase.LoadAssetAtPath() for getting unit prefabs

public class ScenarioMap : MonoBehaviour
{
    readonly System.Random random = new System.Random();

    [SerializeField]
    GameObject clockHand;
    [SerializeField]
    List<GameObject> spawnZones;
    [SerializeField]
    GameObject sceneHandler;
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
    public UnitPool[] unitsPool;  // Unity can't expose dictionaries in the inspector, so Dictionary<string, GameObject[]> actionProficiencies not possible without addon

    public string missionName = "To Sink A City";
    public int currentRound = 1;  // Used by ScenarioMapSave at bottom
    public int reinforcementPoints = 5;


    void Start()
    {
        float startingClockHandAngle = -(currentRound * 30) + 2;
        clockHand.transform.eulerAngles = new Vector3(0, 0, startingClockHandAngle);
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

        SaveIntoJson();  // Do this before CleanupZones() in case player wants to go back to just before they ended their turn.
        CleanupZones();
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

    public void StartHeroTurn()
    {
        CleanupZones();
        float currentClockHandAngle = -(currentRound * 30) + 2;
        currentRound += 1;
        float newClockHandAngle = -(currentRound * 30) + 2;
        StartCoroutine(TurnClockHand(currentClockHandAngle, newClockHandAngle));
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
                        Destroy(unit);
                    }
                }
            }
        }
    }

    public void GoBackATurn()
    {
        //sceneHandler.GetComponent<SceneHandler>().LoadScene();
        ScenarioSave scenarioSave = JsonUtility.FromJson<ScenarioSave>(File.ReadAllText(Application.persistentDataPath + "/" +  (currentRound-1).ToString() + missionName +   ".json"));
        LoadScenarioSave(scenarioSave);
    }

    public void SaveIntoJson()
    {
        ScenarioSave scenarioToSave = new ScenarioSave(this);
        string scenarioAsJson = JsonUtility.ToJson(scenarioToSave);

        Debug.Log("ScenarioAsJson: " + JsonUtility.ToJson(scenarioToSave, true));  // scenarioAsJson is minimum size, this one is pretty printed.
        string filename = "/" + currentRound.ToString() + missionName + ".json";
        System.IO.File.WriteAllText(Application.persistentDataPath + filename, scenarioAsJson);
    }

    public void LoadScenarioSave(ScenarioSave scenarioSave)
    {
        missionName = scenarioSave.missionName;
        reinforcementPoints = scenarioSave.reinforcementPoints;
        currentRound = scenarioSave.currentRound;
        float startingClockHandAngle = -(currentRound * 30) + 2;
        clockHand.transform.eulerAngles = new Vector3(0, 0, startingClockHandAngle);  // Fix clockhand without animation. Not sure if this is needed though.
        villainRiver = new List<string>(scenarioSave.villainRiver);

        unitsPool = new UnitPool[scenarioSave.unitsPool.Count];
        for (int i = 0; i < scenarioSave.unitsPool.Count; i++)
        {
            string assetPath = "Assets/Prefabs/Units/" + scenarioSave.unitsPool[i].tag + ".prefab";
            GameObject unitPrefab = (GameObject)AssetDatabase.LoadAssetAtPath(assetPath, typeof(GameObject));
            if (unitPrefab != null)
            {
                unitsPool[i] = new UnitPool(unitPrefab, scenarioSave.unitsPool[i].total);
            }
            else
            {
                Debug.LogError("ERROR! In ZoneInfo.LoadZoneSave(), unable to find prefab asset for " + scenarioSave.unitsPool[i].tag + " for " + transform.name + " at " + assetPath);
            }
        }

        GameObject[] zones = GameObject.FindGameObjectsWithTag("ZoneInfoPanel");
        int zoneIndex = 0;
        foreach (ZoneSave zoneSave in scenarioSave.zones)
        {
            zones[zoneIndex].GetComponent<ZoneInfo>().LoadZoneSave(zoneSave);
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
    public List<string> villainRiver = new List<string>();
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
        villainRiver.AddRange(scenarioMap.villainRiver);
        foreach (GameObject zone in GameObject.FindGameObjectsWithTag("ZoneInfoPanel"))
        {
            zones.Add(new ZoneSave(zone.GetComponent<ZoneInfo>()));
        }

        foreach (ScenarioMap.UnitPool unitPool in scenarioMap.unitsPool)
        {  // Must be added one at a time because List<UnitPool> can't convert its GameObject unit to JSON, so need List<UnitPoolDict> with strings instead.
            unitsPool.Add(new UnitPoolDict(unitPool.unit.tag, unitPool.total));
        }
    }
}
