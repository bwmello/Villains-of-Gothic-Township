using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;  // For button
using TMPro;  // for TMP_Text to edit SuccessVsFailure's successContainer blanks
using System.Text.RegularExpressions;  // for comparing Zones (by their numbers) in ActivateCryogenicDevice


/*
 * Things About Static Class
 * 
 * yield return StartCoroutine
 * public static IEnumerator staticClassFunction() { yield return aMonoBehaviorClass.StartCoroutine() } will screw up execution unless you got there by a non-static class calling yield return StartCoroutine(staticClassFunction).
 * Ex: See Interrogate's Draggable.EndDrag() calling Unit.InterrogatedByHeroes() { yield return StartCoroutine(MissionSpecifics.UnitInterrogated(gameObject)); } THEN you can yield return in MissionSpecifics
 * 
 * 
*/


public static class MissionSpecifics
{
    public static string missionName;
    public static GameObject mainCamera;
    public static ScenarioMap scenarioMap;
    public static int currentRound;
    public static string currentPhase = "Setup";  // "Setup", "Villain", "VillainAttack", "Hero", "HeroAnimation", "GameOver"

    public static List<ActionWeight> initialAttackWeightTable = new List<ActionWeight>() { new ActionWeight("Hero", 0, 10, null, new List<string>()), new ActionWeight("HeroAlly", 0, 3, null, new List<string>()) };  // 10 * averageTotalWounds vs heroes, 3 * averageTotalWounds vs heroAllies
    public static Dictionary<string, List<ActionWeight>> actionsWeightTable = new Dictionary<string, List<ActionWeight>>() {};

    public delegate IEnumerator ActionCallback(GameObject unit, GameObject target, int totalSuccesses, int requiredSuccesses);
    public struct ActionWeight
    {
        public string targetType;
        public int requiredSuccesses;
        public double weightFactor;  // Might be per wound or * chanceOfSuccess
        public ActionCallback actionCallback;
        public List<string> restrictedUnits;  // Either empty and there are no restrictions, or unit's name must be in this list  // Only taken into account for MANIPULATION and THOUGHT actions right now
        public bool activateLast;

        public ActionWeight(string newTargetType, int newRequiredSuccesses, double newWeightFactor, ActionCallback newActionCallback, List<string> newRestrictedUnits, bool newActivateLast = false)
        {
            targetType = newTargetType;
            requiredSuccesses = newRequiredSuccesses;
            weightFactor = newWeightFactor;
            actionCallback = newActionCallback;
            restrictedUnits = newRestrictedUnits;
            activateLast = newActivateLast;
        }
    }


    public static void SetActionsWeightTable()
    {
        actionsWeightTable = new Dictionary<string, List<ActionWeight>>() { };

        int totalBombs;
        int totalPrimedBombs;
        int totalComputers;
        int totalJammers;
        int totalActiveJammers;
        int totalToyBoxes;
        int totalTraps;

        switch (missionName)  // Each nonconstant key (ex: "THOUGHT") should be wiped each time and only added back in if conditions are still met
        {
            case "ASinkingFeeling":
                actionsWeightTable["MELEE"] = new List<ActionWeight>(initialAttackWeightTable);
                actionsWeightTable["RANGED"] = new List<ActionWeight>(initialAttackWeightTable);

                totalBombs = GetTotalActiveTokens(new List<string>() { "Bomb" });
                totalPrimedBombs = GetTotalActiveTokens(new List<string>() { "PrimedBomb" });
                totalComputers = GetTotalActiveTokens(new List<string>() { "Computer" });

                actionsWeightTable["MANIPULATION"] = new List<ActionWeight>();
                if (totalBombs > 0)
                {
                    actionsWeightTable["MANIPULATION"].Add(new ActionWeight("Bomb", 3, 100, PrimeBombManually, new List<string>()));  // 100 * chanceOfSuccess, 
                }

                actionsWeightTable["THOUGHT"] = new List<ActionWeight>();
                if (totalComputers > 0 && totalBombs > 0)
                {
                    actionsWeightTable["THOUGHT"].Add(new ActionWeight("Computer", 3, 100, PrimeBombRemotely, new List<string>()));
                }

                actionsWeightTable["GUARD"] = new List<ActionWeight>();
                if (totalPrimedBombs > 0)
                {
                    actionsWeightTable["GUARD"].Add(new ActionWeight("PrimedBomb", 0, 20, null, new List<string>()));  // Flat weight bonus for hindering heroes attempting to disable objectives
                }
                if (totalBombs > 0)
                {
                    actionsWeightTable["GUARD"].Add(new ActionWeight("Bomb", 0, 15, null, new List<string>()));
                    //if (totalComputers > 0)  // Heroes should always be going after bombs, not computers
                    //{
                    //    actionsWeightTable["GUARD"].Add(new ActionWeight("Computer", 0, 5, null));
                    //}
                }
                break;
            case "IceToSeeYou":
                actionsWeightTable["MELEE"] = new List<ActionWeight>(initialAttackWeightTable);
                actionsWeightTable["RANGED"] = new List<ActionWeight>(initialAttackWeightTable);

                totalBombs = GetTotalActiveTokens(new List<string>() { "Bomb" });
                totalComputers = GetTotalActiveTokens(new List<string>() { "Computer" });

                actionsWeightTable["MANIPULATION"] = new List<ActionWeight>() { new ActionWeight("Grenade", 0, 15, null, new List<string>()) };  // 15 * averageAutoWounds * chanceOfSuccess - friendlyFire

                actionsWeightTable["THOUGHT"] = new List<ActionWeight>();
                if (totalComputers > 0)
                {
                    actionsWeightTable["THOUGHT"].Add(new ActionWeight("Computer", 3, 80, ActivateCryogenicDevice, new List<string>(), true));  // Assume you cost the hero at least 1 movepoint and auto deal 2/3 of a wound per cryogenic token, 0 weight if would hit anyone except frosty
                }

                actionsWeightTable["GUARD"] = new List<ActionWeight>();
                if (totalBombs > 0)
                {
                    actionsWeightTable["GUARD"].Add(new ActionWeight("Bomb", 0, 20, null, new List<string>()));
                }
                break;
            case "AFewBadApples":
                actionsWeightTable["MELEE"] = new List<ActionWeight>(initialAttackWeightTable);
                actionsWeightTable["RANGED"] = new List<ActionWeight>(initialAttackWeightTable);
                actionsWeightTable["MELEE"].Add(new ActionWeight("BYSTANDER", 0, 20, null, new List<string>()));
                actionsWeightTable["RANGED"].Add(new ActionWeight("BYSTANDER", 0, 20, null, new List<string>()));

                actionsWeightTable["THOUGHT"] = new List<ActionWeight>();
                UtilityBelt utilityBelt = scenarioMap.UIOverlay.GetComponent<UIOverlay>().utilityBelt.GetComponent<UtilityBelt>();  // Pretty terrible chain of calls just to get claimableTokens
                //List<GameObject> claimableTokens = new List<GameObject>(utilityBelt.claimableTokens);
                foreach (GameObject claimableTokenObject in utilityBelt.claimableTokens)
                {
                    ClaimableToken claimableToken = claimableTokenObject.GetComponent<ClaimableToken>();
                    if (claimableToken.tokenType == "Computer" && !claimableToken.isClaimed)
                    {
                        actionsWeightTable["THOUGHT"].Add(new ActionWeight("Computer", 3, 80, DeactivateComputer, new List<string>()));
                    }
                }
                break;
            case "JamAndSeek":
                actionsWeightTable["MELEE"] = new List<ActionWeight>();
                actionsWeightTable["RANGED"] = new List<ActionWeight>();
                if (currentRound > GetFinalPassiveRound())  // Gives a few passive villain turns before turning aggressive
                {
                    actionsWeightTable["MELEE"] = new List<ActionWeight>(initialAttackWeightTable);
                    actionsWeightTable["RANGED"] = new List<ActionWeight>(initialAttackWeightTable);
                }

                totalJammers = GetTotalActiveTokens(new List<string>() { "Jammer" });
                totalActiveJammers = GetTotalActiveTokens(new List<string>() { "ActiveJammer" });

                actionsWeightTable["MANIPULATION"] = new List<ActionWeight>();
                if (totalJammers > 0)
                {
                    actionsWeightTable["MANIPULATION"].Add(new ActionWeight("Jammer", 2, 100, ActivateJammer, new List<string>()));  // 100 * chanceOfSuccess, 
                }

                actionsWeightTable["GUARD"] = new List<ActionWeight>();
                if (totalActiveJammers > 0)
                {
                    actionsWeightTable["GUARD"].Add(new ActionWeight("ActiveJammer", 0, 20, null, new List<string>()));  // Flat weight bonus for hindering heroes attempting to disable objectives
                }
                if (totalJammers > 0)
                {
                    actionsWeightTable["GUARD"].Add(new ActionWeight("Jammer", 0, 15, null, new List<string>()));
                }
                // Maybe GUARD each other to prevent interrogation, but don't guard BYSTANDERs (if menace > 0)
                break;
            case "RatRace":
                actionsWeightTable["MELEE"] = new List<ActionWeight>(initialAttackWeightTable);
                actionsWeightTable["RANGED"] = new List<ActionWeight>(initialAttackWeightTable);

                actionsWeightTable["MANIPULATION"] = new List<ActionWeight>();
                actionsWeightTable["MANIPULATION"].Add(new ActionWeight(null, 3, 200, SpreadInfection, new List<string>() { "HAZMAT" }));  // 200 * chanceOfSuccess, 

                actionsWeightTable["THOUGHT"] = new List<ActionWeight>();
                actionsWeightTable["THOUGHT"].Add(new ActionWeight(null, 3, 200, SpreadInfection, new List<string>() { "RATSNATCHER" }));
                break;
            case "JackInTheBomb":
                actionsWeightTable["MELEE"] = new List<ActionWeight>(initialAttackWeightTable);
                actionsWeightTable["RANGED"] = new List<ActionWeight>(initialAttackWeightTable);

                totalToyBoxes = GetTotalActiveTokens(new List<string>() { "ToyBox" });
                totalTraps = GetTotalActiveTokens(new List<string>() { "Trap" });

                actionsWeightTable["MANIPULATION"] = new List<ActionWeight>();
                if (totalTraps < 4)  // Max of 4 traps
                {
                    actionsWeightTable["MANIPULATION"].Add(new ActionWeight("Trap", 2, 100, PlaceTrap, new List<string>()));  // 100 * chanceOfSuccess, 
                }

                actionsWeightTable["GUARD"] = new List<ActionWeight>();
                if (totalToyBoxes > 0)
                {
                    actionsWeightTable["GUARD"].Add(new ActionWeight("ToyBox", 0, 20, null, new List<string>()));  // Flat weight bonus for hindering heroes attempting to disable objectives
                }
                break;
        }
    }

