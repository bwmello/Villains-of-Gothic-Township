using System;  // For DateTime
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;  // For button
using TMPro;  // To update TMP_Text

public class Animate : MonoBehaviour
{
    public GameObject mainCamera;
    public Camera cameraStuff;
    public GameObject continueButtonUIOverlay;  // Used for PauseUntilPlayerPushesContinue() without having to go through the UIOverlay
    public GameObject uiAnimationContainer;  // Used for animations with claimableTokens on UIOverlay's utilityBelt

    public GameObject woundPrefab;
    public GameObject woundQuestionPrefab;
    public GameObject bulletPrefab;
    public GameObject impactPrefab;
    public GameObject grenadePrefab;
    public GameObject explosionPrefab;
    public GameObject explosionLoopingPrefab;


    private void Awake()
    {
        mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
        cameraStuff = mainCamera.GetComponent<Camera>();  // Not initialized quickly enough if in Start()
    }

    //private bool isPortraitOrientation;  // Would also need to reposition camera after zoom
    //private void Update()  // https://answers.unity.com/questions/1589034/how-can-i-detect-device-orientation-change-in-a-fa.html
    //{
    //    if (Input.deviceOrientation == DeviceOrientation.Portrait || Input.deviceOrientation == DeviceOrientation.PortraitUpsideDown)
    //    {
    //        if (!isPortraitOrientation)
    //        {
    //            // max zoom changed
    //        }
    //        isPortraitOrientation = true;
    //    }
    //    else
    //    {
    //        if (isPortraitOrientation)
    //        {
    //            // max zoom changed
    //        }
    //        isPortraitOrientation = false;
    //    }
    //}

    private readonly float woundFadeTimeCoefficient = 2f;
    public IEnumerator FadeObjects(List<GameObject> objectsToFade, float alphaStart, float alphaEnd, float fadeTimeCoefficient = .5f, float timeBetweenObjectsFading = .15f)
    {
        foreach (GameObject objectToFade in objectsToFade)
        {
            objectToFade.GetComponent<CanvasGroup>().alpha = alphaStart;  // Otherwise objects blink before timeBetweenObjectsFading passes and they start their FadeCanvasGroup coroutine
        }
        for (int i = 0; i < objectsToFade.Count; i++)
        {
            if (objectsToFade[i] != null)
            {
                if (i < objectsToFade.Count)
                {
                    StartCoroutine(FadeCanvasGroup(objectsToFade[i].GetComponent<CanvasGroup>(), alphaStart, alphaEnd, fadeTimeCoefficient));
                    yield return new WaitForSecondsRealtime(timeBetweenObjectsFading);
                }
                else
                {
                    yield return StartCoroutine(FadeCanvasGroup(objectsToFade[i].GetComponent<CanvasGroup>(), alphaStart, alphaEnd, fadeTimeCoefficient));
                }
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
            if (canvasGroupToFade != null)
            {
                canvasGroupToFade.alpha = transparency;
            }
            else
            {
                break;
            }
            yield return null;
        }
        yield return 0;
    }

    public IEnumerator ScaleObjects(List<GameObject> objectsToScale, float scaleStart, float scaleEnd, float scaleTimeCoefficient = .5f, float timeBetweenObjectsScaling = .15f)
    {
        foreach (GameObject objectToScale in objectsToScale)
        {
            objectToScale.transform.localScale = new Vector3(scaleStart, scaleStart);  // Otherwise objects blink before timeBetweenObjectsFading passes and they start their FadeCanvasGroup coroutine
        }
        for (int i = 0; i < objectsToScale.Count; i++)
        {
            if (objectsToScale[i] != null)
            {
                if (i < objectsToScale.Count)
                {
                    StartCoroutine(ScaleTransform(objectsToScale[i].transform, scaleStart, scaleEnd, scaleTimeCoefficient));
                    yield return new WaitForSecondsRealtime(timeBetweenObjectsScaling);
                }
                else
                {
                    yield return StartCoroutine(ScaleTransform(objectsToScale[i].transform, scaleStart, scaleEnd, scaleTimeCoefficient));
                }
            }
        }
        yield return 0;
    }

    public IEnumerator ScaleTransform(Transform transformToScale, float scaleStart, float scaleEnd, float scaleTimeCoefficient)
    {
        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * scaleTimeCoefficient;
            float sizeScale = Mathf.Lerp(scaleStart, scaleEnd, t);
            if (transformToScale != null)
            {
                transformToScale.localScale = new Vector3(sizeScale, sizeScale);
            }
            else
            {
                break;
            }
            yield return null;
        }
        yield return 0;
    }

