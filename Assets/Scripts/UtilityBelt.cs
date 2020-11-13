using System;  // for [Serializable]
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UtilityBelt : MonoBehaviour
{
    public List<GameObject> claimableTokens;
    public GameObject claimableComputerPrefab;
    public GameObject claimableBriefcasePrefab;
    public GameObject claimableRiflePrefab;
    public GameObject leftTokensContainer;
    public GameObject rightTokensContainer;


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
        ClearClaimableTokens();
        GameObject lastTokensContainerUsed = rightTokensContainer;  // Pretty terrible way to do this, but only temporary as will need to be changed when more than 3 utilitybelt pouches.
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
            }
            GameObject claimableToken;
            if (lastTokensContainerUsed == rightTokensContainer)
            {
                claimableToken = Instantiate(claimableTokenPrefab, leftTokensContainer.transform);
                lastTokensContainerUsed = leftTokensContainer;
            }
            else
            {
                claimableToken = Instantiate(claimableTokenPrefab, rightTokensContainer.transform);
                lastTokensContainerUsed = rightTokensContainer;
            }
            claimableToken.GetComponent<ClaimableToken>().LoadClaimableTokenSave(claimableTokenSave);
            claimableTokens.Add(claimableToken);
        }
        // Below is alternative for adding ClaimableTokens to the UtilityBelt without the left/rightTokensContainers
        //Transform pouchesContainer = transform.Find("Pouches");
        //int availableSlots = pouchesContainer.childCount - 1;
        //for (int i = 0; i < utilityBeltSave.claimableTokens.Count; i++)
        //{
        //    ClaimableTokenSave claimableTokenSave = utilityBeltSave.claimableTokens[i];
        //    GameObject claimableTokenPrefab = null;
        //    switch (claimableTokenSave.tokenType)
        //    {
        //        case "Computer":
        //            claimableTokenPrefab = claimableComputerPrefab;
        //            break;
        //        case "Briefcase":
        //            claimableTokenPrefab = claimableBriefcasePrefab;
        //            break;
        //        case "SwatRifle":
        //            claimableTokenPrefab = claimableRiflePrefab;
        //            break;
        //    }

        //    GameObject claimableToken = Instantiate(claimableTokenPrefab, pouchesContainer);
        //    //claimableToken.transform.SetSiblingIndex(i + 2 - (i+1)%availableSlots);  //0:1, 1:3, 2:2, 3:5   3 pouches: 1,3,2,5  4 pouches: 1,3,5,2,5,8   OR  3 pouches: 1,3,1,4
        //    switch (i)   // Should be replaced with equation above for variable number of pouches/availableSlots
        //    {
        //        case 0:
        //            claimableToken.transform.SetSiblingIndex(1);
        //            break;
        //        case 1:
        //            claimableToken.transform.SetSiblingIndex(3);
        //            break;
        //        case 2:
        //            claimableToken.transform.SetSiblingIndex(2);
        //            break;
        //        case 3:
        //            claimableToken.transform.SetSiblingIndex(5);
        //            break;
        //        case 4:
        //            claimableToken.transform.SetSiblingIndex(3);
        //            break;
        //        case 5:
        //            claimableToken.transform.SetSiblingIndex(7);
        //            break;
        //    }
        //    claimableToken.transform.localPosition = new Vector3(claimableToken.transform.localPosition.x, claimableToken.transform.localPosition.y - 30, claimableToken.transform.localPosition.z);
        //    claimableToken.GetComponent<ClaimableToken>().LoadClaimableTokenSave(claimableTokenSave);
        //    claimableTokens.Add(claimableToken);
        //}
    }
}

[Serializable]
public class UtilityBeltSave
{
    public List<ClaimableTokenSave> claimableTokens = new List<ClaimableTokenSave>();

    public UtilityBeltSave(UtilityBelt utilityBelt)
    {
        foreach (GameObject claimableToken in utilityBelt.claimableTokens)
        {
            claimableTokens.Add(claimableToken.GetComponent<ClaimableToken>().ToJSON());
        }
    }
}
