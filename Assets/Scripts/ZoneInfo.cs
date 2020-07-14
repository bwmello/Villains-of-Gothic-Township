using System;  // For NullReferenceException
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;  // For getting henchmen quantity from UnitRows

public class ZoneInfo : MonoBehaviour
{
    readonly System.Random random = new System.Random();

    public List<GameObject> adjacentZones;
    public List<GameObject> steeplyAdjacentZones;
    public List<GameObject> wall1AdjacentZones;
    public List<GameObject> wall2AdjacentZones;
    public List<GameObject> wall3AdjacentZones;
    public List<GameObject> wall4AdjacentZones;
    public List<GameObject> wall5AdjacentZones;
    public List<GameObject> lineOfSightZones;

    public GameObject computerPrefab;
    public GameObject bombPrefab;
    public GameObject primedBombPrefab;
    public GameObject woundPrefab;
    public GameObject successVsFailurePrefab;
    public GameObject successPrefab;
    public GameObject failurePrefab;
    public GameObject elevationDie;  // Determines fall damage and bonus for ranged attacks made from a higher elevation
    public GameObject confirmButtonPrefab;

    public Boolean isSpawnZone = false;
    public int elevation;
    public int maxOccupancy;
    public int terrainDifficulty = 0;
    public int terrainDanger = 0;
    public int supportRerolls = 0;  // Determined by total Unit.supportRerolls of each unit in zone

