﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;  // for getting henchmen quantity from UnitRows

public class ZoneInfo : MonoBehaviour
{
    public List<GameObject> adjacentZones;
    public List<GameObject> steeplyAdjacentZones;
    public List<GameObject> lineOfSightZones;  // Use elevation difference between both zones when determining if height bonus
    public int elevation;
    public int maxOccupancy;
    public int terrainDifficulty = 0;
    public int terrainDanger = 0;

    public int GetCurrentOccupancy()
    {
        int currentOccupancy = 0;
        currentOccupancy += GetHeroesCount();
        foreach (Transform row in transform)
        {
            if (row.name != "TokensRow" && row.name != "HeroesRow" && row.name != "ZoneDebugger" && row.gameObject.activeSelf)
            {
                currentOccupancy += int.Parse(row.Find("UnitNumber").GetComponent<TMP_Text>().text);
            }
        }
        return currentOccupancy;
    }

    public bool HasToken(string tokenName)
    {
        Transform tokensRow = transform.Find("TokensRow");
        return (tokensRow.gameObject.activeSelf && tokensRow.Find(tokenName) != null && tokensRow.Find(tokenName).gameObject.activeSelf);
    }

    public bool HasHeroes()
    {
        return GetHeroesCount() > 0 ? true : false;
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
        //Debug.Log("TokenButtonClicked!!! button.name: " + button.name);
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

    //When receive end villain (and maybe start villain) turn signal, deactivate (hiding) any rows with 0 henchmen/life points
    // could also set GetHeroesCount() to var so function isn't run so many times
}
