using System;  // For NullReferenceException
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;  // For button AddEnvironToken continue prompt when hero suffers terrainDanger
using TMPro;  // For adding heroes to the zone and knowing which button to light up

public class ZoneInfo : MonoBehaviour
{
    public int id;
    readonly System.Random random = new System.Random();

    public List<GameObject> adjacentZones;
    public List<GameObject> steeplyAdjacentZones;
    public List<GameObject> wall1AdjacentZones;
    public List<GameObject> wall2AdjacentZones;
    public List<GameObject> wall3AdjacentZones;
    public List<GameObject> wall4AdjacentZones;
    public List<GameObject> wall5AdjacentZones;
    public List<GameObject> lineOfSightZones;
    public List<LineOfSight> linesOfSight;

    public GameObject computerPrefab;
    public GameObject bombPrefab;
    public GameObject primedBombPrefab;
    public GameObject gasPrefab;
    public GameObject flamePrefab;
    public GameObject smokePrefab;
    public GameObject frostPrefab;
    public GameObject cryogenicPrefab;
    public GameObject woundPrefab;
    public GameObject terrainDangerIconPrefab;
    public GameObject successVsFailurePrefab;
    public GameObject successPrefab;
    public GameObject failurePrefab;
    public GameObject environmentalDie;  // Determines terrainDanger/fall damage and bonus for ranged attacks made from a higher elevation
    public GameObject confirmButtonPrefab;

    public Boolean isSpawnZone = false;
    public int elevation;
    public int maxOccupancy;
    public int terrainDifficulty = 0;
    public int terrainDanger = 0;

    public int frostTokens = 0;  // Increases terrainDifficulty when incremented, ignored by unit.frostwalker
    public int cryogenicTokens = 0;  // Increases terrainDifficulty/Danger when incremented, ignored by unit.frostwalker
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

    [Serializable]
    public struct LineOfSight
    {
        public List<GameObject> sightLine;
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
        // TODO Don't include faded/inactive/dead units (that way more likely to grenade zone with already dead unit in it)
        return transform.GetComponentsInChildren<Unit>();
    }

