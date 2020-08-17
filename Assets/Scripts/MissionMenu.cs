using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MissionMenu : MonoBehaviour
{
    public GameObject spotlightPrefab;

    // Start is called before the first frame update
    IEnumerator Start()
    {
        yield return new WaitForSecondsRealtime(Random.Range(1.0f, 3.0f));
        GameObject spotlight1 = Instantiate(spotlightPrefab, transform);
        spotlight1.transform.localPosition = new Vector3(-20, 185, 0);
        spotlight1.GetComponent<Spotlight>().leftPoint = new Vector3(-140, 185, 0);
        yield return new WaitForSecondsRealtime(Random.Range(1.0f, 3.0f));
        GameObject spotlight2 = Instantiate(spotlightPrefab, transform);
        spotlight2.transform.localPosition = new Vector3(480, 225, 0);
        spotlight2.GetComponent<Spotlight>().leftPoint = new Vector3(380, 85, 0);
        yield return null;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
