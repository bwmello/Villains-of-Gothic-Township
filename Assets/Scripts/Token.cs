using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;  // For button
using TMPro;  // for getting hero number

public class Token : MonoBehaviour  // Should be called ObjectiveToken as there was no need to distinguish between EnvironTokens
{
    void Awake()
    {  // Needed so that when Instantiated is named Bomb or Computer instead of Bomb(Clone) or Computer(Clone)
        transform.name = transform.tag;
    }

    void Start()
    {
        if (gameObject.CompareTag("PrimedBomb"))
        {
            InvokeRepeating(nameof(Blink), Random.Range(1f, 3.5f), 2f);
        }
    }

    public bool isClickable = false;
    public void SetIsClickable(bool shouldMakeClickable)
    {
        //gameObject.GetComponent<Button>().enabled = shouldMakeClickable;
        gameObject.GetComponent<Button>().interactable = shouldMakeClickable;
        isClickable = shouldMakeClickable;
    }

    public void ConfigureClickability()
    {
        switch (MissionSpecifics.currentPhase)
        {
            case "Hero":
                SetIsClickable(true);
                break;
            case "Villain":
                SetIsClickable(false);
                break;
        }
    }

    public void TokenButtonClicked(Button button)
    {
        MissionSpecifics.ObjectiveTokenClicked(button); // Can't call MissionSpecifics function directly from token button prefab as doesn't list any of MissionSpecifics functions (probably because not MonoBehavior)
    }

    public bool IsActive()
    {
        CanvasGroup buttonCanvas = gameObject.GetComponent<CanvasGroup>();
        if (buttonCanvas.alpha == 1)
        {
            return true;
        }
        return false;
    }

    public GameObject GetZone()
    {
        return transform.parent.parent.gameObject;  // Grabs ZoneInfoPanel instead of TokensRow. If changes in future, only need to change this function.
    }

    void Blink()
    {
        GameObject redLight = transform.Find("RedLight").gameObject;
        if (IsActive() && !redLight.activeSelf)
        {
            redLight.SetActive(true);
        }
        else
        {
            redLight.SetActive(false);
        }
    }
}