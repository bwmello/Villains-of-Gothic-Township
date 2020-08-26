﻿using System;  // For DateTime
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;  // For button
using TMPro;  // To update TMP_Text

public class Animate : MonoBehaviour
{
    public GameObject mainCamera;
    public Camera cameraStuff;

    public GameObject woundPrefab;
    public GameObject bulletPrefab;
    public GameObject impactPrefab;
    public GameObject grenadePrefab;
    public GameObject explosionPrefab;
    public GameObject explosionLoopingPrefab;
    public GameObject continueButtonPrefab;
    public GameObject gameOverPrefab;


    private void Awake()
    {
        mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
        cameraStuff = mainCamera.GetComponent<Camera>();  // Not initialized quickly enough if in Start()
    }

    public IEnumerator FadeObjects(List<GameObject> objectsToFade, float alphaStart, float alphaEnd, float fadeTimeCoefficient = .5f)
    {
        for (int i = 0; i < objectsToFade.Count; i++)
        {
            if (i < objectsToFade.Count - 1)
            {
                StartCoroutine(FadeCanvasGroup(objectsToFade[i].GetComponent<CanvasGroup>(), alphaStart, alphaEnd, fadeTimeCoefficient));
            }
            else
            {
                yield return StartCoroutine(FadeCanvasGroup(objectsToFade[i].GetComponent<CanvasGroup>(), alphaStart, alphaEnd, fadeTimeCoefficient));
            }
        }
        yield return 0;
    }

    public IEnumerator FadeCanvasGroup(CanvasGroup canvasGroupToFade, float alphaStart, float alphaEnd, float fadeTimeCoefficient)
    {
        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * fadeTimeCoefficient;
            float transparency = Mathf.Lerp(alphaStart, alphaEnd, t);
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

    public IEnumerator ScaleObjectOverTime(List<GameObject> objectsToMove, Vector3 start, Vector3 end, float timeCoefficient = .5f)  // TODO maybe remove this timeCoefficient param
    {
        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * timeCoefficient;
            foreach (GameObject currentObject in objectsToMove)
            {
                currentObject.transform.localScale = new Vector3(Mathf.Lerp(start.x, end.x, t), Mathf.Lerp(start.y, end.y, t), currentObject.transform.localScale.z);
            }
            yield return null;
        }
        yield return 0;
    }

