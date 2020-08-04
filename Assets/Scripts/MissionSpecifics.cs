using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;  // For button

public static class MissionSpecifics
{
    public static string missionName;
    public static GameObject bombPrefab;
    public static GameObject primedBombPrefab;

    public static void ObjectiveTokenClicked(Button button)
    {
        if (button.CompareTag("Bomb"))
        {
            switch (missionName)
            {
                case "IceToSeeYou":
                    GameObject tokenZone = button.gameObject.GetComponent<Token>().GetZone();
                    GameObject.DestroyImmediate(button.gameObject);
                    GameObject.Instantiate(primedBombPrefab, tokenZone.transform.Find("TokensRow"));
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
                    GameObject.DestroyImmediate(button.gameObject);
                    GameObject.Instantiate(bombPrefab, tokenZone.transform.Find("TokensRow"));
                    tokenZone.GetComponent<ZoneInfo>().ReorganizeTokens();
                    return;
                default:
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

    public static int GetTotalActiveTokens(List<string> tokenTags)
    {
        int totalActiveTokens = 0;
        foreach (string tokenTag in tokenTags)
        {
            GameObject[] activeAndInactiveTokens = GameObject.FindGameObjectsWithTag(tokenTag);
            foreach (GameObject maybeActiveToken in activeAndInactiveTokens)
            {
                if (maybeActiveToken.GetComponent<Token>().IsActive())
                {
                    totalActiveTokens++;
                }
            }
        }
        return totalActiveTokens;
    }

    public static bool IsGameOver(int currentRound)
    {
        switch (missionName)
        {
            case "ASinkingFeeling":
                int totalBombsRemaining = GetTotalActiveTokens(new List<string>() { "Bomb", "PrimedBomb" });
                if (currentRound >= 7 || totalBombsRemaining < 2)  // end of hero turn 7 or 4 of 5 bombs are neutralized
                {
                    return true;
                }
                break;
            case "IceToSeeYou":
                int totalPrimedBombs = GetTotalActiveTokens(new List<string>() { "PrimedBomb" });
                if (currentRound >= 8 || totalPrimedBombs >= 3)  // end of hero turn 8 or 3 bombs are primed
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
                int totalPrimedBombsRemaining = GameObject.FindGameObjectsWithTag("PrimedBomb").Length;
                if (totalPrimedBombsRemaining < 2)
                {
                    return true;
                }
                break;
            case "IceToSeeYou":
                int totalPrimedBombs = GameObject.FindGameObjectsWithTag("PrimedBomb").Length;
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
}