    public static double GetComplexActionWeight(string actionType, ActionWeight actionWeight, GameObject actionZone = null)  // ComplexAction is any MANIPULATION (besides grenade) or THOUGHT action
    {
        double weight = actionWeight.weightFactor;
        switch (missionName)
        {
            case "RatRace":
                if (string.IsNullOrEmpty(actionWeight.targetType))  // No target, as rats can be placed in any zone
                {
                    //string ratPlacementWeightDebugString = "GetComplexActionWeight(" + actionType + ", actionWeight, " + actionZone.name + "), initial weight: " + weight.ToString();
                    ZoneInfo actionZoneInfo = actionZone.GetComponent<ZoneInfo>();  // Will throw if actionZone not passed and I like it that way
                    foreach (GameObject rat in GameObject.FindGameObjectsWithTag("Rat"))
                    {
                        Token ratToken = rat.GetComponent<Token>();
                        if (ratToken.IsActive())
                        {
                            GameObject ratZone = ratToken.GetZone();
                            ZoneInfo ratZoneInfo = ratZone.GetComponent<ZoneInfo>();
                            if (ratZone == actionZone)
                            {
                                weight -= actionWeight.weightFactor / 1.1;
                                //ratPlacementWeightDebugString += "\tratZone == actionZone, weight: " + weight.ToString();
                            }
                            else if (actionZoneInfo.adjacentZones.Contains(ratZone))
                            {
                                weight -= actionWeight.weightFactor / 2;
                                //ratPlacementWeightDebugString += "\tactionZone.adjacentZones.Contains(" + ratZone.name + "), weight: " + weight.ToString();
                            }
                            else if (actionZoneInfo.steeplyAdjacentZones.Contains(ratZone))
                            {
                                weight -= actionWeight.weightFactor / 3;
                                //ratPlacementWeightDebugString += "\tactionZone.steeplyAdjacentZones.Contains(" + ratZone.name + "), weight: " + weight.ToString();
                            }
                        }
                    }
                    double heroProximityWeightFactor = UnitIntel.GetProximityWeightFactorForClosestHero(actionZone, weighingClosestHighest : false);
                    heroProximityWeightFactor += (1 - heroProximityWeightFactor) / 1.2;  // .6 / 1.2 = .5, from .4 to .9  // (1 - .4) / 1.5, go from .4 to .8  // (1 - .4) / 2, hero 10 move points away goes from .4 to .7
                    weight *= heroProximityWeightFactor;
                    //ratPlacementWeightDebugString += "\theroProximityWeightFactor: " + heroProximityWeightFactor.ToString() + "), weight: " + weight.ToString();
                    //Debug.Log(ratPlacementWeightDebugString);
                    return weight;
                }
                else  // just return actionWeight.weightFactor
                {
                    return actionWeight.weightFactor;
                }
            default:  // just return actionWeight.weightFactor
                return actionWeight.weightFactor;
        }
    }

    public static int GetFinalRound()
    {
        switch (missionName)
        {
            case "ASinkingFeeling":
                return 7;
            case "IceToSeeYou":
                return 8;
            case "AFewBadApples":
                return 6;
            case "JamAndSeek":
                return 7;
            case "RatRace":
                return 7;
            case "JackInTheBomb":
                return 6;
        }
        return -1;
    }

    public static int GetFinalPassiveRound()  // For JamAndSeek, where the villain spends the first few rounds in a Passive mode
    {
        switch (missionName)
        {
            case "JamAndSeek":
                return 2;  // 2 is maximum  // Must be Aggressive by villain turn 4, which is round 3, so FinalPassiveRound can't be greater than 2
        }
        return 0;
    }

    public static double[] GetRiverActivationWeight()  // Based on villain's energy for mission, reduce weight for activating tiles further down the river (and thus more energy expensive)
    {
        switch (missionName)
        {
            case "IceToSeeYou":
                return new double[] { 1, .8, .6, .4, .2 };
            case "JamAndSeek":
                if (currentRound <= GetFinalPassiveRound())
                {
                    return new double[] { 1, .8, .6, .4, .2 };
                }
                break;
        }
        return new double[] { 1, .9, .8, .7, .6 };  // Only first 3 tiles are looked at right now
    }

    public static int GetBonusMovePointsPerRound()  // For adjusting difficulty of the game.
    {
        switch (missionName)
        {
            case "ASinkingFeeling":
                return 3;
            case "IceToSeeYou":
                return 2;
            case "AFewBadApples":
                return 3;
            case "JamAndSeek":
                if (currentRound <= GetFinalPassiveRound())
                {
                    return 1;
                }
                return 2;
            case "RatRace":
                return 2;
            case "JackInTheBomb":
                return 3;
        }
        return 0;
    }

    public static int GetUniversalIgnoreSizeHindrance()  // For JamAndSeek's passive mode
    {
        switch (missionName)
        {
            case "JamAndSeek":
                if (currentRound <= GetFinalPassiveRound())
                {
                    return 100;  // Basically ignore all size hindrance when moving
                }
                break;
        }
        return 0;
    }

    static System.Random random = new System.Random();

    public static int GetAttackRollBonus()  // For adjusting diffficulty of the game. Applied to Melee, Ranged, and Manipulation:Grenade attacks. Not taken into account for GetAverageSuccesses/GetChanceOfSuccess
    {
        switch (missionName)
        {
            case "ASinkingFeeling":
                return 0;
                //return 1;
                //return random.Next(0, 2);  // .5, half of the time 0, the other half of the time 1
            case "IceToSeeYou":
                return 0;
            case "AFewBadApples":
                return 0;
                //return random.Next(0, 2);  // .5, half of the time 0, the other half of the time 1
            case "JamAndSeek":
                return 0;
            case "RatRace":
                return 0;
            case "JackInTheBomb":
                return 0;
        }
        return 0;
    }

    public static double GetRerollThreshold()  // If averageSuccesses - actualSuccesses >= GetRerollThreshold(), completely reroll action  // TODO Implement this in Unit.PerformAction() or both of the Unit.RollAndReroll()
    {
        switch (missionName)
        {
            case "ASinkingFeeling":
                return 2.25;
            case "IceToSeeYou":
                return 3;
            case "AFewBadApples":
                return 2;
            case "JamAndSeek":
                return 2.75;
            case "RatRace":
                return 2.5;
            case "JackInTheBomb":
                return 2.25;
        }
        return 100;
    }

    public static int[] GetWoundShieldValues()
    {
        //switch (missionName)
        //{
        //    default:
        //        return new int[] { 0, 1, 2 };
        //}
        return new int[] { 0, 1, 2 };  // Can be adjusted like: { 0, 1, 1, 2 } to increase frequency of certain values
    }

