using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Animate : MonoBehaviour
{
    GameObject mainCamera;

    public GameObject grenadePrefab;

    private void Start()
    {
        mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
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

    public IEnumerator ThrowGrenade(Vector3 origin, Vector3 destination)
    {
        GameObject grenade = Instantiate(grenadePrefab, transform);
        grenade.transform.position = origin;
        yield return StartCoroutine(MoveObjectOverTime(new List<GameObject>() { grenade, mainCamera }, origin, destination));
        Destroy(grenade);
        yield return 0;
    }
}
