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
            switch (draggableType)
            {
                case "Smoke":
                case "Gas":
                case "WallBreak":
                case "Interrogate":
                case "AllySetup":
                    parentOnDragStart = transform.parent;
                    transform.SetParent(GetComponentInParent<UIOverlay>().uiAnimationContainer.transform);  // So you can't drag it behind other UIOverlay draggables
                    break;
                case "Hero":
                case "Unit":  // Any draggable not inside the UIOverlay
                    parentOnDragStart = transform.parent;
                    transform.SetParent(GameObject.FindGameObjectWithTag("AnimationContainer").transform);
                    break;
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
                        ZoneInfo smokeTargetZone = dropZone.GetComponent<ZoneInfo>();
                        if (smokeTargetZone)
                        {
                            StartCoroutine(smokeTargetZone.AddEnvironTokens(new EnvironTokenSave("Smoke", 1, true, false)));
                        }
                        if (parentOnDragStart)
                        {
                            transform.SetParent(parentOnDragStart);
                        }
                        break;
                    case "Gas":
                        ZoneInfo gasTargetZone = dropZone.GetComponent<ZoneInfo>();
                        if (gasTargetZone)  // Prevents error when dropped on another ToolDraggable's collider.
                        {
                            StartCoroutine(gasTargetZone.AddEnvironTokens(new EnvironTokenSave("Gas", 1, true, false)));
                        }
                        if (parentOnDragStart)
                        {
                            transform.SetParent(parentOnDragStart);
                        }
                        break;
                    case "WallBreak":
                        WallRubble targetWallRubble = dropZone.GetComponent<WallRubble>();
                        if (targetWallRubble)
                        {
                            targetWallRubble.WallRubblePlaced();
                        }
                        if (parentOnDragStart)
                        {
                            transform.SetParent(parentOnDragStart);
                        }
                        break;
                    case "Interrogate":
                        Unit interrogatedUnit = dropZone.GetComponent<Unit>();  // Not really needed since Unit type is checked during OnColisionEnter2D, so could do MissionSpecifics.UnitInterrogated(dropZone) directly
                        if (interrogatedUnit)
                        {
                            StartCoroutine(MissionSpecifics.UnitInterrogated(interrogatedUnit.gameObject));
                        }
                        if (parentOnDragStart)
                        {
                            transform.SetParent(parentOnDragStart);
                        }
                        break;
                    case "Hero":
                        ZoneInfo heroTargetZone = dropZone.GetComponent<ZoneInfo>();
                        if (heroTargetZone)
                        {
                            heroTargetZone.AddHeroToZone(gameObject);
                        }
                        else
                        {
                            transform.position = positionOnDragStart;
                        }
                        break;
                    case "AllySetup":
                        ZoneInfo allySetupTargetZone = dropZone.GetComponent<ZoneInfo>();
                        if (allySetupTargetZone)
                        {
                            GameObject placedAlly = allySetupTargetZone.AddUnitToZone(tag, gameObject.GetComponent<Unit>().size);
                            if (placedAlly)  // May not have been anymore available unitSlots
                            {
                                Unit placedAllyUnit = placedAlly.GetComponent<Unit>();
                                placedAllyUnit.SetIsDraggable(false);
                                placedAllyUnit.SetIsClickable(true);
                                placedAllyUnit.isHeroAlly = true;
                            }
                        }
                        if (parentOnDragStart)
                        {
                            transform.SetParent(parentOnDragStart);
                        }
                        break;
                    case "Unit":
                        ZoneInfo unitTargetZone = dropZone.GetComponent<ZoneInfo>();
                        if (unitTargetZone)
                        {
                            GameObject availableUnitSlot = unitTargetZone.GetAvailableUnitSlot();
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
                if (new List<string>() { "Smoke", "Gas", "WallBreak", "Interrogate", "AllySetup" }.Contains(draggableType))
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
        if (draggableType == "WallBreak")
        {
            GameObject wallBreak = collision.transform.parent.gameObject;
            if (wallBreak && wallBreak.TryGetComponent<WallRubble>(out var tempWallRubble)) {
                dropZone = wallBreak;
            }
        }
        else if (draggableType == "Interrogate")
        {
            //GameObject interrogationTarget = collision.transform.parent.gameObject;
            GameObject interrogationTarget = collision.transform.gameObject;  // For some reason collides with boxcollider on main Unit parent instead of dropZone child
            if (interrogationTarget && interrogationTarget.TryGetComponent<Unit>(out var tempUnit))
            {
                dropZone = interrogationTarget;
            }
        }
        else  // Smoke, Gas, Hero, Ally, Unit
        {
            GameObject zoneInfoPanel = collision.transform.parent.gameObject;
            if (zoneInfoPanel && zoneInfoPanel.TryGetComponent<ZoneInfo>(out var tempZoneInfoPanel))
            {
                dropZone = zoneInfoPanel;
            }
        }
        //string onCollisionEnterDebugString = "!!!" + gameObject.name + " OnCollisionEnter2D with " + collision.gameObject.name + " and collision.transform.parent.gameObject " + collision.transform.parent.gameObject.name + "  with dropZone now set to: ";
        //onCollisionEnterDebugString += dropZone ? dropZone.name : "null";
        //Debug.Log(onCollisionEnterDebugString);
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        //string onCollisionExitDebugString = "!!!" + gameObject.name + " OnCollisionExit2D with " + collision.gameObject.name + " and collision.transform.parent.gameObject " + collision.transform.parent.gameObject.name + "  with dropZone previously: ";
        //onCollisionExitDebugString += dropZone ? dropZone.name : " null";
        if (dropZone == collision.transform.parent.gameObject)  // Helps WallBreak dropping be a little more reliable when being dragged over other WallBreak DropZones.
        {
            dropZone = null;
        }
        //onCollisionExitDebugString += dropZone ? "  with dropZone now set to: " + dropZone.name : "  with dropZone now set to: null";
        //Debug.Log(onCollisionExitDebugString);
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
            else if (draggableType == "Interrogate")
            {
                dropZone.GetComponent<Unit>().EnableDropZone();
            }
            else  // Smoke, Gas, Hero, Ally, Unit
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
            else if (draggableType == "Interrogate")
            {
                dropZone.GetComponent<Unit>().DisableDropZone();
            }
            else  // Smoke, Gas, Hero, Ally
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
            case "Interrogate":
                return MissionSpecifics.GetInterrogationTargets();
                //return new List<GameObject>(GameObject.FindGameObjectsWithTag("WallRubble"));
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