    public static void ObjectiveTokenClicked(Button button)
    {
        Token buttonToken = button.gameObject.GetComponent<Token>();
        switch (button.tag)
        {
            case "Bomb":
                switch (missionName)
                {
                    case "IceToSeeYou":
                        GameObject tokenZone = buttonToken.GetZone();
                        GameObject.DestroyImmediate(button.gameObject);
                        tokenZone.GetComponent<ZoneInfo>().AddObjectiveToken("PrimedBomb");
                        return;
                    default:
                        break;
                }
                break;
            case "Briefcase":
                switch (missionName)
                {
                    case "AFewBadApples":
                        UtilityBelt utilityBelt = scenarioMap.UIOverlay.GetComponent<UIOverlay>().utilityBelt.GetComponent<UtilityBelt>();  // Pretty terrible chain of calls just to get claimableTokens
                        if (buttonToken.IsActive())
                        {
                            foreach (GameObject claimableTokenObject in utilityBelt.claimableTokens)
                            {
                                ClaimableToken claimableToken = claimableTokenObject.GetComponent<ClaimableToken>();
                                if (claimableToken.tokenType == "Briefcase")
                                {
                                    if (!claimableToken.isClaimed)
                                    {
                                        claimableToken.ClaimableTokenClicked();
                                    }
                                    break;
                                }
                            }
                        }
                        break;  // Allow token to be faded/unfaded below
                    default:
                        break;
                }
                break;
            case "PrimedBomb":
                switch (missionName)
                {
                    case "IceToSeeYou":
                        GameObject tokenZone = button.gameObject.GetComponent<Token>().GetZone();
                        Object.DestroyImmediate(button.gameObject);
                        tokenZone.GetComponent<ZoneInfo>().AddObjectiveToken("Bomb");
                        return;
                    default:
                        button.transform.Find("BlinkingLight").gameObject.SetActive(false);  // Turns off blinking light and is faded/unfaded below
                        break;
                }
                break;
            case "Computer":
                switch (missionName)
                {
                    case "AFewBadApples":
                        UtilityBelt utilityBelt = scenarioMap.UIOverlay.GetComponent<UIOverlay>().utilityBelt.GetComponent<UtilityBelt>();  // Pretty terrible chain of calls just to get claimableTokens
                        if (buttonToken.IsActive())
                        {
                            foreach (GameObject claimableTokenObject in utilityBelt.claimableTokens)
                            {
                                ClaimableToken claimableToken = claimableTokenObject.GetComponent<ClaimableToken>();
                                if (claimableToken.tokenType == "Computer")
                                {
                                    if (!claimableToken.isClaimed)
                                    {
                                        claimableToken.ClaimableTokenClicked();
                                    }
                                    break;
                                }
                            }
                        }
                        break;  // Allow Computer token to be faded/unfaded below
                    default:
                        break;
                }
                break;
            case "ActiveJammer":
                switch (missionName)
                {
                    default:
                        button.transform.Find("BlinkingLight").gameObject.SetActive(false);  // Turns off blinking light and is faded/unfaded below
                        break;
                }
                break;
        }
        CanvasGroup buttonCanvas = button.GetComponent<CanvasGroup>();
        if (buttonToken.IsActive())  // Token was disabled, so remove from board
        {
            buttonCanvas.alpha = buttonToken.fadedAlpha;
        }
        else  // Mistake was made in removing token, so add token back to the board
        {
            buttonCanvas.alpha = 1f;
        }
    }

    public static int GetTotalActiveTokens(List<string> tokenTags)  // Not really MissionSpecific, so maybe this function belongs in another script
    {
        return GetActiveTokens(tokenTags).Count;
    }

    public static List<GameObject> GetActiveTokens(List<string> tokenTags)  // Not really MissionSpecific, so maybe this function belongs in another script
    {
        List<GameObject> activeTokens = new List<GameObject>();
        foreach (string tokenTag in tokenTags)
        {
            GameObject[] activeAndInactiveTokens = GameObject.FindGameObjectsWithTag(tokenTag);
            foreach (GameObject maybeActiveToken in activeAndInactiveTokens)
            {
                if (maybeActiveToken.GetComponent<Token>().IsActive())
                {
                    activeTokens.Add(maybeActiveToken);
                }
            }
        }
        return activeTokens;
    }

    public static bool IsGameOver(int currentRound)
    {
        switch (missionName)
        {
            case "ASinkingFeeling":
                int totalBombsRemaining = GetTotalActiveTokens(new List<string>() { "Bomb", "PrimedBomb" });
                if (currentRound >= GetFinalRound() || totalBombsRemaining < 2)  // end of hero turn 7 or 4 of 5 bombs are neutralized
                {
                    return true;
                }
                break;
            case "IceToSeeYou":
                int totalPrimedBombs = GetTotalActiveTokens(new List<string>() { "PrimedBomb" });
                if (currentRound >= GetFinalRound() || totalPrimedBombs >= 3)  // end of hero turn 8 or 3 bombs are primed
                {
                    return true;
                }
                break;
            case "AFewBadApples":
                int totalBystandersRemaining = 0;
                foreach (GameObject bystander in GameObject.FindGameObjectsWithTag("BYSTANDER"))
                {
                    if (bystander.GetComponent<Unit>().IsActive())
                    {
                        totalBystandersRemaining++;
                    }
                }
                int totalComputers = GetTotalActiveTokens(new List<string>() { "Computer" });
                UtilityBelt utilityBelt = scenarioMap.UIOverlay.GetComponent<UIOverlay>().utilityBelt.GetComponent<UtilityBelt>();  // Pretty terrible chain of calls just to get claimableTokens
                int totalClaimedTokens = 0;
                bool isComputerTokenClaimed = false;
                foreach (GameObject claimableTokenObject in utilityBelt.claimableTokens)
                {
                    ClaimableToken claimableToken = claimableTokenObject.GetComponent<ClaimableToken>();
                    if (claimableToken.isClaimed)
                    {
                        totalClaimedTokens++;
                        if (claimableToken.tokenType == "Computer")
                        {
                            isComputerTokenClaimed = true;
                        }
                    }
                    else
                    {
                        break;
                    }
                }
                if (currentRound >= GetFinalRound() || totalClaimedTokens >= 3 || totalBystandersRemaining == 0 || (!isComputerTokenClaimed && totalComputers == 0))
                {
                    return true;
                }
                break;
            case "JamAndSeek":
                int totalJammersRemaining = GetTotalActiveTokens(new List<string>() { "Jammer", "ActiveJammer" });
                if (currentRound >= GetFinalRound() || totalJammersRemaining <= 0)  // end of hero turn 7 or all jammers are neutralized
                {
                    return true;
                }
                break;
            case "RatRace":
                int totalRats = GetTotalActiveTokens(new List<string>() { "Rat" });
                if (currentRound >= GetFinalRound() || totalRats >= 6)  // end of hero turn 7 or 6 active rats
                {
                    return true;
                }
                break;
            case "JackInTheBomb":
                int totalToyBoxesRemaining = GetTotalActiveTokens(new List<string>() { "ToyBox" });
                if (currentRound >= GetFinalRound() || totalToyBoxesRemaining < 2)  // end of hero turn 6 or 4 of 5 toyboxes are neutralized
                {
                    return true;
                }
                break;
            default:
                break;
        }
        return false;
    }

    public static bool IsHeroVictory()
    {
        UtilityBelt utilityBelt = scenarioMap.UIOverlay.GetComponent<UIOverlay>().utilityBelt.GetComponent<UtilityBelt>();  // Pretty terrible chain of calls just to get claimableTokens
        switch (missionName)
        {
            case "ASinkingFeeling":
                int totalPrimedBombsRemaining = GetTotalActiveTokens(new List<string>() { "PrimedBomb" });
                if (totalPrimedBombsRemaining < 2)
                {
                    return true;
                }
                break;
            case "IceToSeeYou":
                int totalPrimedBombs = GetTotalActiveTokens(new List<string>() { "PrimedBomb" }); ;
                if (totalPrimedBombs >= 3)
                {
                    return true;
                }
                break;
            case "AFewBadApples":
                int totalBystandersRemaining = GameObject.FindGameObjectsWithTag("BYSTANDER").Length;
                int totalClaimedTokens = 0;
                foreach (GameObject claimableTokenObject in utilityBelt.claimableTokens)
                {
                    ClaimableToken claimableToken = claimableTokenObject.GetComponent<ClaimableToken>();
                    if (claimableToken.isClaimed)
                    {
                        totalClaimedTokens++;
                    }
                    else
                    {
                        break;
                    }
                }
                if (totalClaimedTokens >= 3 && totalBystandersRemaining > 0)
                {
                    return true;
                }
                break;
            case "JamAndSeek":
                int totalActiveJammersRemaining = GetTotalActiveTokens(new List<string>() { "ActiveJammer" });
                if (totalActiveJammersRemaining <= 0)
                {
                    return true;
                }
                break;
            case "RatRace":
                bool gasCapsuleClaimed = utilityBelt.claimableTokens[0].GetComponent<ClaimableToken>().isClaimed;  // bleh, but foreach loop like in AFewBadApples case also bad
                int totalRats = GetTotalActiveTokens(new List<string>() { "Rat" });
                if (gasCapsuleClaimed && totalRats < 2)
                {
                    return true;
                }
                break;
            case "JackInTheBomb":
                int totalToyBoxesRemaining = GetTotalActiveTokens(new List<string>() { "ToyBox" });
                if (totalToyBoxesRemaining < 2)
                {
                    return true;
                }
                break;
            default:
                break;
        }
        return false;
    }

    public static void EndGameAnimation()
    {
        switch (missionName)
        {
            case "ASinkingFeeling":
                if (!IsHeroVictory())
                {
                    foreach (GameObject primedBomb in GetActiveTokens(new List<string>() { "PrimedBomb" }))
                    {
                        scenarioMap.animate.ShowLoopingExplosion(primedBomb.transform.position);
                    }
                }
                break;
            case "IceToSeeYou":
                if (IsHeroVictory())
                {
                    foreach (GameObject primedBomb in GetActiveTokens(new List<string>() { "PrimedBomb" }))
                    {
                        scenarioMap.animate.ShowLoopingExplosion(primedBomb.transform.position);
                    }
                }
                break;
            case "JackInTheBomb":
                if (!IsHeroVictory())
                {
                    foreach (GameObject toyBox in GetActiveTokens(new List<string>() { "ToyBox" }))
                    {
                        scenarioMap.animate.ShowLoopingExplosion(toyBox.transform.position);
                    }
                }
                break;
            default:
                break;
        }
    }

