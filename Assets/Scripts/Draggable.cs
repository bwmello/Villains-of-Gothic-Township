using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Draggable : MonoBehaviour
{
    public string draggableType;
    public bool isDraggable = true;
    public bool isDragging = false;
    private Vector3 positionOnDragStart;
    private Transform parentOnDragStart;
    private GameObject dropZone;
    private List<GameObject> enabledDropZones = new List<GameObject>();
    private ScenarioMap scenarioMap;


    private void Start()
    {
        scenarioMap = GameObject.FindGameObjectWithTag("ScenarioMap").GetComponent<ScenarioMap>();
    }

    // Update is called once per frame
    void Update()
    {
        if (isDragging)  // Can't check if !isDraggable here because EndDrag() still called
        {
            Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            transform.position = new Vector3(mousePosition.x, mousePosition.y, transform.position.z);
        }
    }

    public void StartDrag()
    {
        if (isDraggable)
        {
            isDragging = true;
            EnableDropZones();
            Camera.main.GetComponent<PanAndZoom>().controlCamera = false;  // TODO Comment this out then test on device
            positionOnDragStart = transform.position;
            transform.localScale = new Vector3(1.5f, 1.5f, 1);
            if (draggableType == "Hero" || draggableType == "Unit")  // Any draggable not inside the UIOverlay
            {
                parentOnDragStart = transform.parent;
                transform.SetParent(GameObject.FindGameObjectWithTag("AnimationContainer").transform);
            }
        }
    }

    public void EndDrag()
    {
        if (isDraggable)
        {
            if (dropZone != null)
            {
                switch (draggableType)
                {
                    case "Smoke":
                        ZoneInfo smokeTargetZone = dropZone.GetComponentInParent<ZoneInfo>();
                        if (smokeTargetZone)
                        {
                            StartCoroutine(dropZone.GetComponentInParent<ZoneInfo>().AddEnvironTokens(new EnvironTokenSave("Smoke", 1, true, false)));
                        }
                        break;
                    case "Gas":
                        ZoneInfo gasTargetZone = dropZone.GetComponentInParent<ZoneInfo>();
                        if (gasTargetZone)  // Prevents error when dropped on another ToolDraggable's collider.
                        {
                            StartCoroutine(dropZone.GetComponentInParent<ZoneInfo>().AddEnvironTokens(new EnvironTokenSave("Gas", 1, true, false)));
                        }
                        break;
                    case "WallBreak":
                        WallRubble targetWallRubble = dropZone.GetComponentInParent<WallRubble>();
                        if (targetWallRubble)
                        {
                            dropZone.GetComponentInParent<WallRubble>().WallRubblePlaced();
                        }
                        break;
                    case "Hero":
                        ZoneInfo heroTargetZone = dropZone.GetComponentInParent<ZoneInfo>();
                        if (heroTargetZone)
                        {
                            dropZone.GetComponentInParent<ZoneInfo>().AddHeroToZone(gameObject);
                        }
                        else
                        {
                            transform.position = positionOnDragStart;
                        }
                        break;
                    case "AllySetup":
                        ZoneInfo allySetupTargetZone = dropZone.GetComponentInParent<ZoneInfo>();
                        if (allySetupTargetZone)
                        {
                            GameObject placedAlly = allySetupTargetZone.GetComponentInParent<ZoneInfo>().AddUnitToZone(tag, gameObject.GetComponent<Unit>().size);
                            if (placedAlly)  // May not have been anymore available unitSlots
                            {
                                Unit placedAllyUnit = placedAlly.GetComponent<Unit>();
                                placedAllyUnit.SetIsDraggable(false);
                                placedAllyUnit.SetIsClickable(true);
                                placedAllyUnit.isHeroAlly = true;
                            }
                        }
                        break;
                    case "Unit":
                        ZoneInfo unitTargetZone = dropZone.GetComponentInParent<ZoneInfo>();
                        if (unitTargetZone)
                        {
                            GameObject availableUnitSlot = dropZone.GetComponentInParent<ZoneInfo>().GetAvailableUnitSlot();
                            if (availableUnitSlot)
                            {
                                transform.SetParent(availableUnitSlot.transform);
                                transform.localPosition = new Vector3(0, 0, transform.localPosition.z);
                            }
                            else
                            {
                                transform.position = positionOnDragStart;
                                if (parentOnDragStart)
                                {
                                    transform.SetParent(parentOnDragStart);
                                }
                            }
                        }
                        else
                        {
                            transform.position = positionOnDragStart;
                            if (parentOnDragStart)
                            {
                                transform.SetParent(parentOnDragStart);
                            }
                        }
                        break;
                }
                if (new List<string>() { "Smoke", "Gas", "WallBreak", "AllySetup" }.Contains(draggableType))
                {
                    transform.position = positionOnDragStart;  // Return to position on UIOverlay
                }
            }
            else
            {
                transform.position = positionOnDragStart;
                if (parentOnDragStart)
                {
                    transform.SetParent(parentOnDragStart);
                }
            }
            isDragging = false;
            DisableDropZones();
            Camera.main.GetComponent<PanAndZoom>().controlCamera = true;
            transform.localScale = new Vector3(1, 1, 1);
        }
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
        enabledDropZones = new List<GameObject>(GetDropZones());
        foreach (GameObject dropZone in enabledDropZones)
        {
            if (draggableType == "WallBreak")
            {
                dropZone.GetComponent<WallRubble>().EnableDropZone();
            }
            else  // Smoke, Gas, Hero, Ally, Bystander
            {
                dropZone.GetComponent<ZoneInfo>().EnableDropZone();
            }
        }
    }

    private void DisableDropZones()
    {
        foreach (GameObject dropZone in enabledDropZones)
        {
            if (draggableType == "WallBreak")
            {
                dropZone.GetComponent<WallRubble>().DisableDropZone();
            }
            else  // Smoke, Gas, Hero, Ally, Bystander
            {
                dropZone.GetComponent<ZoneInfo>().DisableDropZone();
            }
        }
        enabledDropZones = new List<GameObject>();
    }

    private List<GameObject> GetDropZones()
    {
        switch (draggableType)
        {
            case "Smoke":
            case "Gas":
                return new List<GameObject>(GameObject.FindGameObjectsWithTag("ZoneInfoPanel"));
            case "WallBreak":
                return new List<GameObject>(GameObject.FindGameObjectsWithTag("WallRubble"));
            case "Hero":
                List<GameObject> heroDropZones = new List<GameObject>(GameObject.FindGameObjectsWithTag("ZoneInfoPanel"));
                heroDropZones.Remove(gameObject.GetComponent<Hero>().GetZone());
                return heroDropZones;
            case "AllySetup":
                List<GameObject> allyDropZones = new List<GameObject>();
                foreach (GameObject hero in scenarioMap.heroes)
                {
                    allyDropZones.AddRange(hero.GetComponent<Hero>().GetPlaceableAllyZones(gameObject.GetComponent<Unit>().size));
                }
                return allyDropZones;
            case "Unit":  // Extra move points can be spent when activating allies, so they can go anywhere
                List<GameObject> unitDropZones = new List<GameObject>(GameObject.FindGameObjectsWithTag("ZoneInfoPanel"));
                unitDropZones.Remove(gameObject.GetComponent<Unit>().GetZone());
                return unitDropZones;
        }
        return null;
    }
}
