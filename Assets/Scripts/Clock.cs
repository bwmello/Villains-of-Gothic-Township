using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Clock : MonoBehaviour
{
    public GameObject scenarioMap;

    void OnMouseDown()  // Necessary to determine when polygon collider is clicked and not the image itself.
    {
        scenarioMap.GetComponent<ScenarioMap>().GoBackATurn();
    }
}
