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

    public int movePoints;
    public int ignoreTerrainDifficulty = 0;
    public int ignoreElevation = 0;
    public int ignoreSize = 0;

    public int size = 1;
    public int menace = 1;
    public int reinforcementCost = 1;

    [Serializable]
    public class ActionProficiency
    {
        public string action;
        public GameObject[] dice;
    }
    public ActionProficiency[] actionProficiencies;  // Unity can't expose dictionaries in the inspector, so Dictionary<string, GameObject[]> actionProficiencies not possible without addon
    readonly List<string> actionPriorities = new List<string>() { "MANIPULATION", "THOUGHT", "MELEE", "RANGED" };

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

            Dictionary<string, GameObject[]> actionProficiencies = GetValidActionProficiencies();
            ZoneInfo chosenDestination = null;
            string chosenAction = "";
            foreach (string actionType in actionPriorities)
            {
                if (actionProficiencies.ContainsKey(actionType))
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
                int actionSuccesses = 0;
                foreach (GameObject die in actionProficiencies[chosenAction])
                {
                    actionSuccesses += die.GetComponent<Dice>().RollDice(1);
                }
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

            int sizeCost = currentZoneInfo.GetHeroesCount();  // TODO Stop assuming size 1 for each Hero
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
                    //TMP_Text unitNumber = row.Find("UnitNumber").GetComponent<TMP_Text>();
                    row.gameObject.SetActive(false);
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
                    break;
                }
            }
        }
    }
}
