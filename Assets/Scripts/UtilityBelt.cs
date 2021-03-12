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
    public GameObject claimableGasCapsulePrefab;


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

    public void ConfigureToolAndTokenInteractivity()
    {
        bool isInteractive = false;
        switch (MissionSpecifics.currentPhase)
        {
            //case "Setup":
            case "Hero":
                isInteractive = true;
                break;
            case "HeroAnimation":
            case "Villain":
                isInteractive = false;
                break;
        }
        foreach (GameObject draggableTool in draggableTools)
        {
            Draggable draggability = draggableTool.GetComponentInChildren<Draggable>();  // draggableTools holds the tool pouch themselves, while the draggable is on the tool icon
            if (draggability)  // Necessarry because if tool was currently being dragged, it's in the UIAnimationContainer instead of the draggableTool/pouch
            {
                draggability.isDraggable = isInteractive;
            }
            CanvasGroup alphaToFade = draggableTool.GetComponentInChildren<CanvasGroup>();
            if (alphaToFade)
            {
                alphaToFade.alpha = isInteractive ? 1f : .6f;
            }
            //draggableTool.GetComponentInChildren<Draggable>().isDraggable = isInteractive;  // draggableTools holds the tool pouch themselves, while the draggable is on the tool icon
            //draggableTool.GetComponentInChildren<CanvasGroup>().alpha = isInteractive ? 1f : .6f;
        }
        foreach (GameObject claimableToken in claimableTokens)
        {
            claimableToken.GetComponent<ClaimableToken>().SetIsClickable(isInteractive);  // Could also just call claimableToken.GetComponent<ClaimableToken>().ConfigureClickability()
        }
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
                case "GasCapsule":
                    claimableTokenPrefab = claimableGasCapsulePrefab;
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
