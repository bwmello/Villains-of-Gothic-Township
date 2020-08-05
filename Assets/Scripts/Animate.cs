using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;  // To update TMP_Text

public class Animate : MonoBehaviour
{
    GameObject mainCamera;
    Camera cameraStuff;

    public GameObject grenadePrefab;
    public GameObject gameOverPrefab;

    private void Awake()
    {
        mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
        cameraStuff = mainCamera.GetComponent<Camera>();  // Not initialized quickly enough if in Start()
    }

    public IEnumerator MoveObjectOverTime(List<GameObject> objectsToMove, Vector3 origin, Vector3 destination, float timeCoefficient = .5f)  // Right now mainCamera is also being passed in objectsToMove for Unit.AnimateWounds
    {
        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * timeCoefficient;
            foreach (GameObject currentObject in objectsToMove)
            {
                currentObject.transform.position = new Vector3(Mathf.Lerp(origin.x, destination.x, t), Mathf.Lerp(origin.y, destination.y, t), currentObject.transform.position.z);
            }
            yield return null;
        }
        yield return 0;
    }

    public bool IsPointOnScreen(Vector3 point, double buffer = .2)
    {
        Vector3 screenPoint = cameraStuff.WorldToViewportPoint(point);
        if (screenPoint.z > 0 && screenPoint.x > buffer && screenPoint.x < 1 - buffer && screenPoint.y > buffer && screenPoint.y < 1 - buffer)  // If destination is on camera, stop moving camera
        {
            return true;
        }
        return false;
    }

    public IEnumerator MoveCameraUntilOnscreen(Vector3 origin, Vector3 destination, float timeCoefficient = .3f)  // Right now mainCamera is also being passed in objectsToMove for Unit.AnimateWounds
    {
        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * timeCoefficient;
            if (IsPointOnScreen(origin) && IsPointOnScreen(destination))  // If destination is on camera, stop moving camera
            {
                break;
            }
            mainCamera.transform.position = new Vector3(Mathf.Lerp(origin.x, destination.x, t), Mathf.Lerp(origin.y, destination.y, t), mainCamera.transform.position.z);
            if (IsPointOnScreen(destination))  // If destination is on camera, stop moving camera
            {
                break;
            }
            yield return null;
        }
        yield return 0;
    }

    public IEnumerator ThrowGrenade(Vector3 origin, Vector3 destination)
    {
        GameObject grenade = Instantiate(grenadePrefab, transform);
        grenade.transform.position = origin;
        StartCoroutine(MoveCameraUntilOnscreen(origin, destination));
        yield return StartCoroutine(MoveObjectOverTime(new List<GameObject>() { grenade }, origin, destination));
        Destroy(grenade);
        yield return 0;
    }

    public void ShowGameOver()
    {
        GameObject gameOverPanel = Instantiate(gameOverPrefab, transform);
        if (MissionSpecifics.IsHeroVictory())
        {
            gameOverPanel.transform.Find("MissionStatusText").GetComponent<TMP_Text>().text = "<color=\"green\">Mission Success";
        }
        else
        {
            gameOverPanel.transform.Find("MissionStatusText").GetComponent<TMP_Text>().text = "<color=\"red\">Mission Failure";
        }
        StartCoroutine(MoveObjectOverTime(new List<GameObject>() { mainCamera }, mainCamera.transform.position, new Vector3(0, 0, 0)));  // Move camera to center
    }
}
