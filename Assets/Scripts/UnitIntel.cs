using System;  // for [Serializable]
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class UnitIntel
{
    // Additional unit resources
    public static int bonusMovePointsRemaining;

    // Non-action behavior weight (actions accounted for in MissionSpecifics.cs)
    public static double terrainDangerWeight = -50;  // * terrainDanger
    public static double terrainDangeringFriendlies = -10;  // * terrainDanger * quantity of friendly units.  Chance of unit dying is 2/3 per terrainDanger
    public static double increaseTerrainDifficultyWeight = 10;
    public static double additionalTargetsForAdditionalAttacksWeight = 3;
    public static double[] bonusMovePointWeight = new double[] { 0, -15, -30, -45 };  // Accessed by UnitIntel.bonusMovePointWeight[movePointsUsed - unit.movePoints], so bonusMovePointWeight[0] = 0
    public static double[] partialMoveWeight = new double[] { .25, .0625 };  // * actionWeight. Accessed by UnitIntel.partialMoveWeight[additional moves required - 1]
    //public static Dictionary<string, double> partialMoveWeight = new Dictionary<string, double>()
    //{
    //    { "MELEE", .1 }
    //};
    //public static double breakingThroughWallWeight = 10;  // To encourage Superbarn to break through walls just because it's cool

    // Helpers for activating units during villain turn
    public static Queue<GameObject> unitsToActivateLast = new Queue<GameObject>();  // Needed for when a unit's turn negatively impacts other units activating the same turn. Ex: Crowbars in IceToSeeYou activating computers blocking not yet activated Crowbars

    public static Dictionary<GameObject, List<int>> heroMovesRequiredToReachZone;
    public static Dictionary<GameObject, List<int>> heroMovePointsRequiredToReachZone;  // TODO not used yet, but would be more useful than wildly guessing heroMovesRequiredToReachZone
    //public static List<HeroIntel> heroesIntel;
    //[Serializable]
    //public class HeroIntel  // Moved to Hero.cs
    //{
    //    public string tag;
    //    public int moveSpeed = 4;
    //    public int ignoreTerrainDifficulty = 1;
    //    public int ignoreElevation = 1;
    //    public int ignoreSize = 1;
    //    public int wallBreaker = 0;  // wall breaking items are single use, making tracking this pointless
    //    public int woundsReceived = 0;
    //    public bool canCounterMeleeAttacks = false;
    //    public bool canCounterRangedAttacks = false;

    //    public HeroIntel(string newTag)
    //    {
    //        tag = newTag;
    //    }
    //}

    public static void ResetPerRoundResources()
    {
        bonusMovePointsRemaining = MissionSpecifics.GetBonusMovePointsPerRound();
        SetHeroMovesRequiredToReachZone();
    }

    public static void SetHeroMovesRequiredToReachZone()
    {
        heroMovesRequiredToReachZone = new Dictionary<GameObject, List<int>>();
        heroMovePointsRequiredToReachZone = new Dictionary<GameObject, List<int>>();  // TODO set this below

        ScenarioMap scenarioMap = GameObject.FindGameObjectWithTag("ScenarioMap").GetComponent<ScenarioMap>();

        foreach (GameObject heroObject in scenarioMap.heroes)
        {
            Hero hero = heroObject.GetComponent<Hero>();
            if (heroObject)
            {
                GameObject heroZone = hero.GetZone();
                Dictionary<GameObject, Unit.MovementPath> heroPossibleDestinations = GetPossibleDestinations(hero, heroZone);
                foreach (GameObject zone in heroPossibleDestinations.Keys)
                {
                    if (!heroMovesRequiredToReachZone.ContainsKey(zone))
                    {
                        heroMovesRequiredToReachZone[zone] = new List<int>();
                    }
                    int movesRequired = (int)Math.Ceiling((double)heroPossibleDestinations[zone].movementSpent / (double)hero.moveSpeed);
                    heroMovesRequiredToReachZone[zone].Add(movesRequired);
                }
            }
        }

        //foreach (HeroIntel heroIntel in heroesIntel)
        //{
        //    GameObject heroObject = GameObject.FindGameObjectWithTag(heroIntel.tag);
        //    if (heroObject)
        //    {
        //        GameObject heroZone = heroObject.GetComponent<Hero>().GetZone();
        //        Dictionary<GameObject, Unit.MovementPath> heroPossibleDestinations = GetPossibleDestinations(heroIntel, heroZone);
        //        foreach (GameObject zone in heroPossibleDestinations.Keys)
        //        {
        //            if (!heroMovesRequiredToReachZone.ContainsKey(zone))
        //            {
        //                heroMovesRequiredToReachZone[zone] = new List<int>();
        //            }
        //            int movesRequired = (int)Math.Ceiling((double)heroPossibleDestinations[zone].movementSpent / (double)heroIntel.moveSpeed);
        //            heroMovesRequiredToReachZone[zone].Add(movesRequired);
        //        }
        //    }
        //}
        foreach (List<int> movesRequiredList in heroMovesRequiredToReachZone.Values)
        {
            movesRequiredList.Sort((x, y) => x.CompareTo(y));
        }
    }

    private static Dictionary<GameObject, Unit.MovementPath> GetPossibleDestinations(Hero hero, GameObject currentZone, Dictionary<GameObject, Unit.MovementPath> possibleDestinations = null, HashSet<GameObject> alreadyPossibleZones = null)
    {
        int totalPossibleMovePoints = hero.moveSpeed * 4;  // Get next 4 rounds of movement from hero
        if (possibleDestinations is null)
        {
            possibleDestinations = new Dictionary<GameObject, Unit.MovementPath> { { currentZone, new Unit.MovementPath() } };
        }

        ZoneInfo currentZoneInfo = currentZone.GetComponent<ZoneInfo>();
        List<GameObject> allAdjacentZones = new List<GameObject>(currentZoneInfo.adjacentZones);
        allAdjacentZones.AddRange(currentZoneInfo.steeplyAdjacentZones);
        //if (hero.wallBreaker > 0)
        //{
        //    allAdjacentZones.AddRange(currentZoneInfo.wall1AdjacentZones);
        //    if (hero.wallBreaker > 1)
        //    {
        //        allAdjacentZones.AddRange(currentZoneInfo.wall2AdjacentZones);
        //        if (hero.wallBreaker > 2)
        //        {
        //            allAdjacentZones.AddRange(currentZoneInfo.wall3AdjacentZones);
        //            if (hero.wallBreaker > 3)
        //            {
        //                allAdjacentZones.AddRange(currentZoneInfo.wall4AdjacentZones);
        //                if (hero.wallBreaker > 4)
        //                {
        //                    allAdjacentZones.AddRange(currentZoneInfo.wall5AdjacentZones);
        //                }
        //            }
        //        }
        //    }
        //}

        if (alreadyPossibleZones != null)
        {
            foreach (GameObject zone in alreadyPossibleZones)
            {
                allAdjacentZones.Remove(zone);
            }
        }

        foreach (GameObject potentialZone in allAdjacentZones)
        {
            ZoneInfo potentialZoneInfo = potentialZone.GetComponent<ZoneInfo>();

            if (potentialZoneInfo.GetCurrentOccupancy() >= potentialZoneInfo.maxOccupancy)
            {
                continue;  // Skip this potentialZone if potentialZone is at maxOccupancy
            }

            int terrainDifficultyCost = currentZoneInfo.terrainDifficulty >= hero.ignoreTerrainDifficulty ? currentZoneInfo.terrainDifficulty - hero.ignoreTerrainDifficulty : 0;
            List<GameObject> frostTokens = potentialZoneInfo.GetAllTokensWithTag("Frost");
            foreach (GameObject frostToken in frostTokens)
            {
                terrainDifficultyCost += frostToken.GetComponent<EnvironToken>().quantity;
            }
            List<GameObject> cryogenicTokens = potentialZoneInfo.GetAllTokensWithTag("Cryogenic");
            foreach (GameObject cryogenicToken in cryogenicTokens)
            {
                terrainDifficultyCost += cryogenicToken.GetComponent<EnvironToken>().quantity;
            }

            int sizeCost = currentZoneInfo.GetCurrentHindranceForHero(hero.tag, true);
            int elevationCost = 0;
            if (currentZoneInfo.steeplyAdjacentZones.Contains(potentialZone))
            {
                elevationCost = Math.Abs(currentZoneInfo.elevation - potentialZoneInfo.elevation);
                elevationCost = elevationCost >= hero.ignoreElevation ? elevationCost - hero.ignoreElevation : 0;
            }
            int wallBreakCost = 0;
            if (currentZoneInfo.wall1AdjacentZones.Contains(potentialZone) || currentZoneInfo.wall2AdjacentZones.Contains(potentialZone) || currentZoneInfo.wall3AdjacentZones.Contains(potentialZone) || currentZoneInfo.wall4AdjacentZones.Contains(potentialZone) || currentZoneInfo.wall5AdjacentZones.Contains(potentialZone))
            {
                wallBreakCost = 2;
            }
            int totalMovementCost = 1 + terrainDifficultyCost + sizeCost + elevationCost + wallBreakCost + possibleDestinations[currentZone].movementSpent;

            if (totalPossibleMovePoints >= totalMovementCost)  // if unit can move here
            {
                int totalTerrainDanger = possibleDestinations[currentZone].terrainDanger + potentialZoneInfo.GetTerrainDangerTotal();
                if (possibleDestinations.ContainsKey(potentialZone))
                {
                    // If two movementPaths to the same zone, prioritize movementPath with less terrain danger. If equal, prioritize movementPath with less totalMovementCost. If equal, prioritize movementPath that breaks a wall, because it's cool!
                    if (totalTerrainDanger < possibleDestinations[potentialZone].terrainDanger || (totalTerrainDanger == possibleDestinations[potentialZone].terrainDanger && (totalMovementCost < possibleDestinations[potentialZone].movementSpent || (totalMovementCost == possibleDestinations[potentialZone].movementSpent && wallBreakCost > 0))))
                    {
                        possibleDestinations[potentialZone].zones = new List<GameObject>(possibleDestinations[currentZone].zones);
                        possibleDestinations[potentialZone].zones.Add(currentZone);
                        possibleDestinations[potentialZone].terrainDanger = totalTerrainDanger;
                        possibleDestinations[potentialZone].movementSpent = totalMovementCost;
                        if (totalPossibleMovePoints > totalMovementCost)
                        {
                            possibleDestinations = GetPossibleDestinations(hero, potentialZone, possibleDestinations, alreadyPossibleZones);
                        }
                    }
                }
                else
                {
                    possibleDestinations.Add(potentialZone, new Unit.MovementPath());
                    possibleDestinations[potentialZone].zones = new List<GameObject>(possibleDestinations[currentZone].zones);
                    possibleDestinations[potentialZone].zones.Add(currentZone);
                    possibleDestinations[potentialZone].terrainDanger = totalTerrainDanger;
                    possibleDestinations[potentialZone].movementSpent = totalMovementCost;
                    if (totalPossibleMovePoints > totalMovementCost)
                    {
                        possibleDestinations = GetPossibleDestinations(hero, potentialZone, possibleDestinations, alreadyPossibleZones);
                    }
                }
            }
        }
        return possibleDestinations;
    }

    //public static void LoadUnitIntelSave(UnitIntelSave unitIntelSave)
    //{
    //    heroesIntel = unitIntelSave.heroesIntel;
    //}

    //public static UnitIntelSave ToJSON()
    //{
    //    return new UnitIntelSave();
    //}
}


//[Serializable]
//public class UnitIntelSave
//{
//    public List<UnitIntel.HeroIntel> heroesIntel;

//    public UnitIntelSave()
//    {
//        heroesIntel = UnitIntel.heroesIntel;
//    }
//}
