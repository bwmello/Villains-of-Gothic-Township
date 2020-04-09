using System;  // For Serializable
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallRubble : MonoBehaviour
{
    public GameObject zone1, zone2;

    public void Initialize(GameObject newZone1, GameObject newZone2)
    {
        zone1 = newZone1;
        zone2 = newZone2;

        ZoneInfo zone1Info = zone1.GetComponent<ZoneInfo>();
        ZoneInfo zone2Info = zone2.GetComponent<ZoneInfo>();
        zone1Info.adjacentZones.Add(zone2);
        zone2Info.adjacentZones.Add(zone1);
        zone1Info.lineOfSightZones.Add(zone2);
        zone2Info.lineOfSightZones.Add(zone1);

        transform.position = new Vector3((zone1.transform.position.x + zone2.transform.position.x) / 2, (zone1.transform.position.y + zone2.transform.position.y) / 2, 0);
    }

    public void RemoveRubbleAndRebuildWall()
    {
        ZoneInfo zone1Info = zone1.GetComponent<ZoneInfo>();
        ZoneInfo zone2Info = zone2.GetComponent<ZoneInfo>();
        zone1Info.adjacentZones.Remove(zone2);
        zone2Info.adjacentZones.Remove(zone1);
        zone1Info.lineOfSightZones.Remove(zone2);
        zone2Info.lineOfSightZones.Remove(zone1);

        Destroy(transform.gameObject);
    }

    public WallRubbleSave ToJSON()
    {
        return new WallRubbleSave(this);
    }
}

[Serializable]
public class WallRubbleSave
{
    public string zone1, zone2;

    public WallRubbleSave(WallRubble wallRubble)
    {
        zone1 = wallRubble.zone1.name;
        zone2 = wallRubble.zone2.name;
    }
}