    public static IEnumerator VillainTurnStarted()  // Called by ScenarioMap.StartVillainTurn() before game over check
    {
        switch (missionName)
        {
            case "JamAndSeek":
                if (currentRound == GetFinalPassiveRound() + 1)  // GetFinalPassiveRound() + 1 is the round the villain goes Aggressive
                {
                    GameObject ollygatorEntranceZone = null;
                    foreach (GameObject zone in GameObject.FindGameObjectsWithTag("ZoneInfoPanel"))
                    {
                        if (zone.GetComponent<ZoneInfo>().id == 34)
                        {
                            ollygatorEntranceZone = zone;
                            break;
                        }
                    }
                    GameObject ollygatorUnitSlot = ollygatorEntranceZone.GetComponent<ZoneInfo>().GetAvailableUnitSlot();

                    scenarioMap.animate.CameraToFixedZoom();  // Not the standard reinforcement flow, so doublecheck camera zoom
                    if (!scenarioMap.animate.IsPointOnScreen(ollygatorUnitSlot.transform.position, .01f))  // Reinforcements typically spawned on edges/corners of map, so greatly reduce buffer to prevent slight camera jumps
                    {
                        scenarioMap.animate.mainCamera.transform.position = new Vector3(ollygatorUnitSlot.transform.position.x, ollygatorUnitSlot.transform.position.y, scenarioMap.animate.mainCamera.transform.position.z);
                    }
                    yield return new WaitForSecondsRealtime(1);
                    GameObject ollygator = GameObject.Instantiate(scenarioMap.unitPrefabsMasterDict["OLLYGATOR"], ollygatorUnitSlot.transform);
                    scenarioMap.villainRiver.Insert(0, "OLLYGATOR");  // Insert Ollygator's tile at head of river
                    ollygator.GetComponent<Unit>().GenerateWoundShields();
                    yield return new WaitForSecondsRealtime(2);

                    GameObject livefirePrefab = scenarioMap.unitPrefabsMasterDict["LIVEFIRE"];
                    GameObject[] bystanders = GameObject.FindGameObjectsWithTag("BYSTANDER");
                    List<GameObject> bystanderZones = new List<GameObject>();
                    foreach (GameObject bystander in bystanders)
                    {
                        Unit bystanderUnitInfo = bystander.GetComponent<Unit>();
                        if (bystanderUnitInfo.IsActive())  // Although they couldn't really be inactive at any point during JamAndSeek mission
                        {
                            bystanderZones.Add(bystanderUnitInfo.GetZone());
                        }
                    }
                    GameObject livefireEntranceZone = scenarioMap.ChooseBestUnitPlacement(livefirePrefab, bystanderZones).Item1;
                    GameObject livefireUnitSlot = null;
                    foreach (Unit entranceZoneUnit in livefireEntranceZone.GetComponent<ZoneInfo>().GetUnitsInfo())
                    {
                        if (entranceZoneUnit.CompareTag("BYSTANDER"))
                        {
                            livefireUnitSlot = entranceZoneUnit.GetUnitSlot();
                            break;
                        }
                    }
                    if (!scenarioMap.animate.IsPointOnScreen(livefireUnitSlot.transform.position, .01f))  // Reinforcements typically spawned on edges/corners of map, so greatly reduce buffer to prevent slight camera jumps
                    {
                        scenarioMap.animate.mainCamera.transform.position = new Vector3(livefireUnitSlot.transform.position.x, livefireUnitSlot.transform.position.y, scenarioMap.animate.mainCamera.transform.position.z);
                    }
                    yield return new WaitForSecondsRealtime(1);
                    for (int i = bystanders.Length - 1; i >= 0; i--)  // Remove all BYSTANDERS from board and river
                    {
                        GameObject.DestroyImmediate(bystanders[i]);
                    }
                    scenarioMap.villainRiver.Remove("BYSTANDER");
                    GameObject livefire = GameObject.Instantiate(livefirePrefab, livefireUnitSlot.transform);
                    scenarioMap.villainRiver.Add("LIVEFIRE");  // Add Livefire's tile to end of river
                    livefire.GetComponent<Unit>().GenerateWoundShields();
                    yield return new WaitForSecondsRealtime(2);
                }
                break;
        }
        yield return 0;
    }

    public static IEnumerator CharacterTileActivated()  // Doesn't count REINFORCEMENT
    {
        switch (missionName)
        {
            case "JamAndSeek":
                if (currentRound <= GetFinalPassiveRound())
                {
                    int[] redDieValues = new int[] { 0, 1, 1, 2, 2, 3 };
                    yield return scenarioMap.animate.StartCoroutine(scenarioMap.CallReinforcements(redDieValues[random.Next(redDieValues.Length)]));
                }
                break;
        }
        yield return 0;
    }

    public static int GetReinforcementPoints()
    {
        switch (missionName)
        {
            case "ASinkingFeeling":
                return 5;
            //return 1;
            //return random.Next(0, 2);  // .5, half of the time 0, the other half of the time 1
            case "IceToSeeYou":
                return 3;
            case "AFewBadApples":
                return 5;
            //return random.Next(0, 2);  // .5, half of the time 0, the other half of the time 1
            case "JamAndSeek":
                UtilityBelt utilityBelt = scenarioMap.UIOverlay.GetComponent<UIOverlay>().utilityBelt.GetComponent<UtilityBelt>();  // Pretty terrible chain of calls just to get claimableTokens
                foreach (GameObject claimableTokenObject in utilityBelt.claimableTokens)  // Activate first unclaimed claimableInformation token
                {
                    ClaimableToken claimableToken = claimableTokenObject.GetComponent<ClaimableToken>();
                    if (claimableToken.tokenType == "Information")
                    {
                        if (claimableToken.isClaimed)
                        {
                            return 1;
                        }
                    }
                }
                return 2;
            case "RatRace":
                return 3;
            case "JackInTheBomb":
                return 5;
        }
        return 0;
    }

    public static double GetReinforcementWeight()
    {
        double weight = 0;
        switch (missionName)
        {
            case "ASinkingFeeling":
                GameObject superBarn = GameObject.FindGameObjectWithTag("SUPERBARN");
                if (superBarn != null)
                {
                    weight = superBarn.GetComponent<Unit>().GetMostValuableActionWeight();
                }
                break;
            case "JamAndSeek":
                if (GetReinforcementPoints() == 1)  // Meaning they have a claimableInformation token
                {
                    if (currentRound > GetFinalPassiveRound())
                    {
                        weight = 30;  // Sure, 30 points for getting rid of an information token
                    }
                    else
                    {
                        weight = 10;  // You don't get the red reinforcement die if you activate REINFORCEMENTS instead of a character tile, so wait until you're no longer passive to reinforce
                    }
                }
                break;
            case "RatRace":
                GameObject ratSnatcher = GameObject.FindGameObjectWithTag("RATSNATCHER");
                int activeRats = GetTotalActiveTokens(new List<string>() { "Rat" });
                if (ratSnatcher != null && activeRats > 0)
                {
                    weight = 5;
                }
                break;
            case "JackInTheBomb":
                if (currentRound > 3)  // hyenas can't activate earlier than round 3, so assume hero bitten after this point
                {
                    weight = 10;
                }
                break;
        }
        return weight;
    }