    public IEnumerator MoveObjectOverTime(List<GameObject> objectsToMove, Vector3 origin, Vector3 destination, float timeCoefficient = 1f)
    {
        float xDistance = Mathf.Abs(origin.x - destination.x);
        float yDistance = Mathf.Abs(origin.y - destination.y);
        float longestDistance = xDistance >= yDistance ? xDistance : yDistance;
        timeCoefficient *= 1 / Mathf.Sqrt(longestDistance);
        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * timeCoefficient;
            foreach (GameObject currentObject in objectsToMove)
            {
                if (currentObject != null)
                {
                    currentObject.transform.position = new Vector3(Mathf.Lerp(origin.x, destination.x, t), Mathf.Lerp(origin.y, destination.y, t), currentObject.transform.position.z);
                }
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
                if (currentObject != null)
                {
                    currentObject.transform.localScale = new Vector3(Mathf.Lerp(start.x, end.x, t), Mathf.Lerp(start.y, end.y, t), currentObject.transform.localScale.z);
                }
            }
            yield return null;
        }
        yield return 0;
    }

    public void CameraToFixedZoom()
    {
        cameraStuff.orthographicSize = 2.2f;  // This will look different for different devices/resolution
    }

    public void CameraToMaxZoom()
    {
        cameraStuff.orthographicSize = 20f;  // PanAndZoom's LateUpdate will bring this down to max zoom with CameraInBounds()
    }

    public bool IsPointOnScreen(Vector3 point, float buffer = .1f)
    {
        Vector3 screenPoint = cameraStuff.WorldToViewportPoint(point);
        if (screenPoint.x > buffer && screenPoint.x < 1 - buffer && screenPoint.y > (.01f + buffer) && screenPoint.y < 1 - buffer)  // Add .01f to bottom buffer for UtilityBelt, which is now displayed during both hero and villain turns. Not really needed, but may need to change .01f to more significant value in future
        {
            return true;
        }
        return false;
    }

    public Vector3 GetPointFurthestFromOrigin(Vector3 origin, Vector3 point1, Vector3 point2)
    {
        float x = Mathf.Abs(origin.x - point1.x) >= Mathf.Abs(origin.x - point2.x) ? point1.x : point2.x;
        float y = Mathf.Abs(origin.y - point1.y) >= Mathf.Abs(origin.y - point2.y) ? point1.y : point2.y;
        return new Vector3(x, y, origin.z);  // z doesn't really matter
    }

    public Vector3 GetCameraCoordsBetweenFocusAndTarget(Vector3 focus, Vector3 target, float buffer = .2f)
    {
        float height = 2f * cameraStuff.orthographicSize - buffer;
        float width = height * cameraStuff.aspect - buffer;
        Vector3 halfwayPoint = (focus + target) / 2;
        float x = halfwayPoint.x;
        float y = halfwayPoint.y;

        if (width / 2 < Mathf.Abs(focus.x - halfwayPoint.x))  // If camera isn't wide enough to show both focus.x and halfwayPoint.x
        {
            x = focus.x < target.x ? focus.x + width / 2 : focus.x - width / 2;
        }
        if (height / 2 < Mathf.Abs(focus.y - halfwayPoint.y))  // If camera height isn't tall enough to show both focus.x and halfwayPoint.x
        {
            y = focus.y < target.y ? focus.y + height / 2 : focus.y - height / 2;
        }
        //Debug.Log("!!!GetCameraCoordsBetweenFocusAndTarget focus: " + focus.ToString() + "   target: " + target.ToString() + "   final x,y coordinates: (" + x.ToString() + ", " + y.ToString() + ")   halfwayPoint (ignore the z): " + halfwayPoint.ToString() + "   Mathf.Abs(focus.x - halfwayPoint.x): " + Mathf.Abs(focus.x - halfwayPoint.x).ToString() + "   Mathf.Abs(focus.y - halfwayPoint.y): " + Mathf.Abs(focus.y - halfwayPoint.y).ToString() + "\ncameraWidth: " + width.ToString() + "   cameraHeight: " + height.ToString());
        return new Vector3(x, y, mainCamera.transform.position.z);

        //if (width / 2 >= Mathf.Abs(focus.x - halfwayPoint.x) && height / 2 >= Mathf.Abs(focus.y - halfwayPoint.y))  // Doesn't account for focus being offscreen for just width or for just height
        //{
        //    Debug.Log("!!!GetCameraCoordsBetweenFocusAndTarget focus: " + focus.ToString() + "   target: " + target.ToString() + "   inFocusPoint: " + new Vector3(x, y, mainCamera.transform.position.z).ToString() + "   halfwayPoint (ignore the z): " + halfwayPoint.ToString() + "   Mathf.Abs(focus.x - halfwayPoint.x): " + Mathf.Abs(focus.x - halfwayPoint.x).ToString() + "   Mathf.Abs(focus.y - halfwayPoint.y): " + Mathf.Abs(focus.y - halfwayPoint.y).ToString() + "\ncameraWidth: " + width.ToString() + "   cameraHeight: " + height.ToString());
        //    return new Vector3(halfwayPoint.x, halfwayPoint.y, mainCamera.transform.position.z);
        //}
        //else
        //{
        //    float x = focus.x < target.x ? focus.x + width / 2 : focus.x - width / 2;
        //    float y = focus.y < target.y ? focus.y + height / 2 : focus.y - height / 2;
        //    Debug.Log("!!!GetCameraCoordsBetweenFocusAndTarget focus: " + focus.ToString() + "   target: " + target.ToString() + "   halfwayPoint: " + halfwayPoint.ToString() + "   inFocusPoint: " + new Vector3(x, y, halfwayPoint.z).ToString() + "\ncameraWidth: " + width.ToString() + "   cameraHeight: " + height.ToString());
        //    return new Vector3(x, y, mainCamera.transform.position.z);
        //}
    }

    public Vector3 GetFurtherPointOnLine(Vector3 start, Vector3 end, float buffer = .6f)
    {
        Vector3 furthestPoint = new Vector3(end.x, end.y, end.z);
        if (start.x != end.x)
        {
            furthestPoint.x += start.x < end.x ? buffer : -buffer;
        }
        if (start.y != end.y)
        {
            furthestPoint.y += start.y < end.y ? buffer : -buffer;
        }
        return furthestPoint;
    }

    public float PostionCameraBeforeCameraMove(Vector3 origin, Vector3 destination)  // Only used for Unit.AnimateMovementPath()
    {
        float secondsToDelayBeforeCameraMove = 0;
        Vector3 coordsBetweenFocusAndTarget = GetCameraCoordsBetweenFocusAndTarget(origin, destination);
        if (!IsPointOnScreen(origin))
        {
            mainCamera.transform.position = coordsBetweenFocusAndTarget;
        }
        if (IsPointOnScreen(coordsBetweenFocusAndTarget))  // If camera already has a head start on moving unit, add a 1.5 second delay
        {
            secondsToDelayBeforeCameraMove = 1.5f;
        }
        return secondsToDelayBeforeCameraMove;
    }

    public IEnumerator MoveCameraUntilOnscreen(Vector3 origin, Vector3 destination, float timeCoefficient = .3f, float secondsToDelay = 0)  // Pass camera's position as origin if you don't want camera to jump
    {
        yield return new WaitForSecondsRealtime(secondsToDelay);
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
            mainCamera.transform.position = camStartCoords;
            yield return new WaitForSecondsRealtime(1f);  // Before panning camera to target, pause on origin (otherwise unit making long distance ranged attack is only on screen for a split second)
            //Debug.Log("!!!MoveCameraUntilOnscreen, origin " + origin.ToString() + "  not on screen, so setting camStartCoords to between that and destination: " + destination.ToString() + "  and getting GetCameraCoordsBetweenFocusAndTarget: " + camStartCoords.ToString());
        }

        while (t < 1f)
        {
            t += Time.deltaTime * timeCoefficient;
            mainCamera.transform.position = new Vector3(Mathf.Lerp(camStartCoords.x, destination.x, t), Mathf.Lerp(camStartCoords.y, destination.y, t), mainCamera.transform.position.z);
            if (IsPointOnScreen(destination))  // If destination is on camera, stop moving camera
            {
                //Debug.Log("!!!destination is on screen, so breaking MoveCamera loop");
                break;
            }
            yield return null;
        }
        yield return 0;
    }


    bool waitingOnPlayerInput = false;
    public IEnumerator PauseUntilPlayerPushesContinue(GameObject targetZone)
    {
        waitingOnPlayerInput = true;
        continueButtonUIOverlay.SetActive(true);
        yield return new WaitUntil(() => !waitingOnPlayerInput);
        continueButtonUIOverlay.SetActive(false);
        yield return 0;
    }

    public void ContinueButtonUIOverlayClicked()
    {
        waitingOnPlayerInput = false;
    }

    private bool showingImpact = false;
    public IEnumerator ShowImpact(Vector3 impactPosition, float frequency)
    {
        showingImpact = true;
        GameObject impact = Instantiate(impactPrefab, transform);
        while (showingImpact)
        {
            DateTime nextShowImpactStartTime = DateTime.Now.AddSeconds(frequency);
            impact.transform.position = impactPosition;
            StartCoroutine(ScaleObjectOverTime(new List<GameObject>() { impact }, new Vector3(.5f, .5f, .5f), new Vector3(2f, 2f, 2f)));
            StartCoroutine(FadeObjects(new List<GameObject>() { impact }, 1f, 0, .5f));

            while (showingImpact && DateTime.Now < nextShowImpactStartTime)
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
        ZoneInfo targetZoneInfo = target.GetComponentInParent<ZoneInfo>();  // Used for enabling all hero and heroAlly buttons in target's zone

        attacker.GetComponent<ObjectShake>().StartShaking();
        target.GetComponent<ObjectShake>().StartShaking();

        Vector3 difference = attacker.transform.position - target.transform.position;
        Vector3 directionOnly = difference.normalized;  // the direction of the launch, normalized
        Vector3 pointAlongDirection = target.transform.position + (directionOnly * .07f);  // the point along this vector you are requesting  // * float is the distance along this direction
        StartCoroutine(ShowImpact(pointAlongDirection, 3f));
        Vector3 targetFurthestPoint = GetPointFurthestFromOrigin(attacker.transform.position, target.transform.position, targetZoneInfo.transform.position);
        targetFurthestPoint = GetFurtherPointOnLine(attacker.transform.position, targetFurthestPoint);
        yield return StartCoroutine(MoveCameraUntilOnscreen(attacker.transform.position, targetFurthestPoint));

        List<GameObject> wounds = new List<GameObject>();
        if (woundsTotal >= 0)
        {
            for (int i = 0; i < woundsTotal; i++)
            {
                wounds.Add(Instantiate(woundPrefab, target.transform));
                wounds[i].transform.localPosition = new Vector3(woundPlacement[i].x, woundPlacement[i].y, 0);
            }
        }
        else  // if (woundsTotal < 0), woundsTotal is unknown (ex: circularStrike) and wound markers should be replaced with '?' icons
        {
            for (int i = 0; i < 4; i++)
            {
                wounds.Add(Instantiate(woundQuestionPrefab, target.transform));
                wounds[i].transform.localPosition = new Vector3(woundPlacement[i].x, woundPlacement[i].y, 0);
            }
        }
        StartCoroutine(FadeObjects(wounds, 0, 1, woundFadeTimeCoefficient));

        attacker.GetComponent<Unit>().SetIsClickable(true);
        targetZoneInfo.SetIsClickableForHeroesAndAllies(true);
        PanAndZoom panAndZoom = mainCamera.GetComponent<PanAndZoom>();
        //if (!IsPointOnScreen(attacker.transform.position))  // If you can still see the attacker after panning to the target, player doesn't need camera control
        //{
        //    panAndZoom.controlCamera = true;
        //}
        panAndZoom.controlCamera = true;  // Until I can guarantee the entire target zone is on screen, give player control of camera so they can click any heroAlly units within the target zone
        yield return StartCoroutine(PauseUntilPlayerPushesContinue(targetZoneInfo.gameObject));

        attacker.GetComponent<Unit>().SetIsClickable(false);
        targetZoneInfo.SetIsClickableForHeroesAndAllies(false);
        panAndZoom.controlCamera = false;
        CameraToFixedZoom();

        EndShowImpact();
        attacker.GetComponent<ObjectShake>().StopShaking();
        target.GetComponent<ObjectShake>().StopShaking();

        yield return StartCoroutine(FadeObjects(wounds, 1, 0, woundFadeTimeCoefficient));
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
        GameObject bullet = Instantiate(bulletPrefab, transform);
        bullet.transform.rotation = Quaternion.Euler(new Vector3(0, 0, Mathf.Atan2((end.y - start.y), (end.x - start.x)) * Mathf.Rad2Deg));
        while (firingBullets)
        {
            DateTime nextBulletPathStartTime = DateTime.Now.AddSeconds(frequency);

            bullet.GetComponent<CanvasGroup>().alpha = 1;
            StartCoroutine(MoveObjectOverTime(new List<GameObject>() { bullet }, start, end, 1.5f));
            while (firingBullets && DateTime.Now < nextBulletPathStartTime)
            {
                //Debug.Log("ShowBulletPath, bullet.transform.position.x " + bullet.transform.position.x.ToString() + " == end.x " + end.x.ToString() + " && bullet.transform.position.y " + bullet.transform.position.y.ToString() + " == end.y " + end.y.ToString());
                // Even when bullet.transform.position seems to == end, the boolean doesn't return true. Hence the margin of error used below.
                if (.00001 > Math.Abs(bullet.transform.position.x - end.x) && .00001 > Math.Abs(bullet.transform.position.y - end.y))  // If bullet has reached the end of its move
                {
                    bullet.GetComponent<CanvasGroup>().alpha = 0;
                }
                yield return null;
            }
        }
        Destroy(bullet);
        yield return 0;
    }

    public void EndBulletPaths()
    {
        firingBullets = false;
    }

    public IEnumerator RangedAttack(GameObject attacker, GameObject target, int woundsTotal)
    {
        ZoneInfo targetZoneInfo = target.GetComponentInParent<ZoneInfo>();  // Used for enabling all hero and heroAlly buttons in target's zone

        attacker.GetComponent<ObjectShake>().StartShaking();
        target.GetComponent<ObjectShake>().StartShaking();
        StartCoroutine(ShowBulletPath(attacker.transform.position, target.transform.position, 3f));
        Vector3 targetFurthestPoint = GetPointFurthestFromOrigin(attacker.transform.position, target.transform.position, targetZoneInfo.transform.position);
        targetFurthestPoint = GetFurtherPointOnLine(attacker.transform.position, targetFurthestPoint);
        yield return StartCoroutine(MoveCameraUntilOnscreen(attacker.transform.position, targetFurthestPoint));

        List<GameObject> wounds = new List<GameObject>();
        if (woundsTotal >= 0)
        {
            for (int i = 0; i < woundsTotal; i++)
            {
                wounds.Add(Instantiate(woundPrefab, target.transform));
                wounds[i].transform.localPosition = new Vector3(woundPlacement[i].x, woundPlacement[i].y, 0);
            }
        }
        else  // if (woundsTotal < 0), woundsTotal is unknown (ex: circularStrike) and wound markers should be replaced with '?' icons
        {
            for (int i = 0; i < 4; i++)
            {
                wounds.Add(Instantiate(woundQuestionPrefab, target.transform));
                wounds[i].transform.localPosition = new Vector3(woundPlacement[i].x, woundPlacement[i].y, 0);
            }
        }
        StartCoroutine(FadeObjects(wounds, 0, 1, woundFadeTimeCoefficient));

        attacker.GetComponent<Unit>().SetIsClickable(true);
        targetZoneInfo.SetIsClickableForHeroesAndAllies(true);
        PanAndZoom panAndZoom = mainCamera.GetComponent<PanAndZoom>();
        //if (!IsPointOnScreen(attacker.transform.position))  // If you can still see the attacker after panning to the target, player doesn't need camera control
        //{
        //    panAndZoom.controlCamera = true;
        //}
        panAndZoom.controlCamera = true;  // Until I can guarantee the entire target zone is on screen, give player control of camera so they can click any heroAlly units within the target zone
        yield return StartCoroutine(PauseUntilPlayerPushesContinue(targetZoneInfo.gameObject));

        attacker.GetComponent<Unit>().SetIsClickable(false);
        targetZoneInfo.SetIsClickableForHeroesAndAllies(false);
        panAndZoom.controlCamera = false;

        EndBulletPaths();
        attacker.GetComponent<ObjectShake>().StopShaking();
        target.GetComponent<ObjectShake>().StopShaking();

        yield return StartCoroutine(FadeObjects(wounds, 1, 0, woundFadeTimeCoefficient));
        for (int i = wounds.Count - 1; i >= 0; i--)
        {
            Destroy(wounds[i]);
        }

        yield return 0;
    }

    public IEnumerator ThrowGrenade(Vector3 origin, Vector3 destination)  // destination is the targetZone of the grenade throw
    {
        GameObject grenade = Instantiate(grenadePrefab, transform);
        grenade.transform.position = origin;
        Vector3 slightlyFurtherDestination = GetFurtherPointOnLine(origin, destination);
        StartCoroutine(MoveCameraUntilOnscreen(origin, slightlyFurtherDestination));
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

    public IEnumerator ClaimableTokenTargeted(GameObject claimableToken)
    {
        List<GameObject> questionMarks = new List<GameObject>();
        for (int i = 0; i < 10; i++)
        {
            float xLocalPos = UnityEngine.Random.Range(-40f, 40f);  // System.random (which  I'm using everywhere else) only returns an int, or NextDouble() which only returns between 0.0 and 1.0
            float yLocalPos = UnityEngine.Random.Range(-40f, 40f);
            questionMarks.Add(Instantiate(woundQuestionPrefab, claimableToken.transform));  // If could be covered up by something on villain turn, instead create as child of uiAnimationContainer
            questionMarks[i].transform.localPosition = new Vector3(xLocalPos, yLocalPos, 0);
            questionMarks[i].GetComponent<ObjectShake>().StartShaking();
        }
        StartCoroutine(ScaleObjects(questionMarks, 2, 7, .5f, .15f));

        yield return new WaitForSecondsRealtime(3);  // I don't want to yield return ScaleObjects because I don't want to wait for every object to reach its full size

        for (int i = questionMarks.Count - 1; i >= 0; i--)
        {
            //questionMarks[i].GetComponent<ObjectShake>().StopShaking();  // Not necessary
            Destroy(questionMarks[i]);
        }

        yield return 0;
    }
}