    public List<GameObject> GetUnits()
    {
        // TODO Don't include faded/inactive/dead units
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

    public List<GameObject> GetLineOfSightWithZone(GameObject targetZone)
    {
        foreach (LineOfSight los in linesOfSight)
        {
            if (los.sightLine.Contains(targetZone))
            {
                return los.sightLine;
            }
        }
        return null;
    }

    public double GetOccupantsManipulationLikelihood(GameObject unitToDiscount)
    {
        double chanceOfFailure = 1;

        int requiredSuccesses = 5 + GetCurrentHindrance(unitToDiscount);  // TODO Once GetChanceOfSuccess is fixed to never return >= 1, replace 5 with actual requiredSuccesses (include munitionSpecialist)
        foreach (Unit unit in GetUnitsInfo())
        {
            foreach (Unit.ActionProficiency proficiency in unit.actionProficiencies)
            {
                if (proficiency.actionType == "MANIPULATION")
                {
                    int rerolls = supportRerolls - unit.supportRerolls;
                    chanceOfFailure *= unit.GetChanceOfSuccess(requiredSuccesses, proficiency.proficiencyDice, rerolls);
                }
            }
        }
        return 1 - chanceOfFailure;
    }

    public bool HasObjectiveToken(string tokenName)
    {
        Transform tokensRow = transform.Find("TokensRow");
        Transform token = tokensRow.Find(tokenName);
        if (token)
        {
            return token.GetComponent<Token>().IsActive();
        }
        return false;
    }

    public void DestroyFadedTokens()
    {
        Transform tokensRow = transform.Find("TokensRow");
        foreach (Transform token in tokensRow)
        {
            if (token.gameObject.GetComponent<CanvasGroup>().alpha < (float).5)  // Dissipating EnvironTokens go to .5 while still active
            {
                DestroyImmediate(token.gameObject);
            }
        }
    }

    public bool HasHeroes()
    {
        return GetHeroesCount() > 0;
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

    public List<GameObject> GetHeroes()
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
        return heroes;
    }

    public GameObject GetRandomHero()
    {
        List<GameObject> heroes = GetHeroes();
        return heroes.Count > 0 ? heroes[random.Next(heroes.Count)] : null;
    }

    // Could be expanded to recursively search children instead of just TokensRow
    public List<GameObject> GetAllTokensWithTag(string tagToFind)
    {
        List<GameObject> childrenWithTag = new List<GameObject>();
        Transform tokensRow = transform.Find("TokensRow");
        foreach (Transform token in tokensRow)
        {
            if (token.CompareTag(tagToFind))
            {
                childrenWithTag.Add(token.gameObject);
            }
        }
        return childrenWithTag;
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
            GameObject primedBomb = Instantiate(primedBombPrefab, tokensRow);
            primedBomb.transform.position = bomb.position;
        }
        catch (NullReferenceException err)
        {
            Debug.LogError("ERROR! Tried to prime a bomb in " + transform.name + " without a bomb there. Error details: " + err.ToString());
        }
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

    public int GetTerrainDangerTotal(Unit unit)
    {
        int terrainDangerTotal = terrainDanger;
        if (!unit.fiery)
        {
            List<GameObject> flameTokens = GetAllTokensWithTag("Flame");
            foreach (GameObject flameToken in flameTokens)
            {
                terrainDanger += flameToken.GetComponent<EnvironToken>().quantity;
            }
        }
        if (!unit.frosty)
        {
            List<GameObject> cryogenicTokens = GetAllTokensWithTag("Cryogenic");
            foreach (GameObject cryogenicToken in cryogenicTokens)
            {
                terrainDanger += cryogenicToken.GetComponent<EnvironToken>().quantity;
            }
        }
        if (!unit.gasImmunity)
        {
            List<GameObject> gasTokens = GetAllTokensWithTag("Gas");
            foreach (GameObject gasToken in gasTokens)
            {
                terrainDanger += gasToken.GetComponent<EnvironToken>().quantity;
            }
        }
        return terrainDangerTotal;
    }

    private readonly Vector3[] terrainDangerIconPlacement = new[] { new Vector3(-20f, 15f, 0), new Vector3(20f, 15f, 0), new Vector3(-20f, 0f, 0), new Vector3(20f, 0f, 0) };
    public IEnumerator IncreaseTerrainDangerTemporarily(int dangerIncrease, List<Unit> affectedUnits = null)
    {
        if (affectedUnits == null)
        {
            affectedUnits = new List<Unit>(GetUnitsInfo());
        }
        foreach (Unit affectedUnit in affectedUnits)
        {
            int automaticWounds = 0;
            Dice damageDie = environmentalDie.GetComponent<Dice>();
            for (int i = 0; i < dangerIncrease; i++)
            {
                automaticWounds += damageDie.Roll();
            }
            affectedUnit.ModifyLifePoints(-automaticWounds);
        }
        if (HasHeroes())  // Gives heroes chance to roll damage on themselves (as they can use their rerolls)
        {
            List<GameObject> terrainDangerIcons = new List<GameObject>();
            for (int i = 0; i < dangerIncrease; i++)
            {
                terrainDangerIcons.Add(Instantiate(terrainDangerIconPrefab, transform));
                terrainDangerIcons[i].transform.localPosition = terrainDangerIconPlacement[i];
            }
            GameObject animationContainer = GameObject.FindGameObjectWithTag("AnimationContainer");
            yield return StartCoroutine(PauseUntilPlayerPushesContinue(animationContainer));
            for (int i = terrainDangerIcons.Count - 1; i >= 0; i--)
            {
                Destroy(terrainDangerIcons[i]);
            }
        }
        yield return null;
    }

    public IEnumerator AddEnvironTokens(EnvironTokenSave newEnvironToken)
    {
        Transform tokensRow = transform.Find("TokensRow");
        bool newTokenRequired = true;
        foreach (Transform existingToken in tokensRow)
        {
            if (existingToken.CompareTag(newEnvironToken.tag))
            {
                EnvironToken existingEnvironToken = existingToken.GetComponent<EnvironToken>();
                if (!existingEnvironToken.partiallyDissipated && existingEnvironToken.dissipatesHeroTurn == newEnvironToken.dissipatesHeroTurn && existingEnvironToken.dissipatesVillainTurn == newEnvironToken.dissipatesVillainTurn)
                {
                    newTokenRequired = false;
                    existingEnvironToken.quantity += newEnvironToken.quantity;
                    existingEnvironToken.QuantityModified();
                }
            }
        }
        List<Unit> affectedUnits = new List<Unit>();  // Used for Gas, Flame, and Cryogenic tokens to call IncreaseTerrainDangerTemporarily() with list of who should be affected
        switch (newEnvironToken.tag)
        {
            case "Gas":
                if (newTokenRequired)
                {
                    Instantiate(gasPrefab, tokensRow).GetComponent<EnvironToken>().LoadEnvironTokenSave(newEnvironToken);
                    ReorganizeTokens();
                }
                foreach (Unit unitInZone in GetUnitsInfo())
                {
                    if (!unitInZone.gasImmunity)
                    {
                        affectedUnits.Add(unitInZone);
                    }
                }
                yield return StartCoroutine(IncreaseTerrainDangerTemporarily(1, affectedUnits));
                break;
            case "Flame":
                if (newTokenRequired)
                {
                    Instantiate(flamePrefab, tokensRow).GetComponent<EnvironToken>().LoadEnvironTokenSave(newEnvironToken);
                    ReorganizeTokens();
                }
                foreach (Unit unitInZone in GetUnitsInfo())
                {
                    if (!unitInZone.fiery)
                    {
                        affectedUnits.Add(unitInZone);
                    }
                }
                yield return StartCoroutine(IncreaseTerrainDangerTemporarily(1, affectedUnits));
                break;
            case "Smoke":
                if (newTokenRequired)
                {
                    Instantiate(smokePrefab, tokensRow).GetComponent<EnvironToken>().LoadEnvironTokenSave(newEnvironToken);
                    ReorganizeTokens();
                }
                break;
            case "Frost":
                if (newTokenRequired)
                {
                    Instantiate(frostPrefab, tokensRow).GetComponent<EnvironToken>().LoadEnvironTokenSave(newEnvironToken);
                    ReorganizeTokens();
                }
                break;
            case "Cryogenic":
                if (newTokenRequired)
                {
                    Instantiate(cryogenicPrefab, tokensRow).GetComponent<EnvironToken>().LoadEnvironTokenSave(newEnvironToken);
                    ReorganizeTokens();
                }
                foreach (Unit unitInZone in GetUnitsInfo())
                {
                    if (!unitInZone.frosty)
                    {
                        affectedUnits.Add(unitInZone);
                    }
                }
                yield return StartCoroutine(IncreaseTerrainDangerTemporarily(1, affectedUnits));
                break;
            default:
                Debug.LogError("ERROR! In ZoneInfo.LoadZoneSave(), unable to identify EnvironToken " + newEnvironToken.tag + " for " + transform.name);
                break;
        }
        yield return 0;
    }

    Boolean waitingOnPlayerInput = false;
    IEnumerator PauseUntilPlayerPushesContinue(GameObject animationContainer)  // TODO this function replicates one in Unit.cs, move both to a shared class and distinguish between pausing for one hero and multiple
    {
        waitingOnPlayerInput = true;
        List<GameObject> heroes = GetHeroes();
        foreach (GameObject hero in heroes)
        {
            Button heroButton = hero.GetComponent<Button>();
            heroButton.enabled = true;
        }

        GameObject continueButton = Instantiate(confirmButtonPrefab, transform);
        continueButton.transform.position = transform.TransformPoint(0, -30f, 0);
        continueButton.GetComponent<Button>().onClick.AddListener(delegate { waitingOnPlayerInput = false; });
        yield return new WaitUntil(() => !waitingOnPlayerInput);

        foreach (GameObject hero in heroes)
        {
            Button heroButton = hero.GetComponent<Button>();
            heroButton.enabled = false;
        }
        Destroy(continueButton);
        yield return 0;
    }

    public void RemoveEnvironToken(GameObject tokenToRemove)
    {
        DestroyImmediate(tokenToRemove);
        ReorganizeTokens();
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
        else if (totalTokens == 1)
        {
            tokensRow.GetChild(0).localPosition = new Vector2(0, 0);  // Center
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
        string debugString = "LoadZoneSave for " + gameObject.name + "\ntokensAndHeroesTags: { ";
        foreach (string tokenOrHeroTag in zoneSave.tokensAndHeroesTags)
        {
            debugString += tokenOrHeroTag + ", ";
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
        debugString += "}\nfadedTokensTags: { ";
        foreach (string fadedTokenTag in zoneSave.fadedTokensTags)
        {
            debugString += fadedTokenTag + ", ";
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
        //debugString += "}\nenvironTokens: { ";
        foreach (EnvironTokenSave environTokenSave in zoneSave.environTokens)
        {
            GameObject environTokenCreated = null;
            switch (environTokenSave.tag)
            {
                case "Gas":
                    environTokenCreated = Instantiate(gasPrefab, tokensRow);
                    break;
                case "Flame":
                    environTokenCreated = Instantiate(flamePrefab, tokensRow);
                    break;
                case "Smoke":
                    environTokenCreated = Instantiate(smokePrefab, tokensRow);
                    break;
                case "Frost":
                    environTokenCreated = Instantiate(frostPrefab, tokensRow);
                    break;
                case "Cryogenic":
                    environTokenCreated = Instantiate(cryogenicPrefab, tokensRow);
                    break;
                default:
                    Debug.LogError("ERROR! In ZoneInfo.LoadZoneSave(), unable to identify EnvironToken " + environTokenSave.tag + " for " + transform.name);
                    break;
            }
            EnvironToken environTokenInfo = environTokenCreated.GetComponent<EnvironToken>();
            environTokenInfo.LoadEnvironTokenSave(environTokenSave);
        }
        ReorganizeTokens();

        debugString += "}\nunits: { ";
        foreach (UnitSave unit in zoneSave.units)
        {
            debugString += unit.tag + ", ";
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
        Debug.Log(debugString + "}");
    }
}


[Serializable]
public class ZoneSave
{
    public Boolean isSpawnZone = false;
    public List<string> tokensAndHeroesTags = new List<string>();  // Ex: ["Computer", "2ndHero", "3rdHero"]
    public List<string> fadedTokensTags = new List<string>();
    public List<EnvironTokenSave> environTokens = new List<EnvironTokenSave>();
    public List<UnitSave> units = new List<UnitSave>();

    public ZoneSave(ZoneInfo zone)
    {
        isSpawnZone = zone.isSpawnZone;
        Transform tokensRow = zone.transform.Find("TokensRow");
        List<string> objectiveTokenTags = new List<string> { "Computer", "Bomb", "PrimedBomb" };
        //List<string> environTokenTags = new List<string> { "Gas", "Flame", "Smoke", "Frost", "Cryogenic" };
        foreach (Transform row in tokensRow)
        {
            if (objectiveTokenTags.Contains(row.tag))
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
            else  // else if (environTokenTags.Contains(row.tag))
            {
                environTokens.Add(row.GetComponent<EnvironToken>().ToJSON());
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
