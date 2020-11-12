using System;  // for [Serializable]
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;  // For button

public class ClaimableToken : MonoBehaviour
{
    public string tokenType;
    public bool isClaimed = false;


    public void ClaimableTokenClicked()
    {
        isClaimed = !isClaimed;
        SetTokenTransparency();
    }

    public void SetTokenTransparency()
    {
        if (isClaimed)
        {
            gameObject.GetComponent<CanvasGroup>().alpha = 1;
        }
        else
        {
            gameObject.GetComponent<CanvasGroup>().alpha = .2f;
        }
    }

    public void LoadClaimableTokenSave(ClaimableTokenSave claimableTokenSave)
    {
        tokenType = claimableTokenSave.tokenType;
        isClaimed = claimableTokenSave.isClaimed;
        SetTokenTransparency();
    }

    public ClaimableTokenSave ToJSON()
    {
        return new ClaimableTokenSave(this);
    }
}

[Serializable]
public class ClaimableTokenSave
{
    public string tokenType;
    public bool isClaimed = false;

    public ClaimableTokenSave(ClaimableToken claimableToken)
    {
        tokenType = claimableToken.tokenType;
        isClaimed = claimableToken.isClaimed;
    }
}
