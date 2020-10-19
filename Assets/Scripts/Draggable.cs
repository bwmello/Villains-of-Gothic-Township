using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Draggable : MonoBehaviour
{
    public string draggableType;
    PanAndZoom panAndZoom;
    private bool isDragging = false;
    private Vector3 positionOnDragStart;
    private GameObject dropZone;

    // Start is called before the first frame update
    void Start()
    {
        panAndZoom = Camera.main.GetComponent<PanAndZoom>();
    }

    // Update is called once per frame
    void Update()
    {
        if (isDragging)
        {
            Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            transform.position = new Vector3(mousePosition.x, mousePosition.y, transform.position.z);
        }
    }

    public void StartDrag()
    {
        isDragging = true;
        EnableDropZones();
        panAndZoom.controlCamera = false;
        positionOnDragStart = transform.position;
        transform.localScale = new Vector3(1.5f, 1.5f, 1);
    }

    public void EndDrag()
    {
        if (dropZone != null)
        {
            if (draggableType == "Gas")
            {
                ZoneInfo targetZone = dropZone.GetComponentInParent<ZoneInfo>();
                if (targetZone)  // Prevents error when dropped on another ToolDraggable's collider.
                {
                    StartCoroutine(dropZone.GetComponentInParent<ZoneInfo>().AddEnvironTokens(new EnvironTokenSave("Gas", 1, true, false)));
                }
            }
            else if (draggableType == "Smoke")
            {
                ZoneInfo targetZone = dropZone.GetComponentInParent<ZoneInfo>();
                if (targetZone)
                {
                    StartCoroutine(dropZone.GetComponentInParent<ZoneInfo>().AddEnvironTokens(new EnvironTokenSave("Smoke", 1, true, false)));
                }
            }
            else if (draggableType == "WallBreak")
            {
                WallRubble targetWallRubble = dropZone.GetComponentInParent<WallRubble>();
                if (targetWallRubble)
                {
                    dropZone.GetComponentInParent<WallRubble>().WallRubblePlaced();
                }
            }
        }
        DisableDropZones();
        isDragging = false;
        panAndZoom.controlCamera = true;
        transform.localScale = new Vector3(1, 1, 1);
        transform.position = positionOnDragStart;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        dropZone = collision.gameObject;
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (dropZone == collision.gameObject)  // Helps WallBreak dropping be a little more reliable when being dragged over other WallBreak DropZones.
        {
            dropZone = null;
        }
    }

    private void EnableDropZones()
    {
        foreach (GameObject dropZone in GetDropZones())
        {
            if (draggableType == "Smoke" || draggableType == "Gas")
            {
                dropZone.GetComponent<ZoneInfo>().EnableDropZone();
            }
            else if (draggableType == "WallBreak")
            {
                dropZone.GetComponent<WallRubble>().EnableDropZone();
            }
        }
    }

    private void DisableDropZones()
    {
        foreach (GameObject dropZone in GetDropZones())
        {
            if (draggableType == "Smoke" || draggableType == "Gas")
            {
                dropZone.GetComponent<ZoneInfo>().DisableDropZone();
            }
            else if (draggableType == "WallBreak")
            {
                dropZone.GetComponent<WallRubble>().DisableDropZone();
            }
        }
    }

    private GameObject[] GetDropZones()
    {
        if (draggableType == "Smoke" || draggableType == "Gas")
        {
            return GameObject.FindGameObjectsWithTag("ZoneInfoPanel");
        }
        else if (draggableType == "WallBreak")
        {
            return GameObject.FindGameObjectsWithTag("WallRubble");
        }

        return null;
    }
}
