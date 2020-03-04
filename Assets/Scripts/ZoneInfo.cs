using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ZoneInfo : MonoBehaviour
{
    public void TokenButtonClicked(Button button)
    {
        //Debug.Log("TokenButtonClicked!!! button.name: " + button.name);
        CanvasGroup buttonCanvas = button.GetComponent<CanvasGroup>();
        if (buttonCanvas.alpha == 1)
        {
            buttonCanvas.alpha = (float).2;
        }
        else
        {
            buttonCanvas.alpha = (float)1;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