    public static IEnumerator ActivateReinforcement()
    {
        //string debugString = "MissionSpecifics.ActivateReinforcement(), missionName " + missionName;
        switch (missionName)
        {
            case "ASinkingFeeling":
                GameObject superBarn = GameObject.FindGameObjectWithTag("SUPERBARN");
                if (superBarn == null)
                {
                    GameObject barn = GameObject.FindGameObjectWithTag("BARN");
                    if (barn != null)
                    {
                        GameObject superbarnPrefab = scenarioMap.unitPrefabsMasterDict["SUPERBARN"];
                        if (superbarnPrefab != null)
                        {
                            Unit barnInfo = barn.GetComponent<Unit>();
                            Unit superBarnInfo = GameObject.Instantiate(superbarnPrefab, barn.transform.parent).GetComponent<Unit>();
                            superBarnInfo.ModifyLifePoints(barnInfo.lifePoints - barnInfo.lifePointsMax);  // Do not reset SuperBarn's life points to 6 if Barn was damaged.
                            superBarnInfo.GenerateWoundShields();
                            GameObject.DestroyImmediate(barn);
                            int barnRiverIndex = scenarioMap.villainRiver.IndexOf("BARN");
                            scenarioMap.villainRiver[barnRiverIndex] = "SUPERBARN";
                        }
                        else
                        {
                            Debug.LogError("ERROR! In ScenarioMap.CallReinforcements(), unable to find prefab asset for SUPERBARN at UnitPrefabs/SUPERBARN.prefab");
                        }
                    }
                }
                else
                {
                    Unit superBarnInfo = superBarn.GetComponent<Unit>();
                    yield return scenarioMap.animate.StartCoroutine(superBarnInfo.ActivateUnit());
                    superBarnInfo.ModifyLifePoints(-2);
                    if (superBarnInfo.lifePoints < 1)
                    {
                        GameObject.Destroy(superBarn);
                    }
                }
                break;
            case "JamAndSeek":
                UtilityBelt utilityBelt = scenarioMap.UIOverlay.GetComponent<UIOverlay>().utilityBelt.GetComponent<UtilityBelt>();  // Pretty terrible chain of calls just to get claimableTokens
                foreach (GameObject claimableTokenObject in utilityBelt.claimableTokens)  // Activate first unclaimed claimableInformation token
                {
                    ClaimableToken claimableToken = claimableTokenObject.GetComponent<ClaimableToken>();
                    if (claimableToken.tokenType == "Information")
                    {
                        if (claimableToken.isClaimed)
                        {
                            yield return scenarioMap.animate.StartCoroutine(scenarioMap.animate.ClaimableTokenTargeted(claimableTokenObject));
                            claimableToken.ClaimableTokenClicked();
                            break;
                        }
                    }
                }
                break;
            case "RatRace":
                GameObject ratSnatcher = GameObject.FindGameObjectWithTag("RATSNATCHER");
                List<GameObject> activeRats = GetActiveTokens(new List<string>() { "Rat" });
                if (ratSnatcher != null && activeRats.Count > 0)
                {
                    //debugString += "\nRATSNATCHER is active and activeRats.Count " + activeRats.Count.ToString();
                    List<(GameObject, GameObject, double)> ratMovesByWeight = new List<(GameObject, GameObject, double)>();  // rat, zone, weight
                    ActionWeight spreadInfectionAction = new ActionWeight(null, 3, 100, SpreadInfection, null);  // Generic SpreadInfection actionWeight or GetComplexActionWeight()

                    foreach (GameObject rat in activeRats)
                    {
                        rat.GetComponent<Token>().TokenButtonClicked(rat.GetComponent<Button>());  // Deactivate rat so it's not counting itself when calling GetComplexActionWeight()
                        GameObject ratZone = rat.GetComponent<Token>().GetZone();
                        ZoneInfo ratZoneInfo = ratZone.GetComponent<ZoneInfo>();
                        double ratOriginalZoneWeight = GetComplexActionWeight(null, spreadInfectionAction, ratZone);
                        //ratMovesByWeight.Add((rat, ratZone, 0));

                        HashSet<GameObject> possibleZones = new HashSet<GameObject>(ratZoneInfo.adjacentZones);
                        possibleZones.UnionWith(ratZoneInfo.steeplyAdjacentZones);  // Like AddRange, but non-unique zones are discarded
                        HashSet<GameObject> adjacentZones = new HashSet<GameObject>(possibleZones);  // Needed bc you can't add to possibleZones while iterating over possibleZones
                        foreach (GameObject zone in adjacentZones)  // Get the adjacent zones of the adjacent zones
                        {
                            ZoneInfo zoneInfo = zone.GetComponent<ZoneInfo>();
                            possibleZones.UnionWith(zoneInfo.adjacentZones);
                            possibleZones.UnionWith(zoneInfo.steeplyAdjacentZones);
                        }
                        //debugString += "\nFor rat in " + ratZone.name + ", with " + possibleZones.Count.ToString() + " possibleZones and ratOriginalZoneWeight " + ratOriginalZoneWeight.ToString();

                        foreach (GameObject zone in possibleZones)
                        {
                            double ratInZoneWeight = GetComplexActionWeight(null, spreadInfectionAction, zone);  // If rat is not disabled, it will count against itself as another rat
                            double ratMoveWeight = ratInZoneWeight - ratOriginalZoneWeight;
                            if (ratMoveWeight > 0)
                            {
                                //debugString += "\tAdding " + zone.name + " with ratMoveWeight " + ratMoveWeight.ToString();
                                ratMovesByWeight.Add((rat, zone, ratMoveWeight));
                            }
                        }

                        rat.GetComponent<Token>().TokenButtonClicked(rat.GetComponent<Button>());  // Reactivate rat
                    }

                    if (ratMovesByWeight.Count > 0)
                    {
                        ratMovesByWeight.Sort((x, y) => y.Item3.CompareTo(x.Item3));  // Sorts from highest weight tuple to lowest
                        GameObject ratToMove = ratMovesByWeight[0].Item1;
                        ZoneInfo zoneOriginInfo = ratToMove.GetComponent<Token>().GetZone().GetComponent<ZoneInfo>();
                        GameObject zoneDestination = ratMovesByWeight[0].Item2;
                        ZoneInfo zoneDestinationInfo = zoneDestination.GetComponent<ZoneInfo>();
                        scenarioMap.animate.PostionCameraBeforeCameraMove(ratToMove.transform.position, zoneDestination.transform.position);
                        ratToMove.transform.SetParent(scenarioMap.animationContainer.transform);
                        yield return new WaitForSecondsRealtime(1f);  // Pause with camera on rat before move
                        scenarioMap.animate.StartCoroutine(scenarioMap.animate.MoveCameraUntilOnscreen(scenarioMap.animate.mainCamera.transform.position, zoneDestination.transform.position));
                        if (!zoneOriginInfo.adjacentZones.Contains(zoneDestination) && !zoneOriginInfo.steeplyAdjacentZones.Contains(zoneDestination))  // If origin and destination are not adjacent
                        {
                            HashSet<GameObject> originAllAdjacentZones = new HashSet<GameObject>(zoneOriginInfo.adjacentZones);
                            originAllAdjacentZones.UnionWith(zoneOriginInfo.steeplyAdjacentZones);
                            foreach (GameObject originAdjacentZone in originAllAdjacentZones)
                            {
                                if (zoneDestinationInfo.adjacentZones.Contains(originAdjacentZone) || zoneDestinationInfo.steeplyAdjacentZones.Contains(originAdjacentZone))
                                {
                                    yield return scenarioMap.animate.StartCoroutine(scenarioMap.animate.MoveObjectOverTime(new List<GameObject>() { ratToMove }, ratToMove.transform.position, originAdjacentZone.transform.position));
                                    break;
                                }
                            }
                        }
                        yield return scenarioMap.animate.StartCoroutine(scenarioMap.animate.MoveObjectOverTime(new List<GameObject>() { ratToMove }, ratToMove.transform.position, zoneDestination.transform.position));
                        GameObject.DestroyImmediate(ratToMove);
                        zoneDestination.GetComponent<ZoneInfo>().AddObjectiveToken("Rat");
                        yield return new WaitForSecondsRealtime(1f);  // Pause with camera after rat move
                    }
                }
                break;
        }
        //Debug.Log(debugString);
        yield return 0;
    }

    public static void UnitKilled(GameObject unit)
    {
        switch (missionName)
        {
            case "AFewBadApples":
                if (unit.CompareTag("MUDMAN") || unit.CompareTag("SKULLFACE"))
                {
                    unit.GetComponent<Unit>().GetZone().GetComponent<ZoneInfo>().AddObjectiveToken("Briefcase");
                }
                break;
            case "RatRace":
                if (unit.CompareTag("LAZARUS") || unit.CompareTag("RATSNATCHER"))
                {
                    UtilityBelt utilityBelt = scenarioMap.UIOverlay.GetComponent<UIOverlay>().utilityBelt.GetComponent<UtilityBelt>();  // Pretty terrible chain of calls just to get claimableTokens
                    foreach (GameObject claimableTokenObject in utilityBelt.claimableTokens)
                    {
                        ClaimableToken claimableToken = claimableTokenObject.GetComponent<ClaimableToken>();
                        if (claimableToken.tokenType == "GasCapsule")
                        {
                            if (!claimableToken.isClaimed)
                            {
                                claimableToken.ClaimableTokenClicked();
                            }
                            break;
                        }
                    }
                }
                break;
            default:
                break;
        }
    }

    public static void UnitResuscitated(GameObject unit)
    {
        switch (missionName)
        {
            case "AFewBadApples":
                if (unit.CompareTag("MUDMAN") || unit.CompareTag("SKULLFACE"))
                {
                    unit.GetComponent<Unit>().GetZone().GetComponent<ZoneInfo>().RemoveObjectiveToken("Briefcase");
                }
                break;
            case "RatRace":
                bool ratsnatcherIsAlive = false;
                bool lazarusIsAlive = false;
                try
                {
                    ratsnatcherIsAlive = GameObject.FindGameObjectWithTag("RATSNATCHER").GetComponent<Unit>().IsActive();
                }
                catch { }
                try
                {
                    lazarusIsAlive = GameObject.FindGameObjectWithTag("LAZARUS").GetComponent<Unit>().IsActive();
                }
                catch { }

                if ((unit.CompareTag("LAZARUS") && ratsnatcherIsAlive) || (unit.CompareTag("RATSNATCHER") && lazarusIsAlive))
                {
                    UtilityBelt utilityBelt = scenarioMap.UIOverlay.GetComponent<UIOverlay>().utilityBelt.GetComponent<UtilityBelt>();  // Pretty terrible chain of calls just to get claimableTokens
                    foreach (GameObject claimableTokenObject in utilityBelt.claimableTokens)
                    {
                        ClaimableToken claimableToken = claimableTokenObject.GetComponent<ClaimableToken>();
                        if (claimableToken.tokenType == "GasCapsule")
                        {
                            if (claimableToken.isClaimed)
                            {
                                claimableToken.ClaimableTokenClicked();
                            }
                            break;
                        }
                    }
                }
                break;
            default:
                break;
        }
    }

