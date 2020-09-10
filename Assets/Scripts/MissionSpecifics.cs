using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;  // For button
using TMPro;  // for TMP_Text to edit SuccessVsFailure's successContainer blanks


public static class MissionSpecifics
{
    public static string missionName;
    public static GameObject bombPrefab;
    public static GameObject primedBombPrefab;
    public static GameObject mainCamera;
    public static GameObject animationContainer;
    public static Animate animate;
    public static ScenarioMap scenarioMap;

    public static Dictionary<string, List<ActionWeight>> actionsWeightTable = new Dictionary<string, List<ActionWeight>>() {
        { "MELEE", new List<ActionWeight>() { new ActionWeight(null, 0, 10, null) } },  // 10 * averageTotalWounds
        { "RANGED", new List<ActionWeight>() { new ActionWeight(null, 0, 10, null) } }
    };

    public delegate IEnumerator ActionCallback(GameObject unit, GameObject target, int totalSuccesses, int requiredSuccesses);
    public struct ActionWeight
    {
        public string targetType;
        public int requiredSuccesses;
        public double weightFactor;  // Might be per wound or * chanceOfSuccess
        public ActionCallback actionCallback;
        public bool activateLast;

        public ActionWeight(string newTargetType, int newRequiredSuccesses, double newWeightFactor, ActionCallback newActionCallback, bool newActivateLast = false)
        {
            targetType = newTargetType;
            requiredSuccesses = newRequiredSuccesses;
            weightFactor = newWeightFactor;
            actionCallback = newActionCallback;
            activateLast = newActivateLast;
        }
    }


    public static void SetActionsWeightTable()
    {
        int totalBombs;
        int totalPrimedBombs;
        int totalComputers;
        switch (missionName)  // Each nonconstant key (ex: "THOUGHT") should be wiped each time and only added back in if conditions are still met
        {
            case "ASinkingFeeling":
                totalBombs = GetTotalActiveTokens(new List<string>() { "Bomb" });
                totalPrimedBombs = GetTotalActiveTokens(new List<string>() { "PrimedBomb" });
                totalComputers = GetTotalActiveTokens(new List<string>() { "Computer" });

                actionsWeightTable["MANIPULATION"] = new List<ActionWeight>();
                if (totalBombs > 0)
                {
                    actionsWeightTable["MANIPULATION"].Add(new ActionWeight("Bomb", 3, 100, PrimeBombManually));  // 100 * chanceOfSuccess, 
                }

                actionsWeightTable["THOUGHT"] = new List<ActionWeight>();
                if (totalComputers > 0 && totalBombs > 0)
                {
                    actionsWeightTable["THOUGHT"].Add(new ActionWeight("Computer", 3, 100, PrimeBombRemotely));
                }

                actionsWeightTable["GUARD"] = new List<ActionWeight>();
                if (totalPrimedBombs > 0)
                {
                    actionsWeightTable["GUARD"].Add(new ActionWeight("PrimedBomb", 0, 20, null));  // Flat weight bonus for hindering heroes attempting to disable objectives
                }
                if (totalBombs > 0)
                {
                    actionsWeightTable["GUARD"].Add(new ActionWeight("Bomb", 0, 15, null));
                    //if (totalComputers > 0)  // Heroes should always be going after bombs, not computers
                    //{
                    //    actionsWeightTable["GUARD"].Add(new ActionWeight("Computer", 0, 5, null));
                    //}
                }
                break;
            case "IceToSeeYou":
                totalBombs = GetTotalActiveTokens(new List<string>() { "Bomb" });
                totalComputers = GetTotalActiveTokens(new List<string>() { "Computer" });

                actionsWeightTable["MANIPULATION"] = new List<ActionWeight>() { new ActionWeight("Grenade", 0, 15, null) };  // 15 * averageAutoWounds * chanceOfSuccess - friendlyFire

                actionsWeightTable["THOUGHT"] = new List<ActionWeight>();
                if (totalComputers > 0)
                {
                    actionsWeightTable["THOUGHT"].Add(new ActionWeight("Computer", 3, 80, ActivateCryogenicDevice, true));  // Assume you cost the hero at least 1 movepoint and auto deal 2/3 of a wound per cryogenic token, 0 weight if would hit anyone except frosty
                }

                actionsWeightTable["GUARD"] = new List<ActionWeight>();
                if (totalBombs > 0)
                {
                    actionsWeightTable["GUARD"].Add(new ActionWeight("Bomb", 0, 20, null));
                }
                break;
        }
    }

    public static int GetFinalRound()
    {
        switch (missionName)  // Each nonconstant key (ex: "THOUGHT") should be wiped each time and only added back in if conditions are still met
        {
            case "ASinkingFeeling":
                return 7;
            case "IceToSeeYou":
                return 8;
        }
        return -1;
    }

