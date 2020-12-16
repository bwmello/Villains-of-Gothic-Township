using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MissionMenu : MonoBehaviour
{
    public GameObject spotlightPrefab;


    public void Awake()
    {
        SceneHandler.isFirstTimePlaying = true;  // Set to false by individual MissionSelection scripts
    }

    // Start is called before the first frame update
    private IEnumerator Start()
    {
        //yield return new WaitForSecondsRealtime(Random.Range(0.5f, 1.0f));
        GameObject spotlight1 = Instantiate(spotlightPrefab, transform);
        //Debug.Log("!!transform.width: " + transform.GetComponent<RectTransform>().rect.width.ToString() + "    transform.height: " + transform.GetComponent<RectTransform>().rect.height.ToString());
        //Debug.Log("!!parent.transform.width: " + transform.parent.GetComponent<RectTransform>().rect.width.ToString() + "    parent.transform.height: " + transform.parent.GetComponent<RectTransform>().rect.height.ToString());
        // parent_width=1920, x_center=-5, percentage_of_parent_width=-5/1920=-0.00260416666; parent_height=1080, y_center=170, percentage_of_parent_height=170/1080=0.1574074074
        spotlight1.transform.localPosition = new Vector3((float)(transform.GetComponent<RectTransform>().rect.width * -0.00260416666), (float)(transform.GetComponent<RectTransform>().rect.height * 0.1574074074), 0);
        // parent_width=1920, x_center=-140, percentage_of_parent_width=-140/1920=-0.07291666666; parent_height=1080, y_center=185, percentage_of_parent_height=185/1080=0.17129629629
        spotlight1.GetComponent<Spotlight>().leftPoint = new Vector3((float)(transform.GetComponent<RectTransform>().rect.width * -0.07291666666), (float)(transform.GetComponent<RectTransform>().rect.height * 0.17129629629), 0);
        yield return new WaitForSecondsRealtime(Random.Range(2.0f, 3.0f));

        GameObject spotlight2 = Instantiate(spotlightPrefab, transform);
        // parent_width=1920, x_center=480, percentage_of_parent_width=-480/1920=0.25; parent_height=1080, y_center=215, percentage_of_parent_height=215/1080=0.19907407407
        spotlight2.transform.localPosition = new Vector3((float)(transform.GetComponent<RectTransform>().rect.width * 0.25), (float)(transform.GetComponent<RectTransform>().rect.height * 0.19907407407), 0);
        // parent_width=1920, x_center=390, percentage_of_parent_width=-390/1920=0.203125; parent_height=1080, y_center=95, percentage_of_parent_height=8=95/1080=0.08796296296
        spotlight2.GetComponent<Spotlight>().leftPoint = new Vector3((float)(transform.GetComponent<RectTransform>().rect.width * 0.203125), (float)(transform.GetComponent<RectTransform>().rect.height * 0.08796296296), 0);
        yield return null;
    }
}
