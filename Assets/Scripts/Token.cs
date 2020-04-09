using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;  // For button
using TMPro;  // for getting hero number

public class Token : MonoBehaviour
{
    void Awake()
    {  // Needed so that when Instantiated is named Bomb or Computer instead of Bomb(Clone) or Computer(Clone)
        transform.name = transform.tag;
    }

    public void TokenButtonClicked(Button button)
    {
        CanvasGroup buttonCanvas = button.GetComponent<CanvasGroup>();
        if (buttonCanvas.alpha == 1)  // Token was disabled, so remove from board
        {
            buttonCanvas.alpha = (float).2;
        }
        else  // Mistake was made in removing token, so add token back to the board
        {
            buttonCanvas.alpha = (float)1;
        }
    }
}