    public static double GetHeroProximityToObjectiveWeightMultiplier(GameObject zone, bool isPartialMove = false)
    {
        double weightMultiplier = .1;  // Default if no heroes within 4 moves
        double[] weightBasedOnHeroProximity;
        if (!isPartialMove)
        {
            weightBasedOnHeroProximity = new double[] { 1, .9, .75, .5, .25 };
        }
        else
        {
            weightBasedOnHeroProximity = new double[] { .25, .5, .75, .9, 1 };
        }

        if (UnitIntel.heroMovesRequiredToReachZone.ContainsKey(zone))  // If heroes within 4 moves
        {
            if (UnitIntel.heroMovesRequiredToReachZone[zone].Count > 0)
            {
                weightMultiplier = weightBasedOnHeroProximity[UnitIntel.heroMovesRequiredToReachZone[zone][0]];  // Disregards any heroes beyond or equal to closest hero
            }
        }

        return weightMultiplier;
    }

    public static void ObjectiveTokenClicked(Button button)
    {
        if (button.CompareTag("Bomb"))
        {
            switch (missionName)
            {
                case "IceToSeeYou":
                    GameObject tokenZone = button.gameObject.GetComponent<Token>().GetZone();
                    Object.DestroyImmediate(button.gameObject);
                    Object.Instantiate(primedBombPrefab, tokenZone.transform.Find("TokensRow"));
                    tokenZone.GetComponent<ZoneInfo>().ReorganizeTokens();
                    return;
                default:
                    break;
            }
        }
        if (button.CompareTag("PrimedBomb"))
        {
            switch (missionName)
            {
                case "IceToSeeYou":
                    GameObject tokenZone = button.gameObject.GetComponent<Token>().GetZone();
                    Object.DestroyImmediate(button.gameObject);
                    Object.Instantiate(bombPrefab, tokenZone.transform.Find("TokensRow"));
                    tokenZone.GetComponent<ZoneInfo>().ReorganizeTokens();
                    return;
                default:
                    button.transform.Find("RedLight").gameObject.SetActive(false);
                    break;
            }
        }
        CanvasGroup buttonCanvas = button.GetComponent<CanvasGroup>();
        if (buttonCanvas.alpha == 1)  // Token was disabled, so remove from board
        {
            buttonCanvas.alpha = (float).2;
        }
        else  // Mistake was made in removing token, so add token back to the board
        {
            buttonCanvas.alpha = (float)1;
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
            default:
                break;
        }
        return false;
    }

