using System;  // for Math.abs()
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;  // for updating henchmen quantity from UnitRows
using System.Linq;  // for dictionary.ElementAt

public class ScenarioMap : MonoBehaviour
{
    readonly System.Random random = new System.Random();

    [SerializeField]
    GameObject clockHand;

    private int currentRound = 1;
    private readonly float uncoverTime = 3.0f;

    // Start is called before the first frame update
    void Start()
    {
        float startingClockHandAngle = -(currentRound * 30) + 2;
        clockHand.transform.eulerAngles = new Vector3(0, 0, startingClockHandAngle);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void StartHeroTurn()
    {
        float currentClockHandAngle = -(currentRound * 30) + 2;
        currentRound += 1;
        float newClockHandAngle = -(currentRound * 30) + 2;
        StartCoroutine(TurnClockHand(currentClockHandAngle, newClockHandAngle));
    }

    IEnumerator TurnClockHand(float currentAngle, float newAngle)
    {
        float t = 0;
        var uncoverTime = 2;

        while (t < 1f)
        {
            t += Time.deltaTime * uncoverTime;

            float angle = Mathf.LerpAngle(currentAngle, newAngle, t);
            clockHand.transform.eulerAngles = new Vector3(0, 0, angle);

            yield return null;
        }

        yield return 0;
    }

    // TODO make both of  these dictionary  mappings. We could also add ID, skills, attack
    //private string[] unitMapping = new string[] { "UZI", "CHAIN", "PISTOLS", "REINFORCEMENT", "CROWBAR", "SHOTGUN" };  // Must match order and naming of UnitRows in ZoneInfoPanel prefab
    //private string[] villainMapping = new string[] { "BARN", "SUPERBARN" };  // Must match order and naming of VillainRows in ZoneInfoPanel prefab

    Queue<string> villainRiver = new Queue<string>(new string[] { "UZI", "CHAINS", "PISTOLS", /*"REINFORCEMENT",*/ "CROWBAR", "SHOTGUN", "BARN" });
    //private string[] villainRiver = new string[] { "UZI", "CHAIN", "PISTOLS", "REINFORCEMENT", "CROWBAR", "SHOTGUN", "BARN" };
    private string[] actionsPrioritized = new string[] { "ACTIVATE", "ATTACK" };  //, "DEFEND", "MANEUVER" };
    public void EndHeroTurn()
    {
        for (int i = 0; i < 2; i++)
        {
            string unitTag = villainRiver.Dequeue();
            foreach (GameObject unit in GameObject.FindGameObjectsWithTag(unitTag))
            {
                Unit unitInfo = unit.GetComponent<Unit>();
                GameObject currentZone = unit.transform.parent.gameObject;
                ZoneInfo currentZoneInfo = currentZone.GetComponent<ZoneInfo>();

                Dictionary<GameObject, int> possibleDestinations = getPossibleDestinations(currentZone, unitInfo, 0);
                if (possibleDestinations.Count > 0)
                {
                    // // Below for debugging getPossibleDestinations()
                    //string possibleDestinationsDebugString = unitInfo.name;
                    //foreach (KeyValuePair<GameObject, int> pair in possibleDestinations)
                    //{
                    //    possibleDestinationsDebugString += "  ZONE: " + pair.Key.name + " - " + pair.Value.ToString();
                    //    //Debug.Log(pair.Key.name + pair.Value.ToString());
                    //}
                    //Debug.Log(possibleDestinationsDebugString);

                    // TODO instead of destinationZone being random, choose the one with the highest priority target
                    GameObject destinationZone = possibleDestinations.ElementAt(random.Next(possibleDestinations.Count)).Key;
                    Debug.Log("Moving " + unitTag + " from " + currentZone.name + " to " + destinationZone.name);
                    if (unitTag == "BARN" || unitTag == "SUPERBARN")
                    {
                        foreach (Transform row in currentZone.transform)
                        {
                            if (row.CompareTag(unitTag))
                            {
                                TMP_Text unitNumber = row.Find("UnitNumber").GetComponent<TMP_Text>();
                                row.gameObject.SetActive(false);
                                break;
                            }
                        }
                        foreach (Transform row in destinationZone.transform)
                        {
                            if (row.CompareTag(unitTag))
                            {
                                TMP_Text unitNumber = row.Find("UnitNumber").GetComponent<TMP_Text>();
                                unitNumber.text = unitInfo.lifePoints.ToString();
                                row.gameObject.SetActive(true);
                                break;
                            }
                        }
                    }
                    else
                    {
                        foreach (Transform row in currentZone.transform)
                        {
                            if (row.CompareTag(unitTag))
                            {
                                TMP_Text unitNumber = row.Find("UnitNumber").GetComponent<TMP_Text>();

                                unitNumber.text = (Int32.Parse(unitNumber.text) - 1).ToString();
                                if (unitNumber.text == "0")
                                {
                                    row.gameObject.SetActive(false);
                                }
                                break;
                            }
                        }
                        foreach (Transform row in destinationZone.transform)
                        {
                            if (row.CompareTag(unitTag))
                            {
                                TMP_Text unitNumber = row.Find("UnitNumber").GetComponent<TMP_Text>();
                                unitNumber.text = (Int32.Parse(unitNumber.text) + 1).ToString();
                                row.gameObject.SetActive(true);
                                break;
                            }
                        }
                    }
                }
            }
        }
    }

    private Dictionary<GameObject, int> getPossibleDestinations(GameObject currentZone, Unit unitInfo, int movePointsPreviouslyUsed, Dictionary<GameObject, int> possibleDestinations = null)
    {
        if (possibleDestinations is null)
        {
            possibleDestinations = new Dictionary<GameObject, int>{{ currentZone, 0 }};
        }

        ZoneInfo currentZoneInfo = currentZone.GetComponent<ZoneInfo>();
        List<GameObject> reachableZones = new List<GameObject>();

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
            if (currentZoneInfo.steeplyAdjacentZones.Contains(potentialZone)) {
                terrainDifficultyCost = currentZoneInfo.terrainDifficulty >= unitInfo.ignoreTerrainDifficulty ? currentZoneInfo.terrainDifficulty - unitInfo.ignoreTerrainDifficulty : 0;
            }

            int elevationCost = Math.Abs(currentZoneInfo.elevation - potentialZoneInfo.elevation);
            elevationCost = elevationCost >= unitInfo.ignoreElevation ? elevationCost - unitInfo.ignoreElevation : 0;

            int sizeCost = currentZoneInfo.GetHeroesCount();  // TODO Stop assuming size 1 for each Hero
            sizeCost = sizeCost >= unitInfo.ignoreSize ? sizeCost - unitInfo.ignoreSize : 0;
            int totalMovementCost = 1 + terrainDifficultyCost + elevationCost + sizeCost + movePointsPreviouslyUsed;
            if (unitInfo.movePoints >= totalMovementCost)  // if unit can move here
            {
                if (possibleDestinations.ContainsKey(potentialZone))
                {
                    if (possibleDestinations[potentialZone] > totalMovementCost)
                    {
                        possibleDestinations[potentialZone] = totalMovementCost;
                        if (unitInfo.movePoints > totalMovementCost)
                        {
                            possibleDestinations = getPossibleDestinations(potentialZone, unitInfo, totalMovementCost, possibleDestinations);
                        }
                    }
                }
                else
                {
                    possibleDestinations[potentialZone] = totalMovementCost;
                    if (unitInfo.movePoints > totalMovementCost)
                    {
                        possibleDestinations = getPossibleDestinations(potentialZone, unitInfo, totalMovementCost, possibleDestinations);
                    }
                }
            }
        }
        return possibleDestinations;
    }
}
