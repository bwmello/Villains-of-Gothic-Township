using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;  // for getting henchmen quantity from UnitRows

public class ZoneInfo : MonoBehaviour
{
    public List<GameObject> adjacentZones;
    public List<GameObject> steeplyAdjacentZones;
    public List<GameObject> lineOfSightZones;
    public GameObject elevationDie;  // Determines fall damage and bonus for ranged attacks made from a higher elevation
    public int elevation;
    public int maxOccupancy;
    public int terrainDifficulty = 0;
    public int terrainDanger = 0;
    public int supportRerolls = 0;  // Determined by total Unit.supportRerolls of each unit in zone

    private void Start()
    {
        List<string>  unitTags = transform.GetComponentInParent<ScenarioMap>().villainRiver;
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
                currentOccupancy += int.Parse(row.Find("UnitNumber").GetComponent<TMP_Text>().text);
            }
        }
        return currentOccupancy;
    }

    public int GetCurrentHindrance(bool isMoving, GameObject unitToDiscount)
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
                int quantity = 1;
                if (!row.CompareTag("BARN") && !row.CompareTag("SUPERBARN"))
                {
                    quantity = int.Parse(row.Find("UnitNumber").GetComponent<TMP_Text>().text);
                }
                Unit unitInfo = row.gameObject.GetComponent<Unit>();
                currentHindrance -= quantity * (isMoving ? unitInfo.size : unitInfo.menace);
            }
        }
        if (currentHindrance < 0)
        {
            currentHindrance = 0;
        }
        return currentHindrance;
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
