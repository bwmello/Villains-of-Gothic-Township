﻿using System.Collections;
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

    public IEnumerator MoveObjectOverTime(List<GameObject> objectsToMove, Vector3 origin, Vector3 destination, float timeCoefficient = .5f)  // TODO maybe remove this timeCoefficient param
    {
        float xDistance = Mathf.Abs(origin.x - destination.x);
        float yDistance = Mathf.Abs(origin.y - destination.y);
        float longestDistance = xDistance >= yDistance ? xDistance : yDistance;
        timeCoefficient = 1 / Mathf.Sqrt(longestDistance);
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
        if (screenPoint.x > buffer && screenPoint.x < 1 - buffer && screenPoint.y > buffer && screenPoint.y < 1 - buffer)
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

        if (width / 2 >= Mathf.Abs(focus.x - halfwayPoint.x) && height / 2 >= Mathf.Abs(focus.y - halfwayPoint.y))
        {
            //Debug.Log("!!!GetCameraCoordsBetweenFocusAndTarget focus: " + focus.ToString() + "   target: " + target.ToString() + "   halfwayPoint: " + halfwayPoint.ToString() + "   Mathf.Abs(focus.x - halfwayPoint.x): " + Mathf.Abs(focus.x - halfwayPoint.x).ToString() + "   Mathf.Abs(focus.y - halfwayPoint.y): " + Mathf.Abs(focus.y - halfwayPoint.y).ToString() + "\ncameraWidth: " + width.ToString() + "   cameraHeight: " + height.ToString());
            return halfwayPoint;
        }
        else
        {
            float x = focus.x < target.x ? focus.x + width / 2 : focus.x - width / 2;
            float y = focus.y < target.y ? focus.y + height / 2 : focus.y - height / 2;
            //Debug.Log("!!!GetCameraCoordsBetweenFocusAndTarget focus: " + focus.ToString() + "   target: " + target.ToString() + "   halfwayPoint: " + halfwayPoint.ToString() + "   inFocusPoint: " + new Vector3(x, y, halfwayPoint.z).ToString() + "\ncameraWidth: " + width.ToString() + "   cameraHeight: " + height.ToString());
            return new Vector3(x, y, halfwayPoint.z);
        }
    }

    public IEnumerator MoveCameraUntilOnscreen(Vector3 origin, Vector3 destination, float timeCoefficient = .3f)  // Pass camera's position as origin if you don't want camera to jump
    {
        float t = 0;
        Vector3 camStartCoords;
        if (IsPointOnScreen(origin))
        {
            //Debug.Log("!!!MoveCameraUntilOnscreen, origin " + origin.ToString() + "  is on screen.");
            if (IsPointOnScreen(destination))
            {
                //Debug.Log("destination " + destination.ToString() + "  is also on screen, so exiting.");
                yield break;
            }
            camStartCoords = mainCamera.transform.position;
            //Debug.Log("destination not on screen, so setting camStartCoords to mainCamera.transform.position: " + mainCamera.transform.position.ToString());
        }
        else
        {
            camStartCoords = GetCameraCoordsBetweenFocusAndTarget(origin, destination);
            //Debug.Log("!!!MoveCameraUntilOnscreen, origin " + origin.ToString() + "  not on screen, so setting camStartCoords to between FocusAndTarget: " + camStartCoords.ToString());
        }
        while (t < 1f)
        {
            t += Time.deltaTime * timeCoefficient;
            mainCamera.transform.position = new Vector3(Mathf.Lerp(camStartCoords.x, destination.x, t), Mathf.Lerp(camStartCoords.y, destination.y, t), mainCamera.transform.position.z);
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
