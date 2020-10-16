using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UtilityBelt : MonoBehaviour
{
    private float lastOrthographicSize;
    private Vector3 lastCameraPosition;


    private void Awake()
    {
        //Vector3 leftPoint = Camera.main.ViewportToWorldPoint(new Vector3(0, 0));
        ////Vector3 rightPoint = Camera.main.ViewportToWorldPoint(new Vector3(1, 0));
        //Vector3 rightPoint = Camera.main.ViewportToScreenPoint(new Vector3(1, 0));
        //Debug.Log("UtilityBelt Awake(). leftPoint: " + leftPoint.ToString() + "  and rightScreenPoint: " + rightPoint.ToString());
        //transform.localScale = transform.localScale * ((float)(transform.GetComponent<RectTransform>().rect.width);
        lastOrthographicSize = Camera.main.orthographicSize;
        //lastOrthographicSize = 3.97025f;  // Initial 1920x1080 Camera.main.orthographicSize
        lastCameraPosition = Camera.main.transform.position;  // Both must be in Awake (instead of Start) so it's already set before villain starts their turn first
        float cameraWidth = 2 * Camera.main.aspect * lastOrthographicSize;
        //transform.GetComponent<RectTransform>().rect.width = cameraWidth;
        Debug.Log("UtilityBelt Awake(). cameraWidth: " + cameraWidth.ToString() + "   and rect.width: " + transform.GetComponent<RectTransform>().rect.width.ToString());
    }

    // Update is called once per frame, LateUpdate() is called after all Update functions have been called (so it doesn't lag behind PanAndZoom). Also had to change loading/execution order so this LateUpdate() occurs after PanAndZoom
    //void LateUpdate()
    //{
    //    bool scaleChanged = false;
    //    float cameraOrthoSize = Camera.main.GetComponent<Camera>().orthographicSize;

    //    // Scale UtilityBelt according to main camera's orthographicSize
    //    if (lastOrthographicSize != cameraOrthoSize)
    //    {
    //        transform.localScale = transform.localScale * cameraOrthoSize / lastOrthographicSize;
    //        lastOrthographicSize = cameraOrthoSize;
    //        scaleChanged = true;
    //    }

    //    // Position UtilityBelt at bottom based on main camera's position
    //    if (scaleChanged || lastCameraPosition != Camera.main.transform.position)
    //    {
    //        transform.position = Camera.main.ViewportToWorldPoint(new Vector3(.5f, 0));
    //        transform.position = new Vector3(transform.position.x, transform.position.y, 0);
    //        lastCameraPosition = Camera.main.transform.position;
    //    }
    //}
}
