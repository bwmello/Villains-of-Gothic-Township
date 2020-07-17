using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Animate : MonoBehaviour
{
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
}