    public static bool IsHeroVictory()
    {
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
                        animate.ShowLoopingExplosion(primedBomb.transform.position);
                    }
                }
                break;
            case "IceToSeeYou":
                if (IsHeroVictory())
                {
                    foreach (GameObject primedBomb in GetActiveTokens(new List<string>() { "PrimedBomb" }))
                    {
                        animate.ShowLoopingExplosion(primedBomb.transform.position);
                    }
                }
                break;
            default:
                break;
        }
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
        }
        return weight;
    }

    public static IEnumerator ActivateReinforcement()
    {
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
                            Unit superBarnInfo = Object.Instantiate(superbarnPrefab, barn.transform.parent).GetComponent<Unit>();
                            superBarnInfo.GetComponent<Unit>().ModifyLifePoints(barnInfo.lifePoints - barnInfo.lifePointsMax);  // Do not reset SuperBarn's life points to 6 if Barn was damaged.
                            Object.DestroyImmediate(barn);
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
                    yield return animate.StartCoroutine(superBarnInfo.ActivateUnit());
                    superBarnInfo.ModifyLifePoints(-2);
                    if (superBarnInfo.lifePoints < 1)
                    {
                        Object.Destroy(superBarn);
                    }
                }
                break;
        }
        yield return 0;
    }

    /* ActionCallbacks specific to "ASinkingFeeling" mission */
    public static IEnumerator PrimeBombManually(GameObject unit, GameObject target, int totalSuccesses, int requiredSuccesses)
    {
        ZoneInfo unitZoneInfo = unit.GetComponent<Unit>().GetZone().GetComponent<ZoneInfo>();
        // Animate success vs failure UI
        GameObject successContainer = Object.Instantiate(unitZoneInfo.successVsFailurePrefab, animationContainer.transform);
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
            GameObject successOrFailureMarker = Object.Instantiate(successOrFailurePrefab, successContainer.transform);
        }
        yield return new WaitForSecondsRealtime(1);

        if (totalSuccesses >= requiredSuccesses)
        {
            yield return animate.StartCoroutine(animate.MoveCameraUntilOnscreen(mainCamera.transform.position, unitZoneInfo.GetBomb().transform.position));  // Move camera to bomb being armed
            unitZoneInfo.PrimeBomb();
            yield return new WaitForSecondsRealtime(2);
            SetActionsWeightTable();
        }
        Object.Destroy(successContainer);
        yield return 0;
    }

    public static IEnumerator PrimeBombRemotely(GameObject unit, GameObject target, int totalSuccesses, int requiredSuccesses)
    {
        ZoneInfo unitZoneInfo = unit.GetComponent<Unit>().GetZone().GetComponent<ZoneInfo>();
        // Animate success vs failure UI
        GameObject successContainer = Object.Instantiate(unitZoneInfo.successVsFailurePrefab, animationContainer.transform);
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
            GameObject successOrFailureMarker = Object.Instantiate(successOrFailurePrefab, successContainer.transform);
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
                unitZoneInfo.RemoveComputer();
                yield return animate.StartCoroutine(animate.MoveCameraUntilOnscreen(mainCamera.transform.position, chosenBombZone.GetBomb().transform.position));  // Move camera to bomb being armed
                chosenBombZone.PrimeBomb();
                yield return new WaitForSecondsRealtime(2);
                //unitTurn.targetedZone = chosenBombZone.transform.gameObject;  // Only useful for DEBUG statement at end of PerformAction()
            }
            else
            {
                Debug.LogError("ERROR! Tried to use a computer from " + unitZoneInfo.name + " to prime a bomb, but no zones with bombs available. Why was computer able to be used if there are no zones with bombs?");
            }
            SetActionsWeightTable();
        }
        Object.Destroy(successContainer);
        yield return 0;
    }

    /* ActionCallbacks specific to "IceToSeeYou" mission */
    public static IEnumerator ActivateCryogenicDevice(GameObject unit, GameObject target, int totalSuccesses, int requiredSuccesses)
    {
        ZoneInfo unitZoneInfo = unit.GetComponent<Unit>().GetZone().GetComponent<ZoneInfo>();
        // Animate success vs failure UI
        GameObject successContainer = Object.Instantiate(unitZoneInfo.successVsFailurePrefab, animationContainer.transform);
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
            GameObject successOrFailureMarker = Object.Instantiate(successOrFailurePrefab, successContainer.transform);
        }
        yield return new WaitForSecondsRealtime(1);

        if (totalSuccesses >= requiredSuccesses)
        {
            List<(double, GameObject)> cryoZoneTargets = new List<(double, GameObject)>();

            GameObject hero = GameObject.FindGameObjectWithTag("1stHero");
            GameObject heroZone = hero.GetComponent<Hero>().GetZone();
            cryoZoneTargets.Add((30, heroZone));
            foreach (GameObject bomb in GameObject.FindGameObjectsWithTag("Bomb"))
            {
                GameObject bombZone = bomb.transform.parent.parent.gameObject;
                cryoZoneTargets.Add((15, bombZone));
                ZoneInfo bombZoneInfo = bombZone.GetComponent<ZoneInfo>();
                if ((bombZoneInfo.adjacentZones.Count + bombZoneInfo.steeplyAdjacentZones.Count) == 1)  // If only one way in or out (ignoring walls)
                {
                    if (bombZoneInfo.adjacentZones.Count == 1)
                    {
                        cryoZoneTargets.Add((20, bombZoneInfo.adjacentZones[0]));  // TODO Once hero has broken through any wall, adjust this
                    }
                    else
                    {
                        cryoZoneTargets.Add((20, bombZoneInfo.steeplyAdjacentZones[0]));
                    }
                }
            }
            for (int i = 0; i < cryoZoneTargets.Count - 1; i++)  // Subtract friendly fire from each target zone's weight
            {
                foreach (Unit inAreaUnit in cryoZoneTargets[i].Item2.GetComponent<ZoneInfo>().GetUnitsInfo())
                {
                    if (!inAreaUnit.frosty)
                    {
                        //cryoZoneTargets[i].Item1 -= 5;  // Doesn't work because I think .Item1 is a clone of the value ("return value is not a variable")
                        cryoZoneTargets[i] = (cryoZoneTargets[i].Item1 - 9, cryoZoneTargets[i].Item2);
                    }
                }
            }
            cryoZoneTargets.Sort((x, y) => y.Item1.CompareTo(x.Item1));  // Sorts by doubles in descending order

            //string cryoDebugString = "";
            //foreach ((double, GameObject) cryoZoneTarget in cryoZoneTargets)
            //{
            //    cryoDebugString += "cryoZoneTarget: " + cryoZoneTarget.Item2.name + " worth " + cryoZoneTarget.Item1 + ",   ";
            //}
            //Debug.Log("!!!ActivateCryogenicDevice of ScenarioMap, cryoDebugString: " + cryoDebugString);

            for (int i = 0; i < cryoZoneTargets.Count - 1 && i < 2; i++)
            {
                GameObject zoneToCryo = cryoZoneTargets[i].Item2;
                yield return animate.StartCoroutine(animate.MoveCameraUntilOnscreen(mainCamera.transform.position, zoneToCryo.transform.position));
                yield return new WaitForSecondsRealtime(1);
                yield return animate.StartCoroutine(zoneToCryo.GetComponent<ZoneInfo>().AddEnvironTokens(new EnvironTokenSave("Cryogenic", 1, false, true)));
                yield return new WaitForSecondsRealtime(2);
            }
            unitZoneInfo.RemoveComputer();
            SetActionsWeightTable();
        }
        Object.Destroy(successContainer);
        yield return 0;
    }
}
