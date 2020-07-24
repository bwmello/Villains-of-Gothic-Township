using System.Collections;
using System.Collections.Generic;
using System.IO;  // For File.ReadAllText for loading json save files
using UnityEngine;
using UnityEngine.UI;  // For button
using UnityEngine.SceneManagement;  // For SceneManager for switching scenes

public class MissionSelection : MonoBehaviour
{
    public string missionName;
    public string mapName;

    private void Start()
    {
        if (!PlayerPrefs.HasKey(missionName))
        {
            transform.Find("ContinueButton").gameObject.GetComponent<Button>().interactable = false;
        }
    }

    public void NewGame()
    {
        SceneHandler.saveName = missionName + ".json";
        SceneManager.LoadScene(mapName);
    }

    public void ContinueGame()
    {
        if (PlayerPrefs.HasKey(missionName))
        {
            SceneHandler.saveName = PlayerPrefs.GetInt(missionName).ToString() + missionName + ".json";
            SceneManager.LoadScene(mapName);
        }
        else
        {
            Debug.LogError("ERROR! From MissionSelection.cs, the ContinueGame button should be disabled if there isn't an int stored at PlayerPrefs.GetInt(" + missionName + ").");
        }
    }
}
