using System;  // For NullReferenceException
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;  // For AssetDatabase.LoadAssetAtPath() for getting unit prefabs
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
                Destroy(token.gameObject);
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

    public void TokenButtonClicked(Button button)
    {
        CanvasGroup buttonCanvas = button.GetComponent<CanvasGroup>();
        if (buttonCanvas.alpha == 1)
        {
            buttonCanvas.alpha = (float).2;
        }
        else
        {
            buttonCanvas.alpha = (float)1;
        }
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
        List<string> unitTagsList = transform.GetComponentInParent<ScenarioMap>().villainRiver;
        foreach (Transform child in transform)
        {
            switch (child.name)
            {
                case "TokensRow":
                    foreach (Transform token in child)
                    {
                        Destroy(token.gameObject);
                    }
                    break;
                case "HeroesRow":
                    foreach (Transform hero in child)
                    {
                        hero.tag = "Untagged";
                        hero.GetComponent<CanvasGroup>().alpha = (float).2;
                    }
                    break;
                default:
                    if (child.tag != null && unitTagsList.Contains(child.tag))  // If unit, destroy it
                    {
                        Destroy(child.gameObject);
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

        foreach (UnitSave unit in zoneSave.units)
        {
            string assetPath = "Assets/Prefabs/Units/" + unit.tag + ".prefab";
            GameObject unitPrefab = (GameObject) AssetDatabase.LoadAssetAtPath(assetPath, typeof(GameObject));
            if (unitPrefab != null)
            {
                GameObject spawnedUnit = Instantiate(unitPrefab, transform);
                spawnedUnit.GetComponent<Unit>().lifePoints = unit.lifePoints;
            }
            else
            {
                Debug.LogError("ERROR! In ZoneInfo.LoadZoneSave(), unable to find prefab asset for " + unit.tag + " for " + transform.name + " at " + assetPath);
            }
        }
    }
}


[Serializable]
public class ZoneSave
{
    public List<string> tokensAndHeroesTags = new List<string>();  // Ex: ["Computer", "2ndHero", "3rdHero"]
    public List<UnitSave> units = new List<UnitSave>();
    // TODO track where holes have been punched in walls

    public ZoneSave(ZoneInfo zone)
    {
        List<string> unitTags = zone.transform.GetComponentInParent<ScenarioMap>().villainRiver;
        foreach (Transform row in zone.transform)
        {
            if (unitTags.Contains(row.tag))
            {
                units.Add(row.GetComponent<Unit>().ToJSON());
            }
        }

        Transform tokensRow = zone.transform.Find("TokensRow");
        foreach (Transform row in tokensRow)
        {
            tokensAndHeroesTags.Add(row.tag);  // This should always be a token
        }

        Transform heroesRow = zone.transform.Find("HeroesRow");
        foreach (Transform row in heroesRow)
        {
            if (row.GetComponent<CanvasGroup>().alpha == 1)
            {
                tokensAndHeroesTags.Add(row.tag);  // This should always be a hero
            }
        }
    }
}
