using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;  // To update TMP_Text

public class Animate : MonoBehaviour
{
    public GameObject mainCamera;
    public Camera cameraStuff;

    public GameObject explosionPrefab;
    public GameObject grenadePrefab;
    public GameObject gameOverPrefab;

    private void Awake()
    {
        mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
        cameraStuff = mainCamera.GetComponent<Camera>();  // Not initialized quickly enough if in Start()
    }

    public IEnumerator FadeOut(CanvasGroup canvasGroupToFade, float fadedAlpha,  float fadeTimeCoefficient = .5f)
    {
        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * fadeTimeCoefficient;

            float transparency = Mathf.Lerp(1, fadedAlpha, t);
            canvasGroupToFade.alpha = transparency;

            yield return null;
        }
        yield return 0;
    }

    public IEnumerator MoveObjectOverTime(List<GameObject> objectsToMove, Vector3 origin, Vector3 destination, float timeCoefficient = .5f)
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

    public bool IsPointOnScreen(Vector3 point, float buffer = .2f)
    {
        Vector3 screenPoint = cameraStuff.WorldToViewportPoint(point);
        if (screenPoint.z > 0 && screenPoint.x > buffer && screenPoint.x < 1 - buffer && screenPoint.y > buffer && screenPoint.y < 1 - buffer)
        {
            return true;
        }
        return false;
    }

    public Vector3 GetCameraCoordsBetweenFocusAndTarget(Vector3 focus, Vector3 target, float buffer = .2f)
    {
        float height = 2f * cameraStuff.orthographicSize - buffer;
        float width = height * cameraStuff.aspect - buffer;
        Vector3 halfwayPoint = (focus + target) / 2;

        if (width >= Mathf.Abs(focus.x - halfwayPoint.x) && height >= Mathf.Abs(focus.y - halfwayPoint.y))
        {
            //Debug.Log("!!!GetCameraCoordsBetweenFocusAndTarget focus: " + focus.ToString() + "   target: " + target.ToString() + "   halfwayPoint: " + halfwayPoint.ToString() + "   Mathf.Abs(focus.x - halfwayPoint.x): " + Mathf.Abs(focus.x - halfwayPoint.x).ToString() + "\ncameraWidth: " + width.ToString() + "   cameraHeight: " + height.ToString());
            return halfwayPoint;
        }
        else
        {
            float x = focus.x < target.x ? focus.x + width : focus.x - width;
            float y = focus.y < target.y ? focus.y + width : focus.y - width;
            //Debug.Log("!!!GetCameraCoordsBetweenFocusAndTarget focus: " + focus.ToString() + "   target: " + target.ToString() + "   halfwayPoint: " + halfwayPoint.ToString() + "   inFocusPoint: " + new Vector3(x, y, halfwayPoint.z).ToString() + "\ncameraWidth: " + width.ToString() + "   cameraHeight: " + height.ToString());
            return new Vector3(x, y, halfwayPoint.z);
        }
    }

    public IEnumerator MoveCameraUntilOnscreen(Vector3 origin, Vector3 destination, float timeCoefficient = .3f)
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
        Vector3 cameraStartCoords = IsPointOnScreen(origin) ? mainCamera.transform.position : origin;
        StartCoroutine(MoveCameraUntilOnscreen(cameraStartCoords, destination));
        yield return StartCoroutine(MoveObjectOverTime(new List<GameObject>() { grenade }, origin, destination));
        Destroy(grenade);
        yield return 0;
    }

    public void ShowExplosion(Vector3 targetCoords)
    {
        GameObject explosionObject = Instantiate(explosionPrefab, transform);
        explosionObject.transform.position = targetCoords;
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
