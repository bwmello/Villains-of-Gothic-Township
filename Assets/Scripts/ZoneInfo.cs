using System;  // For NullReferenceException
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;  // For getting henchmen quantity from UnitRows

public class ZoneInfo : MonoBehaviour
{
    public List<GameObject> adjacentZones;
    public List<GameObject> steeplyAdjacentZones;
    public List<GameObject> wall1AdjacentZones;
    public List<GameObject> wall2AdjacentZones;
    public List<GameObject> wall3AdjacentZones;
    public List<GameObject> wall4AdjacentZones;
    public List<GameObject> wall5AdjacentZones;
    public List<GameObject> lineOfSightZones;
    public Vector2[] unitPositions;

    public GameObject computerPrefab;
    public GameObject bombPrefab;
    public GameObject primedBombPrefab;
    public GameObject elevationDie;  // Determines fall damage and bonus for ranged attacks made from a higher elevation

    public int elevation;
    public int maxOccupancy;
    public int terrainDifficulty = 0;
    public int terrainDanger = 0;
    public int supportRerolls = 0;  // Determined by total Unit.supportRerolls of each unit in zone

    private void Start()
    {
        List<string>  unitTags = transform.GetComponentInParent<ScenarioMap>().villainRiver;  // Fetch this list every time you need it as ScenarioMap removes unit tiles by dredging river
        foreach (Transform row in transform)
        {
            if (unitTags.Contains(row.tag))
            {
                Unit unit = row.gameObject.GetComponent<Unit>();
                supportRerolls += unit.supportRerolls;
            }
        }
    }

    public int GetZoneID()
    {
        try
        {
            return Int32.Parse(transform.name.Remove(0, 14));
        }
        catch (FormatException err)
        {
            Debug.LogError("Failed to Parse " + transform.name + " to an int using: " + transform.name.Remove(0, 14) + "  Error details: " + err.ToString());
        }
        return -1;  // An invalid zone id
    }

    public int GetCurrentOccupancy()
    {
        int currentOccupancy = 0;
        currentOccupancy += GetHeroesCount();
        List<string> unitTags = transform.GetComponentInParent<ScenarioMap>().villainRiver;
        foreach (Transform row in transform)
        {
            if (unitTags.Contains(row.tag) && row.gameObject.activeSelf)
            {
                currentOccupancy += row.gameObject.GetComponent<Unit>().size;
            }
        }
        return currentOccupancy;
    }

    public int GetCurrentHindrance(GameObject unitToDiscount, bool isMoving = false)
    {
        int currentHindrance = 0;
        currentHindrance += GetHeroesCount();  // TODO stop assuming size and menace of 1 for each hero
        List<string> unitTags = transform.GetComponentInParent<ScenarioMap>().villainRiver;
        foreach (Transform row in transform)
        {
            if (unitTags.Contains(row.tag) && row.gameObject.activeSelf)
            {
                if (row.gameObject == unitToDiscount)
                {
                    continue;  // Unit doesn't count itself for hindrance, so skip it.
                }
                Unit unitInfo = row.gameObject.GetComponent<Unit>();
                currentHindrance -= isMoving ? unitInfo.size : unitInfo.menace;
            }
        }
        if (currentHindrance < 0)
        {
            currentHindrance = 0;
        }
        return currentHindrance;
    }

    public double GetOccupantsManipulationLikelihood(GameObject unitToDiscount)  // TODO get the odds of success instead of just adding up each unit's average successes
    {
        double manipulationOdds = 0;

        int currentHindrance = 0;
        currentHindrance += GetHeroesCount();  // TODO stop assuming size and menace of 1 for each hero
        List<string> unitTags = transform.GetComponentInParent<ScenarioMap>().villainRiver;
        foreach (Transform row in transform)
        {
            if (unitTags.Contains(row.tag))
            {
                if (row.gameObject == unitToDiscount)
                {
                    continue;  // Unit doesn't count itself for hindrance, so skip it.
                }
                Unit unitInfo = row.gameObject.GetComponent<Unit>();
                if (unitInfo.validActionProficiencies.ContainsKey("MANIPULATION"))
                {
                    foreach (GameObject die in unitInfo.validActionProficiencies["MANIPULATION"])
                    {
                        manipulationOdds += die.GetComponent<Dice>().averageSuccesses;
                    }
                }
                currentHindrance -= unitInfo.menace;
            }
        }
        if (currentHindrance < 0)
        {
            currentHindrance = 0;
        }
        manipulationOdds -= currentHindrance;
        if (manipulationOdds < 0)
        {
            manipulationOdds = 0;
        }

        return manipulationOdds;
    }

