using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;  // For button
using TMPro;  // for getting hero number

public class Hero : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void HeroButtonClicked(Button button)
    {
        string heroTag = "";
        switch (button.transform.Find("NumButtonText").GetComponent<TMP_Text>().text)
        {
            case "1":
                heroTag = "1stHero";
                break;
            case "2":
                heroTag = "2ndHero";
                break;
            case "3":
                heroTag = "3rdHero";
                break;
        }
        if (!button.CompareTag(heroTag))
        {
            GameObject oldHeroButton = GameObject.FindGameObjectWithTag(heroTag);
            if (oldHeroButton != null)
            {
                oldHeroButton.tag = "Untagged";
                oldHeroButton.GetComponent<CanvasGroup>().alpha = (float).2;
            }
            button.tag = heroTag;
            button.GetComponent<CanvasGroup>().alpha = (float)1;
        }
        else
        {
            CanvasGroup buttonCanvas = button.GetComponent<CanvasGroup>();
            if (buttonCanvas.alpha == 1)  // Hero was defeated, so remove from board
            {
                button.tag = "Untagged";
                buttonCanvas.alpha = (float).2;
            }
            else  // Mistake was made in removing hero, so add hero back to the board
            {
                button.tag = heroTag;
                buttonCanvas.alpha = (float)1;
            }
        }
    }
}
