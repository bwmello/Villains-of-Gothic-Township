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
        }
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
