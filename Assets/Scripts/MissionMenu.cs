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
        GameObject spotlight1 = Instantiate(spotlightPrefab, transform);
        // parent_width=1920, x_center=-5, percentage_of_parent_width=-5/1920=-0.00260416666; parent_height=1080, y_center=245, percentage_of_parent_height=245/1080=0.22685185185
        spotlight1.transform.localPosition = new Vector3((float)(transform.GetComponent<RectTransform>().rect.width * -0.00260416666), (float)(transform.GetComponent<RectTransform>().rect.height * 0.22685185185), 0);
        // parent_width=1920, x_center=-140, percentage_of_parent_width=-140/1920=-0.07291666666; parent_height=1080, y_center=265, percentage_of_parent_height=265/1080=0.24537037037
        spotlight1.GetComponent<Spotlight>().leftPoint = new Vector3((float)(transform.GetComponent<RectTransform>().rect.width * -0.07291666666), (float)(transform.GetComponent<RectTransform>().rect.height * 0.24537037037), 0);
        yield return new WaitForSecondsRealtime(Random.Range(2.0f, 3.0f));

        GameObject spotlight2 = Instantiate(spotlightPrefab, transform);
        // parent_width=1920, x_center=485, percentage_of_parent_width=485/1920=0.25260416666; parent_height=1080, y_center=315, percentage_of_parent_height=315/1080=0.29166666666
        spotlight2.transform.localPosition = new Vector3((float)(transform.GetComponent<RectTransform>().rect.width * 0.25260416666), (float)(transform.GetComponent<RectTransform>().rect.height * 0.29166666666), 0);
        // parent_width=1920, x_center=375, percentage_of_parent_width=375/1920=0.1953125; parent_height=1080, y_center=160, percentage_of_parent_height=160/1080=0.14814814814
        spotlight2.GetComponent<Spotlight>().leftPoint = new Vector3((float)(transform.GetComponent<RectTransform>().rect.width * 0.1953125), (float)(transform.GetComponent<RectTransform>().rect.height * 0.14814814814), 0);
        yield return null;
    }
}
