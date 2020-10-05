using System;  // For Serializable
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;  // For button

public class WallRubble : MonoBehaviour
{
    public GameObject zone1, zone2;

    public Boolean WallIsBroken()
    {
        CanvasGroup transparencyCanvas = this.GetComponent<CanvasGroup>();
        return (transparencyCanvas.alpha == 1);
    }

    public void WallRubblePlaced()
    {
        if (WallIsBroken())  // Mistake was made in breaking wall, so deactivate WallRubble
        {
            RebuildWall();
        }
        else  // Breaking wall, so activate WallRubble
        {
            BreakWall();
        }
    }

    public void EnableDropZone()
    {
        if (!WallIsBroken())
        {
            this.GetComponent<CanvasGroup>().alpha = (float).2;
        }
        transform.Find("DropZone").gameObject.SetActive(true);
    }

    public void DisableDropZone()
    {
        if (!WallIsBroken())
        {
            this.GetComponent<CanvasGroup>().alpha = 0;
        }
        transform.Find("DropZone").gameObject.SetActive(false);
    }

    public void BreakWall()
    {
        ZoneInfo zone1Info = zone1.GetComponent<ZoneInfo>();
        ZoneInfo zone2Info = zone2.GetComponent<ZoneInfo>();
        zone1Info.adjacentZones.Add(zone2);
        zone2Info.adjacentZones.Add(zone1);
        zone1Info.lineOfSightZones.Add(zone2);
        zone2Info.lineOfSightZones.Add(zone1);

        CanvasGroup transparencyCanvas = this.GetComponent<CanvasGroup>();
        transparencyCanvas.alpha = (float)1;
    }

    public void RebuildWall()
    {
        ZoneInfo zone1Info = zone1.GetComponent<ZoneInfo>();
        ZoneInfo zone2Info = zone2.GetComponent<ZoneInfo>();
        zone1Info.adjacentZones.Remove(zone2);
        zone2Info.adjacentZones.Remove(zone1);
        zone1Info.lineOfSightZones.Remove(zone2);
        zone2Info.lineOfSightZones.Remove(zone1);

        CanvasGroup transparencyCanvas = this.GetComponent<CanvasGroup>();
        transparencyCanvas.alpha = 0;
    }
}
