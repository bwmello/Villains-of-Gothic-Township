using System;  // for [Serializable]
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class UnitIntel
{
    // Additional unit resources
    public static int universalRerollBonus = 1;  // For adjusting diffficulty of the game. Not taken into account for GetAverageSuccesses/GetChanceOfSuccess
    public static int bonusMovePointsPerRound = 3;
    public static int bonusMovePointsRemaining = bonusMovePointsPerRound;

    // Non-action behavior weight (actions accounted for in MissionSpecifics.cs)
    public static double terrainDangerWeight = -50;  // * terrainDanger
    public static double terrainDangeringFriendlies = -10;  // * terrainDanger * quantity of friendly units.  Chance of unit dying is 2/3 per terrainDanger
    public static double[] bonusMovePointWeight = new double[] { 0, -15, -30, -45 };  // Accessed by UnitIntel.bonusMovePointWeight[movePointsUsed - unit.movePoints], so bonusMovePointWeight[0] = 0
    //public static Dictionary<string, double> partialMoveWeight = new Dictionary<string, double>()
    //{
    //    { "MELEE", .1 }
    //};
    //public static double breakingThroughWallWeight = 10;  // To encourage Superbarn to break through walls just because it's cool

    // Helpers for activating units during villain turn
    public static Stack<GameObject> unitsToActivateLast = new Stack<GameObject>();  // Needed for when a unit's turn negatively impacts other units activating the same turn. Ex: Crowbars in IceToSeeYou activating computers blocking not yet activated Crowbars


    public static void ResetPerRoundResources()
    {
        bonusMovePointsRemaining = bonusMovePointsPerRound;
    }

    public static void LoadUnitIntelSave(UnitIntelSave unitIntelSave)
    {
        //bonusMovePoints = unitIntelSave.bonusMovePoints;
    }

    public static UnitIntelSave ToJSON()
    {
        return new UnitIntelSave();
    }
}


[Serializable]
public class UnitIntelSave
{
    //public int bonusMovePoints;

    //public UnitIntelSave()
    //{
    //    bonusMovePoints = UnitIntel.bonusMovePoints;
    //}
}