    public static IEnumerator UnitInterrogated(GameObject unit)
    {
        UtilityBelt utilityBelt = scenarioMap.UIOverlay.GetComponent<UIOverlay>().utilityBelt.GetComponent<UtilityBelt>();  // Pretty terrible chain of calls just to get claimableTokens
        Unit unitInfo = unit.GetComponent<Unit>();
        switch (missionName)
        {
            case "AFewBadApples":
                foreach (GameObject claimableTokenObject in utilityBelt.claimableTokens)  // Activate SwatRifle claimable token if unclaimed (and it should be)
                {
                    ClaimableToken claimableToken = claimableTokenObject.GetComponent<ClaimableToken>();
                    if (claimableToken.tokenType == "SwatRifle")
                    {
                        if (!claimableToken.isClaimed)
                        {
                            claimableToken.ClaimableTokenClicked();
                        }
                        break;
                    }
                }

                foreach (ScenarioMap.UnitPool unitPool in scenarioMap.unitsPool)  // Remove one from the SwatRifle unitPool
                {
                    if (unitPool.unit.CompareTag(unit.tag))
                    {
                        unitPool.total -= 1;
                        break;
                    }
                }

                //unitInfo.ModifyLifePoints(-unitInfo.lifePoints);  // This allows player to reset if mistake, but then are you going to add a UnitResusictated case just for adding 1 back into the unitsPool for the SwatRifles
                GameObject.Destroy(unit);  // And lastly remove the interrogated SwatRifle, but don't use DestroyImmediate so that Draggable.cs can do cleanup (disabling SwatRifle's dropzone)

                break;
            case "JamAndSeek":
                foreach (GameObject claimableTokenObject in utilityBelt.claimableTokens)  // Activate first unclaimed claimableInformation token
                {
                    ClaimableToken claimableToken = claimableTokenObject.GetComponent<ClaimableToken>();
                    if (claimableToken.tokenType == "Information")
                    {
                        if (!claimableToken.isClaimed)
                        {
                            claimableToken.ClaimableTokenClicked();
                            break;
                        }
                    }
                }
                int originalUnitIgnoreSize = unitInfo.ignoreSize;
                unitInfo.ignoreSize = 100;
                UnitIntel.bonusMovePointsRemaining = 0;  // Will be reset by UnitIntel at start of villain turn
                currentPhase = "HeroAnimation";
                scenarioMap.DisablePlayerUI();
                yield return scenarioMap.animate.StartCoroutine(unitInfo.ForceMovement());
                currentPhase = "Hero";
                scenarioMap.EnablePlayerUI();
                unitInfo.ignoreSize = originalUnitIgnoreSize;
                break;
            default:
                break;
        }
        yield return 0;
    }

    public static List<GameObject> GetInterrogationTargets()
    {
        List<GameObject> interrogationTargets = new List<GameObject>();
        switch (missionName)
        {
            case "AFewBadApples":
                return new List<GameObject>(GameObject.FindGameObjectsWithTag("SWATRIFLE"));
            case "JamAndSeek":
                List<GameObject> potentialInterrogationTargets = new List<GameObject>();
                potentialInterrogationTargets.AddRange(GameObject.FindGameObjectsWithTag("PRISONER"));
                potentialInterrogationTargets.AddRange(GameObject.FindGameObjectsWithTag("BYSTANDER"));
                potentialInterrogationTargets.AddRange(GameObject.FindGameObjectsWithTag("UZI"));
                potentialInterrogationTargets.AddRange(GameObject.FindGameObjectsWithTag("SHOTGUN"));
                potentialInterrogationTargets.AddRange(GameObject.FindGameObjectsWithTag("CHAINS"));
                foreach (GameObject potentialInterrogationTarget in potentialInterrogationTargets)
                {
                    if (potentialInterrogationTarget.GetComponent<Unit>().IsActive())
                    {
                        interrogationTargets.Add(potentialInterrogationTarget);
                    }
                }
                return interrogationTargets;
            default:
                break;
        }
        return null;
    }

    public static List<Unit.UnitPossibleAction> GetPredeterminedActivations()
    {
        switch (missionName)
        {
            //case "ASinkingFeeling":
            //    return null;
            case "IceToSeeYou":
                switch (currentRound)
                {
                    case 1:
                        GameObject otterPop = GameObject.FindGameObjectWithTag("OTTERPOP");
                        GameObject zone26 = scenarioMap.gameObject.transform.Find("ZoneInfoPanel 26").gameObject;
                        GameObject zone25 = scenarioMap.gameObject.transform.Find("ZoneInfoPanel 25").gameObject;
                        GameObject zone22 = scenarioMap.gameObject.transform.Find("ZoneInfoPanel 22").gameObject;
                        GameObject zone15 = scenarioMap.gameObject.transform.Find("ZoneInfoPanel 15").gameObject;
                        GameObject zone16 = scenarioMap.gameObject.transform.Find("ZoneInfoPanel 16").gameObject;
                        Unit.MovementPath zone22MovePath = new Unit.MovementPath(2, 0, new List<GameObject>() { zone26, zone25 });
                        Dictionary<GameObject, Unit.MovementPath> possibleDestinations = new Dictionary<GameObject, Unit.MovementPath>
                        {
                            { zone22, zone22MovePath },
                            { zone15, new Unit.MovementPath(4, 0, new List<GameObject>() { zone25, zone22 }) },
                            { zone16, new Unit.MovementPath(4, 0, new List<GameObject>() { zone25, zone22 }) },
                        };

                        List<Unit.UnitPossibleAction> allPossibleUnitActions = otterPop.GetComponent<Unit>().GetPossibleActions(possibleDestinations);
                        Unit.UnitPossibleAction chosenAction = new Unit.UnitPossibleAction(otterPop.GetComponent<Unit>(), new ActionWeight(null, 0, 0, null, null), new Unit.ActionProficiency(null, 0, null), 0, zone22, zone22, zone22MovePath, null, null);
                        foreach (Unit.UnitPossibleAction unitAction in allPossibleUnitActions)
                        {
                            if (chosenAction == null || unitAction.actionWeight > chosenAction.actionWeight)
                            {
                                chosenAction = unitAction;
                            }
                        }

                        //return new Dictionary<GameObject, Unit.UnitPossibleAction> { { otterPop, chosenAction } };
                        return new List<Unit.UnitPossibleAction> { chosenAction };

                    default:
                        break;
                }
                break;

            default:
                break;
        }
        return null;
    }

    /* ActionCallbacks specific to "ASinkingFeeling" mission */
    public static IEnumerator PrimeBombManually(GameObject unit, GameObject target, int totalSuccesses, int requiredSuccesses)
    {
        ZoneInfo unitZoneInfo = unit.GetComponent<Unit>().GetZone().GetComponent<ZoneInfo>();
        // Animate success vs failure UI
        GameObject successContainer = GameObject.Instantiate(unitZoneInfo.successVsFailurePrefab, scenarioMap.animationContainer.transform);
        successContainer.transform.position = unit.transform.TransformPoint(new Vector3(0, 12, 0));

        successContainer.GetComponent<RectTransform>().sizeDelta = new Vector2(requiredSuccesses * 10, successContainer.GetComponent<RectTransform>().rect.height);
        Transform successContainerText = successContainer.transform.GetChild(0);
        successContainerText.GetComponent<RectTransform>().sizeDelta = new Vector2(requiredSuccesses * 10, successContainerText.GetComponent<RectTransform>().rect.height);
        string successContainerBlanks = "_";
        for (int i = 1; i < requiredSuccesses; i++)
        {
            successContainerBlanks += " _";
        }
        successContainerText.GetComponent<TMP_Text>().text = successContainerBlanks;

        for (int i = -1; i < (totalSuccesses >= 0 ? totalSuccesses : 0); i++)
        {
            yield return new WaitForSecondsRealtime(1);
            if (i == requiredSuccesses - 1)  // Otherwise there can be more results (checks/x's) displayed than requiredSuccesses
            {
                break;
            }
            GameObject successOrFailurePrefab = i + 1 < totalSuccesses ? unitZoneInfo.successPrefab : unitZoneInfo.failurePrefab;
            GameObject successOrFailureMarker = GameObject.Instantiate(successOrFailurePrefab, successContainer.transform);
        }
        yield return new WaitForSecondsRealtime(1);

        if (totalSuccesses >= requiredSuccesses)
        {
            Vector3 furtherPoint = scenarioMap.animate.GetFurtherPointOnLine(mainCamera.transform.position, unitZoneInfo.transform.position);
            yield return scenarioMap.animate.StartCoroutine(scenarioMap.animate.MoveCameraUntilOnscreen(mainCamera.transform.position, furtherPoint));  // Move camera to zone of bomb being armed
            unitZoneInfo.RemoveObjectiveToken("Bomb");
            unitZoneInfo.AddObjectiveToken("PrimedBomb");
            yield return new WaitForSecondsRealtime(2);
            SetActionsWeightTable();
        }
        GameObject.Destroy(successContainer);
        yield return 0;
    }

