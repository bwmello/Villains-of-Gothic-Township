using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Clock : MonoBehaviour
{
    public GameObject scenarioMap;

    void OnMouseUpAsButton()  // Necessary to determine when polygon collider is clicked and not the image itself.
    {
        ScenarioMap scenarioMapInfo = scenarioMap.GetComponent<ScenarioMap>();
        if (scenarioMapInfo.isPlayerUIEnabled)
        {
            scenarioMapInfo.EndHeroTurn();
        }
    }
}
