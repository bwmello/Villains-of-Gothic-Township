using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;  // To fetch 2d light, but Unity can't find namespace


public class Spotlight : MonoBehaviour
{
    public Vector3 leftPoint;
    public Vector3 rightPoint;
    private Vector3 startingPoint;
    private float startingIntensity;
    private float randomOscillationProgress;

    private void Awake()
    {
        startingPoint = transform.localPosition;
        randomOscillationProgress = Random.Range(0f, 3.0f);
        startingIntensity = transform.GetComponent<Light2D>().intensity;
        transform.GetComponent<Light2D>().intensity = 0;
    }

    IEnumerator Start()
    {
        startingPoint = transform.localPosition;
        while (true)
        {
            if (transform.GetComponent<Light2D>().intensity < startingIntensity)
            {
                transform.GetComponent<Light2D>().intensity += .0015f;
            }
            yield return StartCoroutine(OscillateLeftAndRight());
        }
    }

    IEnumerator OscillateLeftAndRight()
    {
        float xScalar = (leftPoint.x - startingPoint.x);
        float yScalar = (leftPoint.y - startingPoint.y);
        transform.localPosition = new Vector3(Mathf.Sin(Time.time + randomOscillationProgress) * xScalar + startingPoint.x, Mathf.Sin(Time.time + randomOscillationProgress) * yScalar + startingPoint.y, 0);
        yield return null;
    }

    IEnumerator OscillateBetweenTwoPoints()
    {
        float xScalar = Mathf.Abs(leftPoint.x - rightPoint.x) / 2;
        float yScalar = Mathf.Abs(leftPoint.y - rightPoint.y) / 2;
        transform.localPosition = new Vector3(Mathf.Sin(Time.time) * xScalar + startingPoint.x, Mathf.Sin(Time.time) * yScalar + startingPoint.y, 0);
        yield return null;
    }
}