    public static IEnumerator PrimeBombRemotely(GameObject unit, GameObject target, int totalSuccesses, int requiredSuccesses)
    {
        ZoneInfo unitZoneInfo = unit.GetComponent<Unit>().GetZone().GetComponent<ZoneInfo>();
        // Animate success vs failure UI
        GameObject successContainer = GameObject.Instantiate(unitZoneInfo.successVsFailurePrefab, scenarioMap.animationContainer.transform);
        successContainer.transform.position = unit.transform.TransformPoint(new Vector3(0, 12, 0));

        successContainer.GetComponent<RectTransform>().sizeDelta = new Vector2(requiredSuccesses * 10, successContainer.GetComponent<RectTransform>().rect.height);
        Transform successContainerText = successContainer.transform.GetChild(0);
        successContainerText.GetComponent<RectTransform>().sizeDelta = new Vector2(requiredSuccesses * 10, successContainerText.GetComponent<RectTransform>().rect.height);
        string successContainerBlanks = "_";
        for (int i = 1; i < requiredSuccesses; i++)
        {
            successContainerBlanks += " _";
        }
        successContainerText.GetComponent<TMP_Text>().text = successContainerBlanks;

        for (int i = -1; i < (totalSuccesses >= 0 ? totalSuccesses : 0); i++)
        {
            yield return new WaitForSecondsRealtime(1);
            if (i == requiredSuccesses - 1)  // Otherwise there can be more results (checks/x's) displayed than requiredSuccesses
            {
                break;
            }
            GameObject successOrFailurePrefab = i + 1 < totalSuccesses ? unitZoneInfo.successPrefab : unitZoneInfo.failurePrefab;
            GameObject successOrFailureMarker = GameObject.Instantiate(successOrFailurePrefab, successContainer.transform);
        }
        yield return new WaitForSecondsRealtime(1);

        if (totalSuccesses >= requiredSuccesses)
        {
            List<ZoneInfo> bombZones = new List<ZoneInfo>();
            foreach (GameObject bomb in GameObject.FindGameObjectsWithTag("Bomb"))
            {
                bombZones.Add(bomb.transform.parent.parent.GetComponentInParent<ZoneInfo>());
            }
            ZoneInfo chosenBombZone = null;
            double leastManipulationChance = 100;
            foreach (ZoneInfo bombZone in bombZones)
            {
                double bombZoneManipulationChance = bombZone.GetOccupantsManipulationLikelihood(3, true);
                //Debug.Log("!!!" + bombZone.transform.name + " with bombZoneManipulationChance: " + bombZoneManipulationChance.ToString());
                if (bombZoneManipulationChance < leastManipulationChance)
                {
                    leastManipulationChance = bombZoneManipulationChance;
                    chosenBombZone = bombZone;
                }
            }
            if (chosenBombZone != null)
            {
                unitZoneInfo.RemoveObjectiveToken("Computer");
                Vector3 furtherPoint = scenarioMap.animate.GetFurtherPointOnLine(mainCamera.transform.position, chosenBombZone.transform.position);
                yield return scenarioMap.animate.StartCoroutine(scenarioMap.animate.MoveCameraUntilOnscreen(mainCamera.transform.position, furtherPoint));  // Move camera to zone of bomb being armed
                chosenBombZone.RemoveObjectiveToken("Bomb");
                chosenBombZone.AddObjectiveToken("PrimedBomb");
                yield return new WaitForSecondsRealtime(2);
                //unitTurn.targetedZone = chosenBombZone.transform.gameObject;  // Only useful for DEBUG statement at end of PerformAction()
            }
            else
            {
                Debug.LogError("ERROR! Tried to use a computer from " + unitZoneInfo.name + " to prime a bomb, but no zones with bombs available. Why was computer able to be used if there are no zones with bombs?");
            }
            SetActionsWeightTable();
        }
        GameObject.Destroy(successContainer);
        yield return 0;
    }

    /* ActionCallbacks specific to "IceToSeeYou" mission */
    // Must stack the left side, as the hero gets screwed coming down the right hand side (Otterpop and reinforcements)
    public static IEnumerator ActivateCryogenicDevice(GameObject unit, GameObject target, int totalSuccesses, int requiredSuccesses)
    {
        ZoneInfo unitZoneInfo = unit.GetComponent<Unit>().GetZone().GetComponent<ZoneInfo>();
        // Animate success vs failure UI
        GameObject successContainer = GameObject.Instantiate(unitZoneInfo.successVsFailurePrefab, scenarioMap.animationContainer.transform);
        successContainer.transform.position = unit.transform.TransformPoint(new Vector3(0, 12, 0));

        successContainer.GetComponent<RectTransform>().sizeDelta = new Vector2(requiredSuccesses * 10, successContainer.GetComponent<RectTransform>().rect.height);
        Transform successContainerText = successContainer.transform.GetChild(0);
        successContainerText.GetComponent<RectTransform>().sizeDelta = new Vector2(requiredSuccesses * 10, successContainerText.GetComponent<RectTransform>().rect.height);
        string successContainerBlanks = "_";
        for (int i = 1; i < requiredSuccesses; i++)
        {
            successContainerBlanks += " _";
        }
        successContainerText.GetComponent<TMP_Text>().text = successContainerBlanks;

        for (int i = -1; i < (totalSuccesses >= 0 ? totalSuccesses : 0); i++)
        {
            yield return new WaitForSecondsRealtime(1);
            if (i == requiredSuccesses - 1)  // Otherwise there can be more results (checks/x's) displayed than requiredSuccesses
            {
                break;
            }
            GameObject successOrFailurePrefab = i + 1 < totalSuccesses ? unitZoneInfo.successPrefab : unitZoneInfo.failurePrefab;
            GameObject successOrFailureMarker = GameObject.Instantiate(successOrFailurePrefab, successContainer.transform);
        }
        yield return new WaitForSecondsRealtime(1);

        if (totalSuccesses >= requiredSuccesses)
        {
            List<(double, GameObject)> cryoZoneTargets = new List<(double, GameObject)>();

            GameObject hero = GameObject.FindGameObjectWithTag("1stHero");
            GameObject heroZone = hero.GetComponent<Hero>().GetZone();
            int heroZoneID = heroZone.GetComponent<ZoneInfo>().GetZoneID();
            cryoZoneTargets.Add((30, heroZone));

            foreach (GameObject bomb in GameObject.FindGameObjectsWithTag("Bomb"))
            {
                GameObject bombZone = bomb.transform.parent.parent.gameObject;
                double zoneWeightMultiplier = UnitIntel.GetProximityWeightFactorForClosestHero(bombZone);  // double heroProximityWeightFactor
                ZoneInfo bombZoneInfo = bombZone.GetComponent<ZoneInfo>();
                cryoZoneTargets.Add((18 * zoneWeightMultiplier, bombZone));

                if ((bombZoneInfo.adjacentZones.Count + bombZoneInfo.steeplyAdjacentZones.Count) == 1)  // If only one way in or out (ignoring walls)
                {
                    if (bombZoneInfo.adjacentZones.Count == 1)
                    {
                        cryoZoneTargets.Add((20 * zoneWeightMultiplier, bombZoneInfo.adjacentZones[0]));
                    }
                    else
                    {
                        cryoZoneTargets.Add((20 * zoneWeightMultiplier, bombZoneInfo.steeplyAdjacentZones[0]));
                    }
                }
                else if (bombZone.name == "ZoneInfoPanel 38")   // Hardcoded, TODO should be with above (if each adjacentZone has adjacentZones.Count == 1)
                {
                    cryoZoneTargets.Add((20 * zoneWeightMultiplier, bombZoneInfo.adjacentZones[0]));
                }
                else if (bombZone.name == "ZoneInfoPanel 26")  // Hardcoded, TODO should look at difference between moveCost for hero (before and after cryo token)
                {
                    if (heroZoneID < 17)
                    {
                        GameObject zone22 = bombZoneInfo.adjacentZones[0].GetComponent<ZoneInfo>().adjacentZones[0];
                        cryoZoneTargets.Add((19 * zoneWeightMultiplier, zone22));
                    }
                }
            }

            if (currentRound < 3)
            {
                if (new List<int>() { 10, 11, 15, 16 }.Contains(heroZoneID))
                {
                    GameObject zone14 = scenarioMap.gameObject.transform.Find("ZoneInfoPanel 14").gameObject;
                    cryoZoneTargets.Add((50, zone14));
                }
            }

            // Not allowed to cryo zones with existing cryo tokens
            List<(double, GameObject)> validCryoZoneTargets = new List<(double, GameObject)>();
            foreach ((double, GameObject) potentialCryoZone in cryoZoneTargets)
            {
                if (potentialCryoZone.Item2.GetComponent<ZoneInfo>().GetQuantityOfEnvironTokensWithTag("Cryogenic") == 0)
                {
                    validCryoZoneTargets.Add(potentialCryoZone);
                }
            }

            // Subtract friendly fire from each target zone's weight
            for (int i = 0; i < validCryoZoneTargets.Count; i++)
            {
                //string cryoZoneTargetsDebugString = "For zone " + validCryoZoneTargets[i].Item2.name;
                foreach (Unit inAreaUnit in validCryoZoneTargets[i].Item2.GetComponentsInChildren<Unit>())
                {
                    //cryoZoneTargetsDebugString += " unit " + inAreaUnit.name;
                    if (inAreaUnit.IsActive() && !inAreaUnit.isHeroAlly && !inAreaUnit.frosty)
                    {
                        //validCryoZoneTargets[i].Item1 -= 5;  // Doesn't work because I think .Item1 is a clone of the value ("return value is not a variable")
                        validCryoZoneTargets[i] = (validCryoZoneTargets[i].Item1 - 9, validCryoZoneTargets[i].Item2);
                        //cryoZoneTargetsDebugString += " sets the weight to " + validCryoZoneTargets[i].Item1.ToString();
                    }
                }
                //Debug.Log(cryoZoneTargetsDebugString);
            }
            validCryoZoneTargets.Sort((x, y) => y.Item1.CompareTo(x.Item1));  // Sorts by doubles in descending order

            //string cryoDebugString = "";
            //foreach ((double, GameObject) validCryoZoneTarget in validCryoZoneTargets)
            //{
            //    cryoDebugString += "cryoZoneTarget: " + validCryoZoneTarget.Item2.name + " worth " + validCryoZoneTarget.Item1 + ",   ";
            //}
            //Debug.Log("!!!ActivateCryogenicDevice of ScenarioMap, cryoDebugString: " + cryoDebugString);

            for (int i = 0; i < validCryoZoneTargets.Count && i < 2; i++)
            {
                GameObject zoneToCryo = validCryoZoneTargets[i].Item2;
                if (i > 0 && zoneToCryo == validCryoZoneTargets[0].Item2)  // Can't cryo the same spot twice
                {
                    if (validCryoZoneTargets.Count > 2 && validCryoZoneTargets[0].Item2 != validCryoZoneTargets[2].Item2)
                    {
                        zoneToCryo = validCryoZoneTargets[2].Item2;
                    }
                    else
                    {
                        break;
                    }
                }
                Vector3 furtherPoint = scenarioMap.animate.GetFurtherPointOnLine(mainCamera.transform.position, zoneToCryo.transform.position);
                yield return scenarioMap.animate.StartCoroutine(scenarioMap.animate.MoveCameraUntilOnscreen(mainCamera.transform.position, furtherPoint));
                yield return new WaitForSecondsRealtime(1);
                yield return scenarioMap.animate.StartCoroutine(zoneToCryo.GetComponent<ZoneInfo>().AddEnvironTokens(new EnvironTokenSave("Cryogenic", 1, false, true)));
                yield return new WaitForSecondsRealtime(2);
            }
            unitZoneInfo.RemoveObjectiveToken("Computer");
            SetActionsWeightTable();
        }
        GameObject.Destroy(successContainer);
        yield return 0;
    }

