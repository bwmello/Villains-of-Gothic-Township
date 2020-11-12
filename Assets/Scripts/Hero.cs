using System;  // for [Serializable]
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;  // For button
using TMPro;  // for getting hero number

public class Hero : MonoBehaviour
{
    public string heroName;  // One of three tags: "1stHero", "2ndHero", "3rdHero"
    private float fadedAlpha = .5f;

    public int moveSpeed = 4;
    public int ignoreTerrainDifficulty = 1;
    public int ignoreElevation = 1;
    public int ignoreSize = 1;
    //public int wallBreaker = 0;  // wall breaking items are single use, making tracking this pointless
    //public int woundsReceived = 0;
    //public bool canCounterMeleeAttacks = false;
    //public bool canCounterRangedAttacks = false;


    public void InitializeHero(string newHeroName)
    {
        heroName = newHeroName;
        transform.tag = heroName;
        transform.Find("NumButtonText").GetComponent<TMP_Text>().text = heroName.Substring(0, 1);
        transform.Find("AlphaButtonText").GetComponent<TMP_Text>().text = heroName.Substring(1, 2);
    }

    public bool IsWoundedOut()
    {
        if (gameObject.GetComponent<CanvasGroup>().alpha < 1f)
        {
            return true;
        }
        return false;
    }

    public bool isClickable = false;
    public void SetIsClickable(bool shouldMakeClickable)
    {
        gameObject.GetComponent<Button>().enabled = shouldMakeClickable;
        isClickable = shouldMakeClickable;
    }

    public bool isDraggable = false;
    public void SetIsDraggable(bool shouldMakeDraggable)
    {
        gameObject.GetComponent<Draggable>().isDraggable = shouldMakeDraggable;
        isDraggable = shouldMakeDraggable;
    }

    public void ConfigureClickAndDragability()
    {
        switch (MissionSpecifics.currentPhase)
        {
            case "Hero":
                SetIsClickable(true);
                SetIsDraggable(true);
                break;
            case "Villain":
                SetIsClickable(false);
                SetIsDraggable(false);
                break;
        }
    }

    public void HeroButtonClicked(Button button)
    {
        if (!gameObject.GetComponent<Draggable>().isDragging)
        {
            CanvasGroup buttonCanvas = button.GetComponent<CanvasGroup>();
            if (!IsWoundedOut())  // Hero is wounded out  // TODO Bring back at start of next round as some wound cubes are moved to fatigue zone.
            {
                buttonCanvas.alpha = fadedAlpha;
            }
            else  // Mistake was made in marking hero as wounded out so bring back
            {
                buttonCanvas.alpha = 1;
            }
        }

    }

    public void RestReset()  // At ScenarioMap.StartHeroTurn(), bring back wounded out heroes because they've rested
    {
        if (IsWoundedOut())
        {
            gameObject.GetComponent<CanvasGroup>().alpha = 1;
        }
    }

    public GameObject GetZone()
    {
        return transform.parent.parent.parent.gameObject;  // Grabs ZoneInfoPanel instead of HeroesRow. If changes in future, only need to change this function.
    }

    public List<GameObject> GetPlaceableAllyZones(int placeableSize)
    {
        List<GameObject> placeableAllyZones = new List<GameObject>();
        ZoneInfo heroZoneInfo = GetZone().GetComponent<ZoneInfo>();
        if (heroZoneInfo.GetCurrentOccupancy() + placeableSize <= heroZoneInfo.maxOccupancy && heroZoneInfo.GetAvailableUnitSlot())
        {
            placeableAllyZones.Add(heroZoneInfo.gameObject);
        }
        else
        {
            placeableAllyZones.AddRange(heroZoneInfo.adjacentZones);
            placeableAllyZones.AddRange(heroZoneInfo.steeplyAdjacentZones);
        }
        return placeableAllyZones;
    }

    public HeroSave ToJSON()
    {
        return new HeroSave(this);
    }
}

[Serializable]
public class HeroSave
{
    public string heroName;
    public int zoneID;

    public HeroSave(Hero hero)
    {
        heroName = hero.heroName;
        zoneID = hero.GetZone().GetComponent<ZoneInfo>().id;
    }
}