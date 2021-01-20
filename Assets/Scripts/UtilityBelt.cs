using System;  // for [Serializable]
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UtilityBelt : MonoBehaviour
{
    public List<GameObject> draggableTools;
    public GameObject smokeToolPouchPrefab;
    public GameObject wallBreakToolPouchPrefab;
    public GameObject gasToolPouchPrefab;
    public GameObject interrogateToolPouchPrefab;

    public List<GameObject> claimableTokensContainers;
    public GameObject claimableTokenContainerPrefab;
    public List<GameObject> claimableTokens;
    public GameObject claimableComputerPrefab;
    public GameObject claimableBriefcasePrefab;
    public GameObject claimableRiflePrefab;
    public GameObject claimableInformationPrefab;


    public void ClearUtilityBelt()
    {
        for (int i = draggableTools.Count - 1; i >= 0; i--)
        {
            DestroyImmediate(draggableTools[i]);
        }
        draggableTools = new List<GameObject>();

        ClearClaimableTokens();

        for (int i = claimableTokensContainers.Count - 1; i >= 0; i--)
        {
            DestroyImmediate(claimableTokensContainers[i]);
        }
        claimableTokensContainers = new List<GameObject>();
    }

    public void ClearClaimableTokens()
    {
        for (int i = claimableTokens.Count - 1; i >= 0; i--)
        {
            DestroyImmediate(claimableTokens[i]);
        }
        claimableTokens = new List<GameObject>();
    }

    public UtilityBeltSave ToJSON()
    {
        return new UtilityBeltSave(this);
    }

    public void LoadUtilityBeltSave(UtilityBeltSave utilityBeltSave)
    {
        ClearUtilityBelt();

        int currentDraggableToolIndex = 0;
        Transform pouchesContainerTransform = transform.Find("Pouches");
        foreach (string draggableToolName in utilityBeltSave.draggableTools)
        {
            switch (draggableToolName)
            {
                case "Smoke":
                    GameObject newSmokeToolPouch = Instantiate(smokeToolPouchPrefab, pouchesContainerTransform);
                    draggableTools.Add(newSmokeToolPouch);
                    break;
                case "WallBreak":
                    GameObject newWallBreakToolPouch = Instantiate(wallBreakToolPouchPrefab, pouchesContainerTransform);
                    draggableTools.Add(newWallBreakToolPouch);
                    break;
                case "Gas":
                    GameObject newGasToolPouch = Instantiate(gasToolPouchPrefab, pouchesContainerTransform);
                    draggableTools.Add(newGasToolPouch);
                    break;
                case "Interrogate":
                    GameObject newInterrogateToolPouch = Instantiate(interrogateToolPouchPrefab, pouchesContainerTransform);
                    draggableTools.Add(newInterrogateToolPouch);
                    break;
                default:
                    Debug.LogError("ERROR! In UtilityBelt.LoadUtilityBeltSave(), unable to pair " + draggableToolName + " with a draggableToolName. This may result in an extra ClaimableTokensContainer.");
                    break;
            }
            currentDraggableToolIndex++;
            if (currentDraggableToolIndex < utilityBeltSave.draggableTools.Count)
            {
                GameObject newClaimableTokensContainer = Instantiate(claimableTokenContainerPrefab, pouchesContainerTransform);
                claimableTokensContainers.Add(newClaimableTokensContainer);
            }
        }

        if (claimableTokensContainers.Count < 1)
        {
            Debug.LogError("ERROR! In UtilityBelt.LoadUtilityBeltSave(), claimableTokensContainers.Count " + claimableTokensContainers.Count + " which means that if there are any claimableTokens, there won't be a container for them.");
        }
        foreach (ClaimableTokenSave claimableTokenSave in utilityBeltSave.claimableTokens)
        {
            GameObject claimableTokenPrefab = null;
            switch (claimableTokenSave.tokenType)
            {
                case "Computer":
                    claimableTokenPrefab = claimableComputerPrefab;
                    break;
                case "Briefcase":
                    claimableTokenPrefab = claimableBriefcasePrefab;
                    break;
                case "SwatRifle":
                    claimableTokenPrefab = claimableRiflePrefab;
                    break;
                case "Information":
                    claimableTokenPrefab = claimableInformationPrefab;
                    break;
            }

            GameObject newClaimableToken = Instantiate(claimableTokenPrefab, claimableTokensContainers[claimableTokens.Count % claimableTokensContainers.Count].transform);
            newClaimableToken.GetComponent<ClaimableToken>().LoadClaimableTokenSave(claimableTokenSave);
            claimableTokens.Add(newClaimableToken);
        }
    }
}

[Serializable]
public class UtilityBeltSave
{
    public List<string> draggableTools = new List<string>();
    public List<ClaimableTokenSave> claimableTokens = new List<ClaimableTokenSave>();
    
    public UtilityBeltSave(UtilityBelt utilityBelt)
    {
        foreach (GameObject draggableTool in utilityBelt.draggableTools)
        {
            draggableTools.Add(draggableTool.GetComponentInChildren<Draggable>().draggableType);
        }
        foreach (GameObject claimableToken in utilityBelt.claimableTokens)
        {
            claimableTokens.Add(claimableToken.GetComponent<ClaimableToken>().ToJSON());
        }
    }
}