    public void CameraToFixedZoom()
    {
        cameraStuff.orthographicSize = 2.2f;
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


    bool waitingOnPlayerInput = false;
    public IEnumerator PauseUntilPlayerPushesContinue(GameObject targetedHero)
    {
        Button heroButton = targetedHero.GetComponent<Button>();
        heroButton.enabled = true;
        waitingOnPlayerInput = true;
        GameObject zone = targetedHero.GetComponent<Hero>().GetZone();
        GameObject continueButton = Instantiate(continueButtonPrefab, zone.transform);
        continueButton.transform.position = zone.transform.TransformPoint(0, -50f, 0);
        //continueButton.transform.position = targetedHero.transform.TransformPoint(0, 25f, 0);
        continueButton.GetComponent<Button>().onClick.AddListener(delegate { waitingOnPlayerInput = false; });
        yield return new WaitUntil(() => !waitingOnPlayerInput);
        heroButton.enabled = false;
        Destroy(continueButton);
        yield return 0;
    }


    private bool showingImpact = false;
    public IEnumerator ShowImpact(Vector3 impactPosition, float frequency)
    {
        showingImpact = true;
        GameObject impact = Instantiate(impactPrefab, transform);
        while (showingImpact)
        {
            DateTime nextBulletPathStartTime = DateTime.Now.AddSeconds(frequency);
            impact.transform.position = impactPosition;
            StartCoroutine(ScaleObjectOverTime(new List<GameObject>() { impact }, new Vector3(.5f, .5f, .5f), new Vector3(2f, 2f, 2f)));
            yield return StartCoroutine(FadeObjects(new List<GameObject>() { impact }, 1f, 0, .5f));

            while (showingImpact && DateTime.Now < nextBulletPathStartTime)
            {
                yield return null;
            }
        }
        Destroy(impact);
        yield return 0;
    }

    public void EndShowImpact()
    {
        showingImpact = false;
    }

    private readonly Vector2[] woundPlacement = new[] { new Vector2(-9f, 8f), new Vector2(9f, 8f), new Vector2(-9f, -8f), new Vector2(9f, -8f), new Vector2(-9f, 0f), new Vector2(9f, 0f), new Vector2(-3f, 8f), new Vector2(3f, -8f), new Vector2(3f, 8f), new Vector2(-3f, -8f) };
    public IEnumerator MeleeAttack(GameObject attacker, GameObject target, int woundsTotal)
    {
        attacker.GetComponent<ObjectShake>().StartShaking();
        target.GetComponent<ObjectShake>().StartShaking();

        //Vector3 pointFacingEnemy = Vector3.Lerp(target.transform.position, attacker.transform.position, 0.3f);
        // vector pointing from the planet to the player
        Vector3 difference = attacker.transform.position - target.transform.position;
        // the direction of the launch, normalized
        Vector3 directionOnly = difference.normalized;
        // the point along this vector you are requesting
        Vector3 pointAlongDirection = target.transform.position + (directionOnly * .07f);  // * float is the distance along this direction
        //Debug.Log("!!!target.transform.position: " + target.transform.position.ToString() + "  pointAlongDirection: " + pointAlongDirection.ToString());
        StartCoroutine(ShowImpact(pointAlongDirection, 3f));

        yield return StartCoroutine(MoveCameraUntilOnscreen(attacker.transform.position, target.transform.position));

        List<GameObject> wounds = new List<GameObject>();
        for (int i = 0; i < woundsTotal; i++)
        {
            wounds.Add(Instantiate(woundPrefab, target.transform));
            wounds[i].transform.localPosition = new Vector3(woundPlacement[i].x, woundPlacement[i].y, 0);
        }
        StartCoroutine(FadeObjects(wounds, 0, 1, .9f));

        foreach (Button unitButton in attacker.transform.GetComponentsInChildren<Button>())
        {
            unitButton.enabled = true;
        }
        PanAndZoom panAndZoom = mainCamera.GetComponent<PanAndZoom>();
        if (!IsPointOnScreen(transform.position, .1f))
        {
            panAndZoom.controlCamera = true;
        }
        yield return StartCoroutine(PauseUntilPlayerPushesContinue(target));

        foreach (Button unitButton in attacker.transform.GetComponentsInChildren<Button>())
        {
            unitButton.enabled = false;
        }
        panAndZoom.controlCamera = false;
        CameraToFixedZoom();

        EndShowImpact();
        attacker.GetComponent<ObjectShake>().StopShaking();
        target.GetComponent<ObjectShake>().StopShaking();

        yield return StartCoroutine(FadeObjects(wounds, 1, 0, .9f));
        for (int i = wounds.Count - 1; i >= 0; i--)
        {
            Destroy(wounds[i]);
        }

        yield return 0;
    }

    private bool firingBullets = false;
    public IEnumerator ShowBulletPath(Vector3 start, Vector3 end, float frequency)
    {
        firingBullets = true;
        while (firingBullets)
        {
            DateTime nextBulletPathStartTime = DateTime.Now.AddSeconds(frequency);
            GameObject bullet = Instantiate(bulletPrefab, transform);
            bullet.transform.rotation = Quaternion.Euler(new Vector3(0, 0, Mathf.Atan2((end.y - start.y), (end.x - start.x)) * Mathf.Rad2Deg));

            yield return StartCoroutine(MoveObjectOverTime(new List<GameObject>() { bullet }, start, end, .9f));
            Destroy(bullet);
            while (firingBullets && DateTime.Now < nextBulletPathStartTime)
            {
                yield return null;
            }
        }
        yield return 0;
    }

    public void EndBulletPaths()
    {
        firingBullets = false;
    }

    public IEnumerator RangedAttack(GameObject attacker, GameObject target, int woundsTotal)
    {
        attacker.GetComponent<ObjectShake>().StartShaking();
        target.GetComponent<ObjectShake>().StartShaking();
        StartCoroutine(ShowBulletPath(attacker.transform.position, target.transform.position, 3f));

        yield return StartCoroutine(MoveCameraUntilOnscreen(attacker.transform.position, target.transform.position));

        List<GameObject> wounds = new List<GameObject>();
        for (int i = 0; i < woundsTotal; i++)
        {
            wounds.Add(Instantiate(woundPrefab, target.transform));
            wounds[i].transform.localPosition = new Vector3(woundPlacement[i].x, woundPlacement[i].y, 0);
        }
        StartCoroutine(FadeObjects(wounds, 0, 1, .9f));

        foreach (Button unitButton in attacker.transform.GetComponentsInChildren<Button>())
        {
            unitButton.enabled = true;
        }
        PanAndZoom panAndZoom = mainCamera.GetComponent<PanAndZoom>();
        if (!IsPointOnScreen(attacker.transform.position, .1f))  // If you can still see the attacker after panning to the target, player doesn't need camera control
        {
            panAndZoom.controlCamera = true;
        }
        yield return StartCoroutine(PauseUntilPlayerPushesContinue(target));

        foreach (Button unitButton in attacker.transform.GetComponentsInChildren<Button>())
        {
            unitButton.enabled = false;
        }
        panAndZoom.controlCamera = false;
        CameraToFixedZoom();

        EndBulletPaths();
        attacker.GetComponent<ObjectShake>().StopShaking();
        target.GetComponent<ObjectShake>().StopShaking();

        yield return StartCoroutine(FadeObjects(wounds, 1, 0, .9f));
        for (int i = wounds.Count - 1; i >= 0; i--)
        {
            Destroy(wounds[i]);
        }

        yield return 0;
    }

    public IEnumerator ThrowGrenade(Vector3 origin, Vector3 destination)
    {
        GameObject grenade = Instantiate(grenadePrefab, transform);
        grenade.transform.position = origin;
        StartCoroutine(MoveCameraUntilOnscreen(origin, destination));
        yield return StartCoroutine(MoveObjectOverTime(new List<GameObject>() { grenade }, origin, destination));
        ShowExplosion(grenade.transform.position);
        Destroy(grenade);
        yield return 0;
    }

    public void ShowExplosion(Vector3 targetCoords)
    {
        GameObject explosionObject = Instantiate(explosionPrefab, transform);
        explosionObject.transform.position = targetCoords;
    }

    public void ShowLoopingExplosion(Vector3 targetCoords)
    {
        GameObject explosionObject = Instantiate(explosionLoopingPrefab, transform);
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