    public bool HasToken(string tokenName)
    {
        Transform tokensRow = transform.Find("TokensRow");
        return (tokensRow.Find(tokenName) != null);
    }

    public void DestroyFadedTokens()
    {
        Transform tokensRow = transform.Find("TokensRow");
        foreach (Transform token in tokensRow)
        {
            if (token.gameObject.GetComponent<CanvasGroup>().alpha < 1)
            {
                DestroyImmediate(token.gameObject);
            }
        }
    }

    public bool HasHeroes()
    {
        return GetHeroesCount() > 0 ? true : false;
    }

    public GameObject GetLineOfSightZoneWithHero()
    {
        List<ZoneInfo> targetableZones = new List<ZoneInfo>();
        if (HasHeroes())
        {
            targetableZones.Add(this);
        }
        foreach (GameObject zone in lineOfSightZones)
        {
            ZoneInfo losZoneInfo = zone.GetComponent<ZoneInfo>();
            if (losZoneInfo.HasHeroes())
            {
                if (elevation > losZoneInfo.elevation)
                {
                    return zone;
                }
                targetableZones.Add(losZoneInfo);
            }
        }
        if (targetableZones.Count > 0)
        {
            return targetableZones[0].gameObject;
        }
        return null;
    }

    public int GetHeroesCount()
    {
        int heroesCount = 0;
        Transform heroesRow = transform.Find("HeroesRow");
        foreach (CanvasGroup heroButtonCanvasGroup in heroesRow.GetComponentsInChildren<CanvasGroup>())
        {
            if (heroButtonCanvasGroup.alpha == 1)  // If button isn't transparent
            {
                heroesCount += 1;
            }
        }
        return heroesCount;
    }

    public void PrimeBomb()
    {
        Transform tokensRow = transform.Find("TokensRow");
        try
        {
            tokensRow.Find("Bomb").GetComponent<CanvasGroup>().alpha = (float).2;
        }
        catch (NullReferenceException err)
        {
            Debug.LogError("ERROR! Tried to prime a bomb in " + transform.name + " without a bomb there. Error details: " + err.ToString());
        }
        Instantiate(primedBombPrefab, tokensRow);
    }

    public void RemoveComputer()
    {
        Transform tokensRow = transform.Find("TokensRow");
        try
        {
            tokensRow.Find("Computer").GetComponent<CanvasGroup>().alpha = (float).2;
        }
        catch (NullReferenceException err)
        {
            Debug.LogError("ERROR! Tried to remove a computer in " + transform.name + " without a computer there. Error details: " + err.ToString());
        }
    }

    public void ReorganizeTokens()
    {
        Transform tokensRow = transform.Find("TokensRow");
        int totalTokens = tokensRow.childCount;
        if (totalTokens > 2)
        {
            tokensRow.GetChild(0).localPosition = new Vector2((float)-7.5, 7);  // Upper left
            tokensRow.GetChild(1).localPosition = new Vector2((float)7.5, 7);  // Upper right

            if (totalTokens == 3)
            {
                tokensRow.GetChild(2).localPosition = new Vector2(0, -7);  // Bottom center
            }
            else  // Supports up to 4 tokens
            {
                tokensRow.GetChild(2).localPosition = new Vector2((float)-7.5, -7);  // Bottom left
                tokensRow.GetChild(3).localPosition = new Vector2((float)7.5, -7);  // Bottom right
            }
        }
        else if (totalTokens == 2)
        {
            tokensRow.GetChild(0).localPosition = new Vector2((float)-7.5, 0);  // Left
            tokensRow.GetChild(1).localPosition = new Vector2((float)7.5, 0);  // Right
        }
    }

    public void ReorganizeUnits()
    {
        int counter = 0;
        foreach (Transform child in transform.Find("UnitsContainer"))
        {
            child.localPosition = unitPositions[counter];
            counter++;
        }
    }

    public Vector2 GetNextUnitCoordinates()
    {
        int currentUnitCount = transform.Find("UnitsContainer").childCount;
        return unitPositions[currentUnitCount];
    }

    public void AddHeroToZone(string heroTag)
    {
        Transform heroesRow = transform.Find("HeroesRow");
        string matchingHeroButtonNumber = null;
        switch (heroTag)
        {
            case "1stHero":
                matchingHeroButtonNumber = "1";
                break;
            case "2ndHero":
                matchingHeroButtonNumber = "2";
                break;
            case "3rdHero":
                matchingHeroButtonNumber = "3";
                break;
        }
        foreach (Transform hero in heroesRow)
        {
            if (matchingHeroButtonNumber == hero.Find("NumButtonText").GetComponent<TMP_Text>().text)
            {
                hero.GetComponent<CanvasGroup>().alpha = 1;
                hero.tag = heroTag;
                break;
            }
        }
    }

