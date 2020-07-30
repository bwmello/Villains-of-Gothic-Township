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
                    GameObject.Instantiate(primedBombPrefab, tokenZone.transform);
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
                    GameObject.Instantiate(bombPrefab, tokenZone.transform);
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
}
