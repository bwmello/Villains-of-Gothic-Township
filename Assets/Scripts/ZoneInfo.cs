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
    private Animate animate;

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
    public GameObject briefcasePrefab;
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
    public int terrainDanger = 0;  // Shouldn't change from initial, use GetTerrainDangerTotal to get current


    [Serializable]
    public class LineOfSight
    {
        public List<GameObject> sightLine;

        public LineOfSight(List<GameObject> newSightLine)  // Used by WallRubble.cs when breaking a wall
        {
            sightLine = newSightLine;
        }
    }

    private void Awake()
    {
        animate = GameObject.FindGameObjectWithTag("AnimationContainer").GetComponent<Animate>();
    }

    public int GetZoneID()
    {
        try
        {
            return Int32.Parse(transform.name.Remove(0, 14));
            // int.Parse(Regex.Match(heroZone.name, @"\d+").Value);
        }
        catch (FormatException err)
        {
            Debug.LogError("Failed to Parse " + transform.name + " to an int using: " + transform.name.Remove(0, 14) + "  Error details: " + err.ToString());
        }
        return -1;  // An invalid zone id
    }

    public List<Unit> GetUnitsInfo(bool onlyActive = true)
    {
        List<Unit> unitsInfo = new List<Unit>(transform.GetComponentsInChildren<Unit>());
        if (onlyActive)
        {
            List<Unit> activeUnitsInfo = new List<Unit>();
            foreach (Unit maybeActiveUnit in unitsInfo)
            {
                if (maybeActiveUnit.IsActive())
                {
                    activeUnitsInfo.Add(maybeActiveUnit);
                }
            }
            return activeUnitsInfo;
        }
        else
        {
            return unitsInfo;
        }
    }

    public List<GameObject> GetUnits()  // Returns only .IsActive() units
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
        currentOccupancy += GetHeroesCount();  // Assumes size and menace of 1 for each hero
        foreach (Unit unit in gameObject.GetComponentsInChildren<Unit>())
        {
            if (unit.IsActive())
            {
                currentOccupancy += unit.size;
            }
        }
        return currentOccupancy;
    }

    public int GetCurrentHindrance(GameObject unitToDiscount, bool isMoving = false)
    {
        int currentHindrance = 0;
        currentHindrance += GetHeroesCount();  // Assumes size and menace of 1 for each hero

        foreach (Unit unit in gameObject.GetComponentsInChildren<Unit>())
        {
            if (unit.IsActive() && unit.gameObject != unitToDiscount)  // Unit doesn't count itself for hindrance
            {
                if (unit.isHeroAlly)
                {
                    currentHindrance += isMoving ? unit.size : unit.menace;
                }
                else
                {
                    currentHindrance -= isMoving ? unit.size : unit.menace;
                }
            }
        }

        if (currentHindrance < 0)
        {
            currentHindrance = 0;
        }
        return currentHindrance;
    }

    public int GetCurrentHindranceForHero(string heroTag, bool isMoving = false)
    {
        int currentHindrance = 0;

        foreach (Unit unit in gameObject.GetComponentsInChildren<Unit>())
        {
            if (unit.IsActive())
            {
                if (unit.isHeroAlly)
                {
                    currentHindrance -= isMoving ? unit.size : unit.menace;
                }
                else
                {
                    currentHindrance += isMoving ? unit.size : unit.menace;
                }
            }
        }

        foreach (GameObject heroOccupant in GetHeroes())
        {
            if (!heroOccupant.CompareTag(heroTag))  // Hero doesn't count itself for hindrance.
            {
                currentHindrance -= 1;
            }
        }

        if (currentHindrance < 0)
        {
            currentHindrance = 0;
        }
        return currentHindrance;
    }

    public LineOfSight GetLineOfSightWithZone(GameObject targetZone)
    {
        foreach (LineOfSight los in linesOfSight)
        {
            if (los.sightLine.Contains(targetZone))
            {
                return los;
            }
        }

        return null;
    }

    public List<GameObject> GetSightLineWithZone(GameObject targetZone)
    {
        LineOfSight losWithZone = GetLineOfSightWithZone(targetZone);
        if (losWithZone != null)
        {
            return losWithZone.sightLine;
        }

        return null;
    }

    public int GetSmokeBetweenZones(GameObject farZone)
    {
        int smokeTokens = GetQuantityOfEnvironTokensWithTag("Smoke");
        if (gameObject == farZone)
        {
            return smokeTokens;
        }

        List<GameObject> lineOfSight = new List<GameObject>();
        //Debug.Log("this zone: " + gameObject.name + "     GetLineOfSightWithZone(" + farZone.name + "): ");// + GetLineOfSightWithZone(farZone).ToString());
        lineOfSight.AddRange(GetSightLineWithZone(farZone));
        foreach (GameObject losZone in lineOfSight)
        {
            smokeTokens += losZone.GetComponent<ZoneInfo>().GetQuantityOfEnvironTokensWithTag("Smoke");
        }
        return smokeTokens;
    }

    public int GetQuantityOfEnvironTokensWithTag(string tokensTag)
    {
        int quantity = 0;
        List<GameObject> environTokens = GetAllTokensWithTag(tokensTag);
        foreach (GameObject environToken in environTokens)
        {
            quantity += environToken.GetComponent<EnvironToken>().quantity;
        }
        return quantity;
    }

    public int GetSupportRerolls(GameObject unitToDiscount = null)
    {
        int supportRerolls = 0;
        foreach (Unit unit in gameObject.GetComponentsInChildren<Unit>())
        {
            if (unit.IsActive() && !unit.isHeroAlly && unitToDiscount != unit.gameObject)
            {
                supportRerolls += unit.supportRerolls;
            }
        }
        return supportRerolls;
    }

    public double GetOccupantsManipulationLikelihood(int requiredSuccesses, bool munitionSpecialistApplies = false)
    {
        double chanceOfFailure = 1;

        foreach (Unit unit in gameObject.GetComponentsInChildren<Unit>())
        {
            if (unit.IsActive() && !unit.isHeroAlly)
            {
                foreach (Unit.ActionProficiency proficiency in unit.actionProficiencies)
                {
                    if (proficiency.actionType == "MANIPULATION")
                    {
                        int totalRequiredSuccesses = requiredSuccesses + GetCurrentHindrance(unit.gameObject);
                        if (munitionSpecialistApplies)
                        {
                            totalRequiredSuccesses -= unit.munitionSpecialist;
                        }
                        chanceOfFailure *= 1 - unit.GetChanceOfSuccess(totalRequiredSuccesses, proficiency.proficiencyDice, GetSupportRerolls(unit.gameObject));
                    }
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

    public GameObject GetObjectiveToken(string tokenName)
    {
        Transform tokensRow = transform.Find("TokensRow");
        Transform token = tokensRow.Find(tokenName);
        if (token && token.GetComponent<Token>().IsActive())
        {
            return token.gameObject;
        }
        return null;
    }

    public void DestroyFadedTokensAndUnits()
    {
        Transform tokensRow = transform.Find("TokensRow");
        foreach (Transform token in tokensRow)
        {
            if (token.gameObject.GetComponent<CanvasGroup>().alpha < (float).5)  // Dissipating EnvironTokens go to .5 while still active
            {
                DestroyImmediate(token.gameObject);
            }
        }
        List<Unit> unitsInfo = new List<Unit>(gameObject.GetComponentsInChildren<Unit>());
        for (int i = unitsInfo.Count - 1; i >= 0; i--)
        {
            if (!unitsInfo[i].IsActive())
            {
                Destroy(unitsInfo[i].gameObject);
            }
        }
    }

    public bool HasHeroes()
    {
        return GetHeroesCount() > 0;
    }

    public bool HasTargetableHeroes()
    {
        return GetTargetableHeroesCount() > 0;
    }

    public bool HasTargets()
    {
        if (GetTargetableHeroesCount() > 0)
        {
            return true;
        }
        else
        {
            foreach (Unit unit in gameObject.GetComponentsInChildren<Unit>())
            {
                if (unit.IsActive() && unit.isHeroAlly)
                {
                    return true;
                }
            }
        }
        return false;
    }

    public GameObject GetLineOfSightZoneWithHero()  // Still used by grenade action in Unit.cs
    {
        List<ZoneInfo> targetableZones = new List<ZoneInfo>();
        if (HasTargetableHeroes())
        {
            targetableZones.Add(this);
        }
        foreach (GameObject zone in lineOfSightZones)
        {
            ZoneInfo losZoneInfo = zone.GetComponent<ZoneInfo>();
            if (losZoneInfo.HasTargetableHeroes())
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

    public List<GameObject> GetZonesWithTargetsWithinLinesOfSight(int maxRange = -1)  // -1 means no maxRange
    {
        List<GameObject> targetableZones = new List<GameObject>();
        if (HasTargets())
        {
            targetableZones.Add(gameObject);
        }

        if (maxRange > 0)  // Range is limited (maxRange == 0 skips to return)
        {
            HashSet<GameObject> uniqueTargetableZones = new HashSet<GameObject>();
            foreach (LineOfSight lineOfSight in linesOfSight)
            {
                for (int i = 0; i < maxRange && i < lineOfSight.sightLine.Count; i++)
                {
                    if (!uniqueTargetableZones.Contains(lineOfSight.sightLine[i]) && lineOfSight.sightLine[i].GetComponent<ZoneInfo>().HasTargets())
                    {
                        uniqueTargetableZones.Add(lineOfSight.sightLine[i]);  // Adds only unique zones, even without !uniqueTargetableZones.Contains() check
                    }
                }
            }
            targetableZones.AddRange(uniqueTargetableZones);
        }
        else if (maxRange < 0)  // Range is limitless
        {
            foreach (GameObject zone in lineOfSightZones)
            {
                if (zone.GetComponent<ZoneInfo>().HasTargets())
                {
                    targetableZones.Add(zone);
                }
            }
        }
        return targetableZones;
    }

    public int GetHeroesCount()
    {
        int heroesCount = 0;
        Hero[] heroes = GetComponentsInChildren<Hero>();
        foreach (Hero hero in heroes)
        {
            heroesCount += 1;
        }
        return heroesCount;
    }

    public int GetTargetableHeroesCount()
    {
        int heroesCount = 0;
        Hero[] heroes = GetComponentsInChildren<Hero>();
        foreach (Hero hero in heroes)
        {
            if (!hero.IsWoundedOut())
            {
                heroesCount += 1;
            }
        }
        return heroesCount;
    }

    public List<GameObject> GetHeroes()
    {
        List<GameObject> occupyingHeroes = new List<GameObject>();
        Hero[] heroes = GetComponentsInChildren<Hero>();
        foreach (Hero hero in heroes)
        {
            occupyingHeroes.Add(hero.gameObject);
        }
        return occupyingHeroes;
    }

    public List<GameObject> GetTargetableHeroes()
    {
        List<GameObject> occupyingHeroes = new List<GameObject>();
        Hero[] heroes = GetComponentsInChildren<Hero>();
        foreach (Hero hero in heroes)
        {
            if (!hero.IsWoundedOut())
            {
                occupyingHeroes.Add(hero.gameObject);
            }
        }
        return occupyingHeroes;
    }

    public List<GameObject> GetTargetableHeroAllies()
    {
        List<GameObject> occupyingHeroAllies = new List<GameObject>();
        foreach (Unit unit in gameObject.GetComponentsInChildren<Unit>())
        {
            if (unit.IsActive() && unit.isHeroAlly)
            {
                occupyingHeroAllies.Add(unit.gameObject);
            }
        }
        return occupyingHeroAllies;
    }

    public GameObject GetRandomHero()
    {
        List<GameObject> heroes = GetHeroes();
        return heroes.Count > 0 ? heroes[random.Next(heroes.Count)] : null;
    }

    public GameObject GetRandomTargetableHero()
    {
        List<GameObject> heroes = GetTargetableHeroes();
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

    public void SetIsClickableForHeroesAndAllies(bool shouldMakeClickable)  // This also makes inactive heroes/units clickable, but does that really matter?
    {
        foreach (Unit unit in this.GetComponentsInChildren<Unit>())
        {
            if (unit.isHeroAlly)  // && unit.IsActive()
            {
                unit.SetIsClickable(shouldMakeClickable);
            }
        }
        foreach (Hero hero in this.GetComponentsInChildren<Hero>())
        {  // if (!hero.IsWoundedOut) {
            hero.SetIsClickable(shouldMakeClickable);
        }
    }

    public int GetTerrainDangerTotal(Unit unit = null)
    {
        int terrainDangerTotal = terrainDanger;
        if (unit == null || !unit.fiery)
        {
            terrainDangerTotal += GetQuantityOfEnvironTokensWithTag("Flame");
        }
        if (unit == null || !unit.frosty)
        {
            terrainDangerTotal += GetQuantityOfEnvironTokensWithTag("Cryogenic");
        }
        if (unit == null || !unit.gasImmunity)
        {
            terrainDangerTotal += GetQuantityOfEnvironTokensWithTag("Gas");
        }
        return terrainDangerTotal;
    }

    private readonly Vector3[] terrainDangerIconPlacement = new[] { new Vector3(-20f, 15f, 0), new Vector3(20f, 15f, 0), new Vector3(-20f, 0f, 0), new Vector3(20f, 0f, 0) };
    public IEnumerator IncreaseTerrainDangerTemporarily(int dangerIncrease, List<Unit> affectedUnits = null)
    {
        if (affectedUnits == null)
        {
            affectedUnits = new List<Unit>(GetUnitsInfo());  // This must be a list of .IsActive() units
        }
        List<GameObject> unitCasualties = new List<GameObject>();
        foreach (Unit affectedUnit in affectedUnits)
        {
            int automaticWounds = 0;
            Dice damageDie = environmentalDie.GetComponent<Dice>();
            for (int i = 0; i < dangerIncrease; i++)
            {
                automaticWounds += damageDie.Roll();
            }
            affectedUnit.ModifyLifePoints(-automaticWounds);
            if (!affectedUnit.IsActive())
            {
                unitCasualties.Add(affectedUnit.gameObject);
            }
        }
        if (unitCasualties.Count > 0)
        {
            float unitFadeAlpha = unitCasualties[0].GetComponent<Unit>().fadedAlpha;
            yield return StartCoroutine(animate.FadeObjects(unitCasualties, 1, unitFadeAlpha));
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

    Boolean waitingOnPlayerInput = false;
    IEnumerator PauseUntilPlayerPushesContinue(GameObject animationContainer)  // TODO this function replicates one in Unit.cs/Animate.cs, move both to a shared class and distinguish between pausing for one hero and multiple
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

    public void EnableDropZone()
    {
        transform.Find("DropZone").gameObject.SetActive(true);
    }

    public void DisableDropZone()
    {
        transform.Find("DropZone").gameObject.SetActive(false);
    }

    public void AddObjectiveToken(string tokenTag)
    {
        GameObject objectiveTokenPrefab = null;
        switch (tokenTag)
        {
            case "Bomb":
                objectiveTokenPrefab = bombPrefab;
                break;
            case "Briefcase":
                objectiveTokenPrefab = briefcasePrefab;
                break;
            case "Computer":
                objectiveTokenPrefab = computerPrefab;
                break;
            case "PrimedBomb":
                objectiveTokenPrefab = primedBombPrefab;
                break;
        }
        if (objectiveTokenPrefab)
        {
            Transform tokensRow = transform.Find("TokensRow");
            GameObject objectiveToken = Instantiate(objectiveTokenPrefab, tokensRow);
            objectiveToken.GetComponent<Token>().ConfigureClickability();
            ReorganizeTokens();
        }
    }

    public void RemoveObjectiveToken(string tokenTag)
    {
        foreach (Token token in gameObject.GetComponentsInChildren<Token>())
        {
            if (token.CompareTag(tokenTag))
            {
                DestroyImmediate(token.gameObject);  // Destroy() doesn't kick in before ReorganizeTokens()
                ReorganizeTokens();
                break;
            }
        }
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
                foreach (Unit unitInZone in gameObject.GetComponentsInChildren<Unit>())
                {
                    if (unitInZone.IsActive() && !unitInZone.gasImmunity)
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
                foreach (Unit unitInZone in gameObject.GetComponentsInChildren<Unit>())
                {
                    if (unitInZone.IsActive() && !unitInZone.fiery)
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
                foreach (Unit unitInZone in gameObject.GetComponentsInChildren<Unit>())
                {
                    if (unitInZone.IsActive() && !unitInZone.frosty)
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
        GameObject inactiveUnitToDestroy = null;
        foreach (Transform unitSlot in transform.Find("UnitsContainer"))
        {
            if (unitSlot.childCount == 0)  // If unitSlot is empty
            {
                return unitSlot.gameObject;  // Bypass if(inactiveUnitToDestroy) check
            }
            else if (!unitSlot.GetComponentInChildren<Unit>().IsActive())
            {
                availableUnitSlot = unitSlot.gameObject;
                inactiveUnitToDestroy = unitSlot.GetChild(0).gameObject;
            }
        }
        if (inactiveUnitToDestroy)
        {
            Destroy(inactiveUnitToDestroy);
        }
        return availableUnitSlot;
    }

    public GameObject AddUnitToZone(string unitTag, int unitSize)  // Used by UIOverlay.cs when dragging allies onto board during Setup
    {
        GameObject unitSlot = GetAvailableUnitSlot();
        if (unitSlot && GetCurrentOccupancy() + unitSize <= maxOccupancy)
        {
            return Instantiate(transform.GetComponentInParent<ScenarioMap>().unitPrefabsMasterDict[unitTag], unitSlot.transform);
        }
        return null;
    }

    public void AddHeroToZone(GameObject hero)
    {
        Transform heroesRow = transform.Find("HeroesRow");
        switch (hero.tag)
        {
            case "1stHero":
                hero.transform.SetParent(heroesRow.Find("HeroSlot 0"));
                break;
            case "2ndHero":
                hero.transform.SetParent(heroesRow.Find("HeroSlot 1"));
                break;
            case "3rdHero":
                hero.transform.SetParent(heroesRow.Find("HeroSlot 2"));
                break;
        }
        hero.transform.localPosition = new Vector3(0, 0, 0);  // Otherwise is centered on scenario map
    }

    public void EmptyOutZone()
    {
        Transform tokensContainer = transform.Find("TokensRow");
        for (int i = tokensContainer.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(tokensContainer.GetChild(i).gameObject);
        }  // Must use DestroyImmediate as Destroy doesn't kick in until after the rest of LoadZoneSave(), so you're instantiating new tokens/units before having deleted the old ones

        List<GameObject> heroesOccupying = GetHeroes();
        foreach (GameObject hero in heroesOccupying)
        {
            DestroyImmediate(hero);
        }

        List<GameObject> units = GetUnits();
        for (int i = units.Count - 1; i >= 0; i--)
        {
            DestroyImmediate(units[i]);
        }
    }

    public void LoadZoneSave(ZoneSave zoneSave)
    {
        isSpawnZone = zoneSave.isSpawnZone;

        EmptyOutZone();
        Transform tokensRow = transform.Find("TokensRow");
        //string debugString = "LoadZoneSave for " + gameObject.name + "\ntokensAndHeroesTags: { ";
        foreach (string tokenOrHeroTag in zoneSave.tokensAndHeroesTags)
        {
            //debugString += tokenOrHeroTag + ", ";
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
            }
        }
        //debugString += "}\nfadedTokensTags: { ";
        foreach (string fadedTokenTag in zoneSave.fadedTokensTags)
        {
            //debugString += fadedTokenTag + ", ";
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

        //debugString += "}\nunits: { ";
        foreach (UnitSave unitSave in zoneSave.units)
        {
            //debugString += unit.tag + ", ";
            GameObject unitPrefab = transform.GetComponentInParent<ScenarioMap>().unitPrefabsMasterDict[unitSave.tag];
            if (unitPrefab != null)
            {
                GameObject spawnedUnit = Instantiate(unitPrefab, GetAvailableUnitSlot().transform);
                spawnedUnit.GetComponent<Unit>().InitializeUnit(unitSave);
            }
            else
            {
                Debug.LogError("ERROR! In ZoneInfo.LoadZoneSave(), unable to find prefab asset for " + unitSave.tag + " for " + transform.name);
            }
        }
        //Debug.Log(debugString + "}");
    }
}


[Serializable]
public class ZoneSave
{
    public int id;
    public Boolean isSpawnZone = false;
    public List<string> tokensAndHeroesTags = new List<string>();  // Ex: ["Computer", "2ndHero", "3rdHero"]
    public List<string> fadedTokensTags = new List<string>();
    public List<EnvironTokenSave> environTokens = new List<EnvironTokenSave>();
    public List<UnitSave> units = new List<UnitSave>();

    public ZoneSave(ZoneInfo zone)
    {
        id = zone.id;
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

        foreach (Unit unit in zone.gameObject.GetComponentsInChildren<Unit>())
        {
            units.Add(unit.ToJSON());
        }
    }
}
