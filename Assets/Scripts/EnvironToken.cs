using System;  // for [Serializable]
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;  // For changing QuantityText

public class EnvironToken : MonoBehaviour
{
    public int quantity = 1;
    public bool dissipatesHeroTurn = false;
    public bool dissipatesVillainTurn = false;
    public bool partiallyDissipated = false;

    void Awake()
    {  // Needed so that when Instantiated is named Bomb or Computer instead of Bomb(Clone) or Computer(Clone)
        transform.name = transform.tag;
    }

    public void Dissipate(bool isHeroTurn)
    {
        if ((dissipatesHeroTurn && isHeroTurn) || (dissipatesVillainTurn && !isHeroTurn))
        {
            CanvasGroup tokenCanvas = gameObject.GetComponent<CanvasGroup>();
            if (!partiallyDissipated)
            {
                tokenCanvas.alpha = (float).7;
                partiallyDissipated = true;
            }
            else
            {
                GetZone().GetComponent<ZoneInfo>().RemoveEnvironToken(gameObject);
            }
        }
    }

    public GameObject GetZone()
    {
        return transform.parent.parent.gameObject;  // Grabs ZoneInfoPanel instead of TokensRow. If changes in future, only need to change this function.
    }

    public void QuantityModified()
    {
        if (quantity > 1)
        {
            TMP_Text quantityText = transform.Find("QuantityText").GetComponent<TMP_Text>();
            quantityText.text = 'x' + quantity.ToString();
            quantityText.enabled = true;
        }
    }

    public void LoadEnvironTokenSave(EnvironTokenSave environTokenSave)
    {
        quantity = environTokenSave.quantity;
        QuantityModified();
        dissipatesHeroTurn = environTokenSave.dissipatesHeroTurn;
        dissipatesVillainTurn = environTokenSave.dissipatesVillainTurn;
        partiallyDissipated = environTokenSave.partiallyDissipated;
        if (partiallyDissipated)
        {
            gameObject.GetComponent<CanvasGroup>().alpha = (float).7;
        }
    }

    public EnvironTokenSave ToJSON()
    {
        return new EnvironTokenSave(this);
    }
}

[Serializable]
public class EnvironTokenSave
{
    public string tag;
    public int quantity;
    public bool dissipatesHeroTurn;
    public bool dissipatesVillainTurn;
    public bool partiallyDissipated;

    public EnvironTokenSave(EnvironToken environToken)
    {
        tag = environToken.tag;
        quantity = environToken.quantity;
        dissipatesHeroTurn = environToken.dissipatesHeroTurn;
        dissipatesVillainTurn = environToken.dissipatesVillainTurn;
        partiallyDissipated = environToken.partiallyDissipated;
    }

    public EnvironTokenSave(string newTag, int newQuantity, bool newDissipatesHeroTurn, bool newDissipatesVillainTurn)
    {
        tag = newTag;
        quantity = newQuantity;
        dissipatesHeroTurn = newDissipatesHeroTurn;
        dissipatesVillainTurn = newDissipatesVillainTurn;
    }
}