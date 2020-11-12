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
        else  // Check to make sure it's a valid save file
        {
            ScenarioSave scenarioSave = JsonUtility.FromJson<ScenarioSave>(File.ReadAllText(Application.persistentDataPath + "/" + PlayerPrefs.GetInt(missionName).ToString() + missionName + ".json"));
            if (SceneHandler.version != scenarioSave.version)
            {
                PlayerPrefs.DeleteKey(missionName);
                transform.Find("ContinueButton").gameObject.GetComponent<Button>().interactable = false;
            }
        }
    }

    public void NewGame()
    {
        MissionSpecifics.missionName = missionName;
        //SceneHandler.saveName = missionName + ".json";
        SceneHandler.saveName = null;  // Set to null again in case returning to MissionSelect after completing another mission
        SceneManager.LoadScene(mapName);
    }

    public void ContinueGame()
    {
        if (PlayerPrefs.HasKey(missionName))
        {
            MissionSpecifics.missionName = missionName;
            SceneHandler.saveName = PlayerPrefs.GetInt(missionName).ToString() + missionName + ".json";
            SceneManager.LoadScene(mapName);
        }
        else
        {
            Debug.LogError("ERROR! From MissionSelection.cs, the ContinueGame button should be disabled if there isn't an int stored at PlayerPrefs.GetInt(" + missionName + ").");
        }
    }
}