    public void EmptyOutZone()
    {
        HashSet<string> unitTagsSet = new HashSet<string>(transform.GetComponentInParent<ScenarioMap>().unitTagsMasterList);
        foreach (Transform child in transform)
        {
            switch (child.name)
            {
                case "TokensRow":
                    for (int i = child.childCount-1; i >= 0; i--)
                    {
                        DestroyImmediate(child.GetChild(i).gameObject);
                    }
                    break;

                case "HeroesRow":
                    foreach (Transform hero in child)
                    {
                        hero.tag = "Untagged";
                        hero.GetComponent<CanvasGroup>().alpha = (float).2;
                    }
                    break;

                case "UnitsContainer":
                    for (int i = child.childCount-1; i >= 0; i--)
                    {
                        DestroyImmediate(child.GetChild(i).gameObject);
                    }
                    break;
            }
        }
    }

    public void LoadZoneSave(ZoneSave zoneSave)
    {
        EmptyOutZone();
        Transform tokensRow = transform.Find("TokensRow");
        foreach (string tokenOrHeroTag in zoneSave.tokensAndHeroesTags)
        {
            switch (tokenOrHeroTag)
            {
                case "Computer":
                    Instantiate(computerPrefab, tokensRow);
                    break;
                case "Bomb":
                    Instantiate(bombPrefab, tokensRow);
                    break;
                case "PrimedBomb":
                    Instantiate(primedBombPrefab, tokensRow);
                    break;
                case "1stHero":
                case "2ndHero":
                case "3rdHero":
                    AddHeroToZone(tokenOrHeroTag);
                    break;
            }
        }
        foreach (string fadedTokenTag in zoneSave.fadedTokensTags)
        {
            switch (fadedTokenTag)
            {
                case "Computer":
                    Instantiate(computerPrefab, tokensRow).GetComponent<CanvasGroup>().alpha = (float).2;
                    break;
                case "Bomb":
                    Instantiate(bombPrefab, tokensRow).GetComponent<CanvasGroup>().alpha = (float).2;
                    break;
                case "PrimedBomb":
                    Instantiate(primedBombPrefab, tokensRow).GetComponent<CanvasGroup>().alpha = (float).2;
                    break;
            }
        }
        ReorganizeTokens();

        Transform unitsContainer = transform.Find("UnitsContainer");
        foreach (UnitSave unit in zoneSave.units)
        {
            GameObject unitPrefab = transform.GetComponentInParent<ScenarioMap>().unitPrefabsMasterDict[unit.tag];
            if (unitPrefab != null)
            {
                GameObject spawnedUnit = Instantiate(unitPrefab, unitsContainer);
                Unit spawnedUnitInfo = spawnedUnit.GetComponent<Unit>();
                spawnedUnitInfo.ModifyLifePoints((unit.lifePoints - spawnedUnitInfo.lifePoints));
                if (spawnedUnitInfo.lifePoints < 1)
                {
                    spawnedUnit.GetComponent<CanvasGroup>().alpha = (float).2;
                }
            }
            else
            {
                Debug.LogError("ERROR! In ZoneInfo.LoadZoneSave(), unable to find prefab asset for " + unit.tag + " for " + transform.name);
            }
        }
        ReorganizeUnits();
    }
}


[Serializable]
public class ZoneSave
{
    public List<string> tokensAndHeroesTags = new List<string>();  // Ex: ["Computer", "2ndHero", "3rdHero"]
    public List<string> fadedTokensTags = new List<string>();
    public List<UnitSave> units = new List<UnitSave>();

    public ZoneSave(ZoneInfo zone)
    {
        Transform tokensRow = zone.transform.Find("TokensRow");
        foreach (Transform row in tokensRow)
        {
            if (row.GetComponent<CanvasGroup>().alpha == 1)
            {
                tokensAndHeroesTags.Add(row.tag);  // This should always be a token
            }
            else
            {
                fadedTokensTags.Add(row.tag);  // This should always be a token
            }
        }

        Transform heroesRow = zone.transform.Find("HeroesRow");
        foreach (Transform row in heroesRow)
        {
            if (row.GetComponent<CanvasGroup>().alpha == 1)
            {
                tokensAndHeroesTags.Add(row.tag);  // This should always be a hero
            }
        }

        Transform unitsContainer = zone.transform.Find("UnitsContainer");
        foreach (Transform row in unitsContainer)
        {
            units.Add(row.GetComponent<Unit>().ToJSON());
        }
    }
}