    private void Start()
    {
        List<string> unitTags = transform.GetComponentInParent<ScenarioMap>().villainRiver;  // Fetch this list every time you need it as ScenarioMap removes unit tiles by dredging river
        foreach (Transform row in transform.Find("UnitsContainer"))
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

    public Unit[] GetUnitsInfo()
    {
        return transform.GetComponentsInChildren<Unit>();
    }

    public List<GameObject> GetUnits()
    {
        List<GameObject> units = new List<GameObject>();
        foreach (Unit unit in GetUnitsInfo())
        {
            units.Add(unit.gameObject);
        }
        return units;
    }

    public int GetCurrentOccupancy()
    {
        int currentOccupancy = 0;
        currentOccupancy += GetHeroesCount();  // TODO stop assuming size and menace of 1 for each hero
        foreach (Unit unit in GetUnitsInfo())
        {
            currentOccupancy += unit.size;
        }
        return currentOccupancy;
    }

    public int GetCurrentHindrance(GameObject unitToDiscount, bool isMoving = false)
    {
        int currentHindrance = 0;
        currentHindrance += GetHeroesCount();  // TODO stop assuming size and menace of 1 for each hero

        foreach (Unit unit in GetUnitsInfo())
        {
            if (unit.gameObject != unitToDiscount)  // Unit doesn't count itself for hindrance.
            {
                currentHindrance -= isMoving ? unit.size : unit.menace;
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
        foreach (Transform hero in heroesRow)
        {
            if (hero.GetComponent<CanvasGroup>().alpha == 1)  // If button isn't transparent
            {
                heroesCount += 1;
            }
        }
        return heroesCount;
    }

    public GameObject GetRandomHero()
    {
        Transform heroesRow = transform.Find("HeroesRow");
        List<GameObject> heroes = new List<GameObject>();
        foreach (Transform hero in heroesRow)
        {
            if (hero.GetComponent<CanvasGroup>().alpha == 1)  // If button isn't transparent
            {
                heroes.Add(hero.gameObject);
            }
        }
        return heroes[random.Next(heroes.Count)];
    }

    public void PrimeBomb()
    {
        Transform tokensRow = transform.Find("TokensRow");
        try
        {
            Transform bomb = tokensRow.Find("Bomb");
            bomb.GetComponent<CanvasGroup>().alpha = (float).2;  // Still exists to be primed by someone else
            bomb.tag = "Untagged";
            //Destroy(tokensRow.Find("Bomb"));  // When Destroy() or DestroyImmediate(), throws error: Can't remove RectTransform because Image (Script), Image (Script), Image (Script) depends on it
        }
        catch (NullReferenceException err)
        {
            Debug.LogError("ERROR! Tried to prime a bomb in " + transform.name + " without a bomb there. Error details: " + err.ToString());
        }
        Instantiate(primedBombPrefab, tokensRow);
    }

    public GameObject GetBomb()
    {
        Transform tokensRow = transform.Find("TokensRow");
        GameObject bomb = null;
        try
        {
            bomb = tokensRow.Find("Bomb").gameObject;
        }
        catch (NullReferenceException err)
        {
            Debug.LogError("ERROR! Tried to get a bomb in " + transform.name + " without a bomb there. Error details: " + err.ToString());
        }
        return bomb;
    }

    public void RemoveComputer()
    {
        Transform tokensRow = transform.Find("TokensRow");
        try
        {
            Transform computer = tokensRow.Find("Computer");
            computer.GetComponent<CanvasGroup>().alpha = (float).2;
            computer.tag = "Untagged";
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

    public GameObject GetAvailableUnitSlot()
    {
        GameObject availableUnitSlot = null;
        foreach (Transform unitSlot in transform.Find("UnitsContainer"))
        {
            if (unitSlot.childCount == 0)  // If unitSlot is empty
            {
                availableUnitSlot = unitSlot.gameObject;
                break;
            }
        }
        return availableUnitSlot;
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

    public void RemoveHeroesToMatchTotal(int totalHeroes)  // Used by LoadZoneSave
    {
        if (totalHeroes < 3)
        {
            Transform heroesRow = transform.Find("HeroesRow");
            if (heroesRow.childCount > 2)
            {
                Destroy(heroesRow.GetChild(2).gameObject);
            }
            if (totalHeroes < 2 && heroesRow.childCount > 1)
            {
                Destroy(heroesRow.GetChild(1).gameObject);
            }
        }
    }

    public void EmptyOutZone()
    {
        Transform tokensContainer = transform.Find("TokensRow");
        for (int i = tokensContainer.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(tokensContainer.GetChild(i).gameObject);
        }  // Must use DestroyImmediate as Destroy doesn't kick in until after the rest of LoadZoneSave(), so you're instantiating new tokens/units before having deleted the old ones

        Transform heroesContainer = transform.Find("HeroesRow");
        foreach (Transform hero in heroesContainer)
        {
            hero.tag = "Untagged";
            hero.GetComponent<CanvasGroup>().alpha = (float).2;
        }

        List<GameObject> units = GetUnits();
        for (int i = units.Count - 1; i >= 0; i--)
        {
            DestroyImmediate(units[i]);
        }
    }

    public void LoadZoneSave(ZoneSave zoneSave, int totalHeroes)
    {
        isSpawnZone = zoneSave.isSpawnZone;
        RemoveHeroesToMatchTotal(totalHeroes);

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

        foreach (UnitSave unit in zoneSave.units)
        {
            GameObject unitPrefab = transform.GetComponentInParent<ScenarioMap>().unitPrefabsMasterDict[unit.tag];
            if (unitPrefab != null)
            {
                GameObject spawnedUnit = Instantiate(unitPrefab, GetAvailableUnitSlot().transform);
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
    }
}


[Serializable]
public class ZoneSave
{
    public Boolean isSpawnZone = false;
    public List<string> tokensAndHeroesTags = new List<string>();  // Ex: ["Computer", "2ndHero", "3rdHero"]
    public List<string> fadedTokensTags = new List<string>();
    public List<UnitSave> units = new List<UnitSave>();

    public ZoneSave(ZoneInfo zone)
    {
        isSpawnZone = zone.isSpawnZone;

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

        foreach (Unit unit in zone.GetUnitsInfo())
        {
            units.Add(unit.ToJSON());
        }
    }
}
