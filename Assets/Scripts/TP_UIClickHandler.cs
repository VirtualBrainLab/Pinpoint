using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TP_UIClickHandler : MonoBehaviour
{
    // Raycaster
    private GraphicRaycaster raycaster;
    [SerializeField] private TrajectoryPlannerManager tpmanager;
    [SerializeField] private TP_InPlaneSlice inPlaneSlice;
    //[SerializeField] private UM_CameraController cameraController;

    // Start is called before the first frame update
    void Start()
    {
        raycaster = GetComponent<GraphicRaycaster>();
    }

    // Update is called once per frame
    void Update()
    {
        // Check if user is over a UI element
        if (EventSystem.current.IsPointerOverGameObject())
        {
            PointerEventData pointerData = new PointerEventData(EventSystem.current);
            GameObject uiTarget = GetUIRaycastTarget(pointerData);


            // Check if the element they are over is the in-plane slice element
            if (uiTarget != null)
            {
                //cameraController.BlockDragging();

                bool leftMouseClick = Input.GetMouseButtonDown(0);

                // check if this is the in-plane slice panel
                if (uiTarget.name == "InPlaneSlicePanel")
                {
                    if (leftMouseClick)
                    {
                        // If a click happened, then target this brain region
                        inPlaneSlice.TargetBrainArea(pointerData.position);
                    }
                    else
                    {
                        // If just hovering, set the slice name
                        inPlaneSlice.InPlaneSliceHover(pointerData.position);
                    }
                }
                else
                {
                    // If the user clicks while over a UI element, check to see if it's a probe panel and activate that probe
                    if (leftMouseClick)
                    {
                        switch (uiTarget.tag)
                        {
                            case "ProbePanel":
                                tpmanager.SetActiveProbe(uiTarget.GetComponent<TP_ProbePanel>().GetProbeController());
                                break;
                            case "AreaPanel":
                                tpmanager.ClickSearchArea(uiTarget);
                                break;
                        }
                    }
                }
            }
        }
    }

    GameObject GetUIRaycastTarget(PointerEventData pointerData)
    {
        List<RaycastResult> results = new List<RaycastResult>();

        //Raycast using the Graphics Raycaster and mouse click position
        pointerData.position = Input.mousePosition;
        raycaster.Raycast(pointerData, results);

        if (results.Count == 1)
        {
            return results[0].gameObject;
        }
        if (results.Count > 1)
        {
            //Debug.Log("Warning: multiple raycast results");
            ////For every result returned, output the name of the GameObject on the Canvas hit by the Ray
            //foreach (RaycastResult result in results)
            //{
            //    Debug.Log("Hit " + result.gameObject.name);
            //}
            return results[0].gameObject;
        }

        return null;
    }
}