    /* ActionCallbacks specific to "AFewBadApples" mission */
    public static IEnumerator DeactivateComputer(GameObject unit, GameObject target, int totalSuccesses, int requiredSuccesses)
    {
        ZoneInfo unitZoneInfo = unit.GetComponent<Unit>().GetZone().GetComponent<ZoneInfo>();
        // Animate success vs failure UI
        GameObject successContainer = GameObject.Instantiate(unitZoneInfo.successVsFailurePrefab, scenarioMap.animationContainer.transform);
        successContainer.transform.position = unit.transform.TransformPoint(new Vector3(0, 12, 0));

        successContainer.GetComponent<RectTransform>().sizeDelta = new Vector2(requiredSuccesses * 10, successContainer.GetComponent<RectTransform>().rect.height);
        Transform successContainerText = successContainer.transform.GetChild(0);
        successContainerText.GetComponent<RectTransform>().sizeDelta = new Vector2(requiredSuccesses * 10, successContainerText.GetComponent<RectTransform>().rect.height);
        string successContainerBlanks = "_";
        for (int i = 1; i < requiredSuccesses; i++)
        {
            successContainerBlanks += " _";
        }
        successContainerText.GetComponent<TMP_Text>().text = successContainerBlanks;

        for (int i = -1; i < (totalSuccesses >= 0 ? totalSuccesses : 0); i++)
        {
            yield return new WaitForSecondsRealtime(1);
            if (i == requiredSuccesses - 1)  // Otherwise there can be more results (checks/x's) displayed than requiredSuccesses
            {
                break;
            }
            GameObject successOrFailurePrefab = i + 1 < totalSuccesses ? unitZoneInfo.successPrefab : unitZoneInfo.failurePrefab;
            GameObject successOrFailureMarker = GameObject.Instantiate(successOrFailurePrefab, successContainer.transform);
        }
        yield return new WaitForSecondsRealtime(1);

        if (totalSuccesses >= requiredSuccesses)
        {
            unitZoneInfo.RemoveObjectiveToken("Computer");
            yield return new WaitForSecondsRealtime(2);
        }
        GameObject.Destroy(successContainer);
        yield return 0;
    }

    /* ActionCallbacks specific to "JamAndSeek" mission */
    public static IEnumerator ActivateJammer(GameObject unit, GameObject target, int totalSuccesses, int requiredSuccesses)
    {
        ZoneInfo unitZoneInfo = unit.GetComponent<Unit>().GetZone().GetComponent<ZoneInfo>();
        // Animate success vs failure UI
        GameObject successContainer = GameObject.Instantiate(unitZoneInfo.successVsFailurePrefab, scenarioMap.animationContainer.transform);
        successContainer.transform.position = unit.transform.TransformPoint(new Vector3(0, 12, 0));

        successContainer.GetComponent<RectTransform>().sizeDelta = new Vector2(requiredSuccesses * 10, successContainer.GetComponent<RectTransform>().rect.height);
        Transform successContainerText = successContainer.transform.GetChild(0);
        successContainerText.GetComponent<RectTransform>().sizeDelta = new Vector2(requiredSuccesses * 10, successContainerText.GetComponent<RectTransform>().rect.height);
        string successContainerBlanks = "_";
        for (int i = 1; i < requiredSuccesses; i++)
        {
            successContainerBlanks += " _";
        }
        successContainerText.GetComponent<TMP_Text>().text = successContainerBlanks;

        for (int i = -1; i < (totalSuccesses >= 0 ? totalSuccesses : 0); i++)
        {
            yield return new WaitForSecondsRealtime(1);
            if (i == requiredSuccesses - 1)  // Otherwise there can be more results (checks/x's) displayed than requiredSuccesses
            {
                break;
            }
            GameObject successOrFailurePrefab = i + 1 < totalSuccesses ? unitZoneInfo.successPrefab : unitZoneInfo.failurePrefab;
            GameObject successOrFailureMarker = GameObject.Instantiate(successOrFailurePrefab, successContainer.transform);
        }
        yield return new WaitForSecondsRealtime(1);

        if (totalSuccesses >= requiredSuccesses)
        {
            Vector3 furtherPoint = scenarioMap.animate.GetFurtherPointOnLine(mainCamera.transform.position, unitZoneInfo.transform.position);
            yield return scenarioMap.animate.StartCoroutine(scenarioMap.animate.MoveCameraUntilOnscreen(mainCamera.transform.position, furtherPoint));  // Move camera to zone of bomb being armed
            unitZoneInfo.RemoveObjectiveToken("Jammer");
            unitZoneInfo.AddObjectiveToken("ActiveJammer");
            yield return new WaitForSecondsRealtime(2);
            SetActionsWeightTable();
        }
        GameObject.Destroy(successContainer);
        yield return 0;
    }

    /* ActionCallbacks specific to "RatRace" mission */
    public static IEnumerator SpreadInfection(GameObject unit, GameObject target, int totalSuccesses, int requiredSuccesses)
    {
        ZoneInfo unitZoneInfo = unit.GetComponent<Unit>().GetZone().GetComponent<ZoneInfo>();
        // Animate success vs failure UI
        GameObject successContainer = GameObject.Instantiate(unitZoneInfo.successVsFailurePrefab, scenarioMap.animationContainer.transform);
        successContainer.transform.position = unit.transform.TransformPoint(new Vector3(0, 12, 0));

        successContainer.GetComponent<RectTransform>().sizeDelta = new Vector2(requiredSuccesses * 10, successContainer.GetComponent<RectTransform>().rect.height);
        Transform successContainerText = successContainer.transform.GetChild(0);
        successContainerText.GetComponent<RectTransform>().sizeDelta = new Vector2(requiredSuccesses * 10, successContainerText.GetComponent<RectTransform>().rect.height);
        string successContainerBlanks = "_";
        for (int i = 1; i < requiredSuccesses; i++)
        {
            successContainerBlanks += " _";
        }
        successContainerText.GetComponent<TMP_Text>().text = successContainerBlanks;

        for (int i = -1; i < (totalSuccesses >= 0 ? totalSuccesses : 0); i++)
        {
            yield return new WaitForSecondsRealtime(1);
            if (i == requiredSuccesses - 1)  // Otherwise there can be more results (checks/x's) displayed than requiredSuccesses
            {
                break;
            }
            GameObject successOrFailurePrefab = i + 1 < totalSuccesses ? unitZoneInfo.successPrefab : unitZoneInfo.failurePrefab;
            GameObject successOrFailureMarker = GameObject.Instantiate(successOrFailurePrefab, successContainer.transform);
        }
        yield return new WaitForSecondsRealtime(1);

        if (totalSuccesses >= requiredSuccesses)
        {
            Vector3 furtherPoint = scenarioMap.animate.GetFurtherPointOnLine(mainCamera.transform.position, unitZoneInfo.transform.position);
            //yield return scenarioMap.animate.StartCoroutine(scenarioMap.animate.MoveCameraUntilOnscreen(mainCamera.transform.position, furtherPoint));  // Move camera to zone of bomb being armed
            //unitZoneInfo.RemoveObjectiveToken("Bomb");
            unitZoneInfo.AddObjectiveToken("Rat");
            yield return new WaitForSecondsRealtime(2);
            SetActionsWeightTable();
        }
        GameObject.Destroy(successContainer);
        yield return 0;
    }

    /* ActionCallbacks specific to "JackInTheBomb" mission */
    public static IEnumerator PlaceTrap(GameObject unit, GameObject target, int totalSuccesses, int requiredSuccesses)
    {
        // TODO
        yield return 0;
    }
}
