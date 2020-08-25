using UnityEngine;
using System.Collections;


public class ObjectShake : MonoBehaviour
{
    float startTime;
    float animDur = .2f;

    Vector3 startPos, endPos;
    float posRange = .01f;
    bool noZMove = true;

    Vector3 startRot, endRot;
    float rotRange = 15f;
    bool lockRotToZ = true;

    bool keepShaking = false;

    // Use this for initialization
    public void StartShaking()
    {
        startPos = transform.position;
        startRot = transform.eulerAngles;

        MakeNewEnds();
        keepShaking = true;
    }

    public void StopShaking()
    {
        keepShaking = false;
        transform.position = startPos;
        transform.eulerAngles = startRot;
    }

    float RandomRange(float a, float b)
    {
        if (a > b)
        {
            return Random.Range(b, a);
        }
        else
        {
            return Random.Range(a, b);
        }
    }

    void MakeNewEnds()
    {
        startTime = Time.time;

        endPos = new Vector3(
            RandomRange(startPos.x - posRange, startPos.x + posRange),
            RandomRange(startPos.y - posRange, startPos.y + posRange),
            noZMove ? 0 : RandomRange(startPos.z - posRange, startPos.z + posRange)
        );

        endRot = new Vector3(
            lockRotToZ ? 0 : RandomRange(startRot.x - rotRange, startRot.x + rotRange),
            lockRotToZ ? 0 : RandomRange(startRot.y - rotRange, startRot.y + rotRange),
            RandomRange(startRot.z - rotRange, startRot.z + rotRange)
        );
    }

    // Update is called once per frame
    void Update()
    {
        if (keepShaking)
        {
            float timePassed = Time.time - startTime;
            float pctComplete = timePassed / animDur;

            if (pctComplete >= 1.0f)
            {
                MakeNewEnds();
                return;
            }

            if (pctComplete < 0.5f)
            {
                transform.position = Vector3.Lerp(startPos, endPos, pctComplete * 2f);
                transform.eulerAngles = Vector3.Lerp(startRot, endRot, pctComplete * 2f);
            }
            else
            {
                // tween back
                float pct2 = (1.0f - pctComplete) * 2f;
                transform.position = Vector3.Lerp(startPos, endPos, pct2);
                transform.eulerAngles = Vector3.Lerp(startRot, endRot, pct2);
            }
        }
    }
}