using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UtilityBelt : MonoBehaviour
{
    private float lastOrthographicSize;
    private Vector3 lastCameraPosition;


    private void Awake()
    {
        lastOrthographicSize = Camera.main.GetComponent<Camera>().orthographicSize;
        lastCameraPosition = Camera.main.transform.position;  // Both must be in Awake (instead of Start) so it's already set before villain starts their turn first
    }

    // Update is called once per frame, LateUpdate() is called after all Update functions have been called (so it doesn't lag behind PanAndZoom). Also had to change loading/execution order so this LateUpdate() occurs after PanAndZoom
    void LateUpdate()
    {
        bool scaleChanged = false;
        float cameraOrthoSize = Camera.main.GetComponent<Camera>().orthographicSize;

        // Scale UtilityBelt according to main camera's orthographicSize
        if (lastOrthographicSize != cameraOrthoSize)
        {
            transform.localScale = transform.localScale * cameraOrthoSize / lastOrthographicSize;
            lastOrthographicSize = cameraOrthoSize;
            scaleChanged = true;
        }

        // Position UtilityBelt at bottom based on main camera's position
        if (scaleChanged || lastCameraPosition != Camera.main.transform.position)
        {
            transform.position = Camera.main.GetComponent<Camera>().ViewportToWorldPoint(new Vector3(.5f, 0));
            transform.position = new Vector3(transform.position.x, transform.position.y, 0);
            lastCameraPosition = Camera.main.transform.position;
        }
    }
}
